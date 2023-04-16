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
    private readonly HttpClient _httpClient;

    private readonly bool _useEncryption;
    private readonly byte[] _aesKey = new byte[32];
    private readonly byte[] _aesIv = new byte[16];

    private readonly string _walletAddress;

    public RpcWalletNetwork(IOptions<XenopoolOptions> options)
    {
        _httpClient = new HttpClient();

        _walletAddress = options.Value.RpcWallet.WalletAddress;
        
        _httpClient.BaseAddress = new UriBuilder(options.Value.RpcWallet.Host) { Port = options.Value.RpcWallet.Port }.Uri;
        _httpClient.Timeout = TimeSpan.FromSeconds(options.Value.RpcWallet.NetworkTimeoutDuration);
        _httpClient.DefaultRequestHeaders.UserAgent.Clear();
        _httpClient.DefaultRequestHeaders.UserAgent.ParseAdd(options.Value.RpcWallet.UserAgent);

        if (options.Value.RpcWallet.EncryptionKey.Length >= 8)
        {
            _useEncryption = true;

            Span<byte> encryptionKey = stackalloc byte[Encoding.UTF8.GetByteCount(options.Value.RpcWallet.EncryptionKey)];
            Encoding.UTF8.GetBytes(options.Value.RpcWallet.EncryptionKey, encryptionKey);

            Span<byte> saltKey = stackalloc byte[Encoding.UTF8.GetByteCount(options.Value.RpcWallet.EncryptionKey[..8])];
            Encoding.UTF8.GetBytes(options.Value.RpcWallet.EncryptionKey.AsSpan(0, 8), saltKey);

            using var pbkdf1 = new PBKDF1(encryptionKey, Encoding.UTF8.GetBytes(Convert.ToHexString(saltKey)));
            pbkdf1.FillBytes(_aesKey);
            pbkdf1.FillBytes(_aesIv);
        }
        else
        {
            _useEncryption = false;
        }
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

    private async Task<int> GetTotalWalletIndexAsync(CancellationToken cancellationToken = default)
    {
        var response = await DoGetRequestAsync("get_total_wallet_index", GetTotalWalletIndexResponseContext.Default.GetTotalWalletIndexResponse, cancellationToken);
        return response?.Result ?? 0;
    }

    private async Task<string> GetWalletAddressByIndexAsync(int index, CancellationToken cancellationToken = default)
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

    public void Dispose()
    {
        _httpClient.Dispose();
    }
}