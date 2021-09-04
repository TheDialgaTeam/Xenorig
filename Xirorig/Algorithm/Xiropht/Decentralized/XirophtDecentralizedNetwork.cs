using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Xirorig.Algorithm.Xiropht.Decentralized.Api.JobResult;
using Xirorig.Algorithm.Xiropht.Decentralized.Api.JobResult.Models;
using Xirorig.Algorithm.Xiropht.Decentralized.Api.JobTemplate;
using Xirorig.Algorithm.Xiropht.Decentralized.Api.JobTemplate.Models;
using Xirorig.Network;
using Xirorig.Network.Api.JobResult;
using Xirorig.Network.Api.JobTemplate;
using Xirorig.Options;
using Xirorig.Utility;

namespace Xirorig.Algorithm.Xiropht.Decentralized
{
    internal class XirophtDecentralizedNetwork : INetwork
    {
        public event NetworkConnectionStatus? Connected;
        public event NetworkConnectionStatus? Disconnected;
        public event NetworkJob? NewJob;
        public event NetworkJobResult? JobResult;

        private readonly ApplicationContext _context;
        private readonly Pool _pool;

        private readonly string _blockchainVersion;
        private readonly int _blockchainChecksum;

        private readonly HttpClient _httpClient;
        private readonly Timer _downloadBlockTemplateTimer;

        private string? _currentBlockHash;

        public XirophtDecentralizedNetwork(ApplicationContext context, Pool pool)
        {
            _context = context;
            _pool = pool;

            var coin = _pool.GetCoin();

            if (coin.Equals("xiropht", StringComparison.OrdinalIgnoreCase))
            {
                _blockchainVersion = "01";
                _blockchainChecksum = 16;
            }
            else if (coin.Equals("xirobod", StringComparison.OrdinalIgnoreCase))
            {
                _blockchainVersion = "01";
                _blockchainChecksum = 16;
            }
            else
            {
                throw new JsonException("The selected coin is not supported.");
            }

            if (!IsValidWalletAddress(_pool.GetUsername())) throw new JsonException("Invalid wallet address for the specified pool.");

            _httpClient = new HttpClient
            {
                BaseAddress = new Uri(new UriBuilder(_pool.GetUrl()).ToString()),
                DefaultRequestHeaders = { UserAgent = { ProductInfoHeaderValue.Parse(_pool.GetUserAgent()) } },
                Timeout = TimeSpan.FromMilliseconds(_context.Options.GetMaxPingTime())
            };

            _downloadBlockTemplateTimer = new Timer(DownloadBlockTemplateCallback, null, Timeout.Infinite, Timeout.Infinite);
        }

        public void StartNetwork()
        {
            _downloadBlockTemplateTimer.Change(0, 1000);
            Connected?.Invoke(_pool, null);
        }

        public void StopNetwork()
        {
            _downloadBlockTemplateTimer.Change(0, Timeout.Infinite);
        }

