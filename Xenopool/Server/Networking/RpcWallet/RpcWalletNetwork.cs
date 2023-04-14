using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using Microsoft.Extensions.Options;
using Xenolib.Utilities;
using Xenolib.Utilities.Buffer;
using Xenolib.Utilities.KeyDerivationFunction;
using Xenopool.Server.Options;

namespace Xenopool.Server.Networking.RpcWallet;

public sealed class RpcWalletNetwork : IDisposable
{
    private readonly IDisposable? _disposable;
    private readonly HttpClient _httpClient;

    private bool _useEncryption;

    private readonly byte[] _aesKey = new byte[32];
    private readonly byte[] _aesIv = new byte[16];

    private string _walletAddress;

    public RpcWalletNetwork(IOptionsMonitor<XenopoolOptions> optionsMonitor)
    {
        _disposable = optionsMonitor.OnChange(OnOptionsChanged);
        _httpClient = new HttpClient();

        OnOptionsChanged(optionsMonitor.CurrentValue);
    }

    public async Task<bool> CheckIfWalletAddressExistAsync(CancellationToken cancellationToken = default)
    {
        var totalWalletIndex = await GetTotalWalletIndexAsync(cancellationToken);
       
        for (var i = 0; i < totalWalletIndex; i++)
        {
            var address = await GetWalletAddressByIndexAsync(i + 1, cancellationToken);
            if (_walletAddress == address) return true;
        }

        return false;
    }

    public async Task<int> GetTotalWalletIndexAsync(CancellationToken cancellationToken = default)
    {
        var response = await DoGetRequestAsync("get_total_wallet_index", GetTotalWalletIndexResponseContext.Default.GetTotalWalletIndexResponse, cancellationToken);
        return response?.Result ?? 0;
    }

    public async Task<string> GetWalletAddressByIndexAsync(int index, CancellationToken cancellationToken = default)
    {
        var response = await DoGetRequestAsync($"get_wallet_address_by_index|{index}", GetWalletAddressByIndexResponseContext.Default.GetWalletAddressByIndexResponse, cancellationToken);
        return response?.Result ?? "wallet_not_exist";
    }

    private async Task<T?> DoGetRequestAsync<T>(string request, JsonTypeInfo<T> context, CancellationToken cancellationToken = default)
    {
        if (!_useEncryption) return await _httpClient.GetFromJsonAsync(request, context, cancellationToken);

        var requestLength = Encoding.UTF8.GetByteCount(request);
        using var requestBytes = ArrayPoolOwner<byte>.Rent(requestLength);
        Encoding.UTF8.GetBytes(request, requestBytes.Span);

        var aesEncryptOutputLength = requestLength + (16 - requestLength % 16);
        using var aesEncryptOutput = ArrayPoolOwner<byte>.Rent(aesEncryptOutputLength);
        SymmetricAlgorithmUtility.Encrypt_AES_256_CFB_8(_aesKey, _aesIv, requestBytes.Span, aesEncryptOutput.Span);

        var base64EncodeLength = Base64Utility.EncodeLength(aesEncryptOutput.Span);
        using var base64EncodedOutput = ArrayPoolOwner<byte>.Rent(base64EncodeLength);
        Base64Utility.Encode(aesEncryptOutput.Span, base64EncodedOutput.Span);

        var response = await _httpClient.GetStringAsync(Encoding.UTF8.GetString(base64EncodedOutput.Span), cancellationToken);
        using var responseBytes = ArrayPoolOwner<byte>.Rent(Encoding.UTF8.GetByteCount(response));
        Encoding.UTF8.GetBytes(response, responseBytes.Span);

        var base64DecodeLength = Base64Utility.DecodeLength(responseBytes.Span);
        using var base64DecodeOutput = ArrayPoolOwner<byte>.Rent(base64DecodeLength);
        Base64Utility.Decode(responseBytes.Span, base64DecodeOutput.Span);

        using var aesDecryptOutput = ArrayPoolOwner<byte>.Rent(base64DecodeLength);
        var outputLength = SymmetricAlgorithmUtility.Decrypt_AES_256_CFB_8(_aesKey, _aesIv, base64DecodeOutput.Span, aesDecryptOutput.Span);

        return JsonSerializer.Deserialize(aesDecryptOutput.Span[..outputLength], context);
    }

    [SkipLocalsInit]
    private void OnOptionsChanged(XenopoolOptions options)
    {
        _walletAddress = options.RpcWallet.WalletAddress;
        
        _httpClient.BaseAddress = new UriBuilder(options.RpcWallet.Host) { Port = options.RpcWallet.Port }.Uri;
        _httpClient.Timeout = TimeSpan.FromSeconds(options.RpcWallet.NetworkTimeoutDuration);
        _httpClient.DefaultRequestHeaders.UserAgent.Clear();
        _httpClient.DefaultRequestHeaders.UserAgent.ParseAdd(options.RpcWallet.UserAgent);

        if (options.RpcWallet.EncryptionKey.Length >= 8)
        {
            _useEncryption = true;

            Span<byte> encryptionKey = stackalloc byte[Encoding.UTF8.GetByteCount(options.RpcWallet.EncryptionKey)];
            Encoding.UTF8.GetBytes(options.RpcWallet.EncryptionKey, encryptionKey);

            Span<byte> saltKey = stackalloc byte[Encoding.UTF8.GetByteCount(options.RpcWallet.EncryptionKey[..8])];
            Encoding.UTF8.GetBytes(options.RpcWallet.EncryptionKey.AsSpan(0, 8), saltKey);

            using var pbkdf1 = new PBKDF1(encryptionKey, Encoding.UTF8.GetBytes(Convert.ToHexString(saltKey)));
            pbkdf1.FillBytes(_aesKey);
            pbkdf1.FillBytes(_aesIv);
        }
        else
        {
            _useEncryption = false;
        }
    }

    public void Dispose()
    {
        _disposable?.Dispose();
        _httpClient.Dispose();
    }
}