        public async Task SubmitJobAsync(IJobTemplate jobTemplate, IJobResult jobResult)
        {
            if (jobTemplate is not BlockTemplate blockTemplate) return;
            if (jobResult is not MiningShare miningShare) return;

            try
            {
                miningShare.MiningPowShareObject.WalletAddress = _pool.GetUsername();

                var requestTime = DateTime.Now;

                var json = JsonSerializer.Serialize(new SendMiningShare.Request(JsonSerializer.Serialize(miningShare, NetworkUtility.DefaultJsonSerializerOptions)), NetworkUtility.DefaultJsonSerializerOptions);
                var response = await _httpClient.PostAsync(string.Empty, new StringContent(json), _context.ApplicationShutdownCancellationToken);

                var jsonResponse = JsonSerializer.Deserialize<SendMiningShare.Response>(await response.Content.ReadAsStreamAsync(), NetworkUtility.DefaultJsonSerializerOptions);
                if (jsonResponse == null) throw new JsonException();

                var miningShareResponse = JsonSerializer.Deserialize<MiningShareResponse>(jsonResponse.PacketObjectSerialized);
                if (miningShareResponse == null) throw new JsonException();

                var responseTime = DateTime.Now - requestTime;

                switch (miningShareResponse.MiningShareResult)
                {
                    case MiningShareResult.EmptyShare:
                        JobResult?.Invoke(_pool, false, "Empty share submitted", blockTemplate.CurrentBlockDifficulty.ToString(), responseTime.TotalMilliseconds);
                        break;

                    case MiningShareResult.InvalidWalletAddress:
                        JobResult?.Invoke(_pool, false, "Invalid wallet address submitted", blockTemplate.CurrentBlockDifficulty.ToString(), responseTime.TotalMilliseconds);
                        break;

                    case MiningShareResult.InvalidBlockHash:
                        JobResult?.Invoke(_pool, false, "Invalid block hash submitted", blockTemplate.CurrentBlockDifficulty.ToString(), responseTime.TotalMilliseconds);
                        break;

                    case MiningShareResult.InvalidBlockHeight:
                        JobResult?.Invoke(_pool, false, "Invalid block height submitted", blockTemplate.CurrentBlockDifficulty.ToString(), responseTime.TotalMilliseconds);
                        break;

                    case MiningShareResult.InvalidNonceShare:
                        JobResult?.Invoke(_pool, false, "Invalid nonce share submitted", blockTemplate.CurrentBlockDifficulty.ToString(), responseTime.TotalMilliseconds);
                        break;

                    case MiningShareResult.InvalidShareFormat:
                        JobResult?.Invoke(_pool, false, "Invalid share format submitted", blockTemplate.CurrentBlockDifficulty.ToString(), responseTime.TotalMilliseconds);
                        break;

                    case MiningShareResult.InvalidShareDifficulty:
                        JobResult?.Invoke(_pool, false, "Invalid share difficulty submitted", blockTemplate.CurrentBlockDifficulty.ToString(), responseTime.TotalMilliseconds);
                        break;

                    case MiningShareResult.InvalidShareEncryption:
                        JobResult?.Invoke(_pool, false, "Invalid share encryption submitted", blockTemplate.CurrentBlockDifficulty.ToString(), responseTime.TotalMilliseconds);
                        break;

                    case MiningShareResult.InvalidShareData:
                        JobResult?.Invoke(_pool, false, "Invalid share data submitted", blockTemplate.CurrentBlockDifficulty.ToString(), responseTime.TotalMilliseconds);
                        break;

                    case MiningShareResult.InvalidShareDataSize:
                        JobResult?.Invoke(_pool, false, "Invalid share data size submitted", blockTemplate.CurrentBlockDifficulty.ToString(), responseTime.TotalMilliseconds);
                        break;

                    case MiningShareResult.InvalidShareCompatibility:
                        JobResult?.Invoke(_pool, false, "Invalid share compatibility submitted", blockTemplate.CurrentBlockDifficulty.ToString(), responseTime.TotalMilliseconds);
                        break;

                    case MiningShareResult.InvalidTimestampShare:
                        JobResult?.Invoke(_pool, false, "Invalid timestamp submitted", blockTemplate.CurrentBlockDifficulty.ToString(), responseTime.TotalMilliseconds);
                        break;

                    case MiningShareResult.LowDifficultyShare:
                        JobResult?.Invoke(_pool, false, "Low difficulty share submitted", blockTemplate.CurrentBlockDifficulty.ToString(), responseTime.TotalMilliseconds);
                        break;

                    case MiningShareResult.BlockAlreadyFound:
                        JobResult?.Invoke(_pool, false, "Block already found", blockTemplate.CurrentBlockDifficulty.ToString(), responseTime.TotalMilliseconds);
                        break;

                    case MiningShareResult.ValidShare:
                        JobResult?.Invoke(_pool, true, string.Empty, blockTemplate.CurrentBlockDifficulty.ToString(), responseTime.TotalMilliseconds);
                        break;

                    case MiningShareResult.ValidUnlockBlockShare:
                        JobResult?.Invoke(_pool, true, string.Empty, blockTemplate.CurrentBlockDifficulty.ToString(), responseTime.TotalMilliseconds);
                        break;

                    default:
                        throw new JsonException($"{nameof(MiningShareResult)} is not implemented.");
                }
            }
            catch (Exception exception)
            {
                Disconnected?.Invoke(_pool, exception);
            }
        }

        private bool IsValidWalletAddress(string walletAddress)
        {
            var base58RawBytes = Base58Utility.Decode(walletAddress);
            if (Convert.ToHexString(base58RawBytes, 0, 1) != _blockchainVersion) return false;

            var givenChecksumBytes = base58RawBytes.AsSpan(base58RawBytes.Length - _blockchainChecksum);

            var hash1 = Sha2Utility.ComputeSha256Hash(base58RawBytes, 0, base58RawBytes.Length - _blockchainChecksum);
            var hash2 = Sha2Utility.ComputeSha256Hash(hash1);

            return givenChecksumBytes.SequenceEqual(hash2.AsSpan(0, _blockchainChecksum));
        }

        private async void DownloadBlockTemplateCallback(object? state)
        {
            try
            {
                var response = await _httpClient.GetStreamAsync("get_block_template", _context.ApplicationShutdownCancellationToken);

                var jsonResponse = JsonSerializer.Deserialize<GetBlockTemplate.Response>(response, NetworkUtility.DefaultJsonSerializerOptions);
                if (jsonResponse == null) throw new JsonException();

                var blockTemplate = JsonSerializer.Deserialize<BlockTemplate>(jsonResponse.PacketObjectSerialized, NetworkUtility.DefaultJsonSerializerOptions);
                if (blockTemplate == null) throw new JsonException();

                if (_currentBlockHash == blockTemplate.CurrentBlockHash) return;
                _currentBlockHash = blockTemplate.CurrentBlockHash;

                NewJob?.Invoke(_pool, blockTemplate);
            }
            catch (Exception exception)
            {
                Disconnected?.Invoke(_pool, exception);
            }
        }

        public void Dispose()
        {
            _httpClient.Dispose();
            _downloadBlockTemplateTimer.Dispose();
        }
    }
}