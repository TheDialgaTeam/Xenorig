using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Xirorig.Algorithms.Xiropht.Decentralized.Api.JobResult;
using Xirorig.Algorithms.Xiropht.Decentralized.Api.JobResult.Models;
using Xirorig.Algorithms.Xiropht.Decentralized.Api.JobTemplate;
using Xirorig.Algorithms.Xiropht.Decentralized.Api.JobTemplate.Models;
using Xirorig.Miner;
using Xirorig.Miner.Network.Api.JobResult;
using Xirorig.Miner.Network.Api.JobTemplate;
using Xirorig.Options;
using Xirorig.Utilities;

namespace Xirorig.Algorithms.Xiropht.Decentralized
{
    internal class XirophtDecentralizedNetwork : MinerNetwork
    {
        private readonly string _blockchainVersion;
        private readonly int _blockchainChecksum;

        private readonly HttpClient _httpClient;
        private readonly Timer _downloadBlockTemplateTimer;

        private bool _connected;
        private string? _currentBlockHash;

        protected override string AlgorithmName => "Xiropht Decentralized";

        public XirophtDecentralizedNetwork(Pool pool, int maxResponseTime, CancellationToken cancellationToken) : base(pool, cancellationToken)
        {
            var coin = pool.GetCoin();

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

            if (!IsValidWalletAddress(pool.GetUsername())) throw new JsonException("Invalid wallet address for the specified pool.");

            _httpClient = new HttpClient
            {
                BaseAddress = new UriBuilder(pool.GetUrl()).Uri,
                DefaultRequestHeaders = { UserAgent = { ProductInfoHeaderValue.Parse(pool.GetUserAgent()) } },
                Timeout = TimeSpan.FromMilliseconds(maxResponseTime)
            };

            _downloadBlockTemplateTimer = new Timer(DownloadBlockTemplateCallback, null, Timeout.Infinite, Timeout.Infinite);
        }

        public override void StartNetwork()
        {
            base.StartNetwork();
            _downloadBlockTemplateTimer.Change(0, Timeout.Infinite);
        }

        public override void StopNetwork()
        {
            base.StopNetwork();

            _downloadBlockTemplateTimer.Change(Timeout.Infinite, Timeout.Infinite);
            _httpClient.CancelPendingRequests();
        }

        public override async Task SubmitJobAsync(IJobTemplate jobTemplate, IJobResult jobResult)
        {
            if (jobTemplate is not BlockTemplate blockTemplate) return;
            if (jobResult is not MiningShare miningShare) return;

            try
            {
                var requestTime = DateTime.Now;

                var json = JsonSerializer.Serialize(new SendMiningShare.Request(JsonSerializer.Serialize(miningShare, DefaultJsonSerializerOptions)), DefaultJsonSerializerOptions);
                var response = await _httpClient.PostAsync(string.Empty, new StringContent(json), CancellationToken);

                var jsonResponse = JsonSerializer.Deserialize<SendMiningShare.Response>(await response.Content.ReadAsStreamAsync(), DefaultJsonSerializerOptions);
                if (jsonResponse == null) throw new JsonException();

                var miningShareResponse = JsonSerializer.Deserialize<MiningShareResponse>(jsonResponse.PacketObjectSerialized, DefaultJsonSerializerOptions);
                if (miningShareResponse == null) throw new JsonException();

                var responseTime = DateTime.Now - requestTime;

                switch (miningShareResponse.MiningShareResult)
                {
                    case MiningShareResult.EmptyShare:
                        OnJobResult(false, blockTemplate.CurrentBlockDifficulty.ToString(), responseTime.TotalMilliseconds, "Empty share submitted");
                        break;

                    case MiningShareResult.InvalidWalletAddress:
                        OnJobResult(false, blockTemplate.CurrentBlockDifficulty.ToString(), responseTime.TotalMilliseconds, "Invalid wallet address submitted");
                        break;

                    case MiningShareResult.InvalidBlockHash:
                        OnJobResult(false, blockTemplate.CurrentBlockDifficulty.ToString(), responseTime.TotalMilliseconds, "Invalid block hash submitted");
                        break;

                    case MiningShareResult.InvalidBlockHeight:
                        OnJobResult(false, blockTemplate.CurrentBlockDifficulty.ToString(), responseTime.TotalMilliseconds, "Invalid block height submitted");
                        break;

                    case MiningShareResult.InvalidNonceShare:
                        OnJobResult(false, blockTemplate.CurrentBlockDifficulty.ToString(), responseTime.TotalMilliseconds, "Invalid nonce share submitted");
                        break;

                    case MiningShareResult.InvalidShareFormat:
                        OnJobResult(false, blockTemplate.CurrentBlockDifficulty.ToString(), responseTime.TotalMilliseconds, "Invalid share format submitted");
                        break;

                    case MiningShareResult.InvalidShareDifficulty:
                        OnJobResult(false, blockTemplate.CurrentBlockDifficulty.ToString(), responseTime.TotalMilliseconds, "Invalid share difficulty submitted");
                        break;

                    case MiningShareResult.InvalidShareEncryption:
                        OnJobResult(false, blockTemplate.CurrentBlockDifficulty.ToString(), responseTime.TotalMilliseconds, "Invalid share encryption submitted");
                        break;

                    case MiningShareResult.InvalidShareData:
                        OnJobResult(false, blockTemplate.CurrentBlockDifficulty.ToString(), responseTime.TotalMilliseconds, "Invalid share data submitted");
                        break;

                    case MiningShareResult.InvalidShareDataSize:
                        OnJobResult(false, blockTemplate.CurrentBlockDifficulty.ToString(), responseTime.TotalMilliseconds, "Invalid share data size submitted");
                        break;

                    case MiningShareResult.InvalidShareCompatibility:
                        OnJobResult(false, blockTemplate.CurrentBlockDifficulty.ToString(), responseTime.TotalMilliseconds, "Invalid share compatibility submitted");
                        break;

                    case MiningShareResult.InvalidTimestampShare:
                        OnJobResult(false, blockTemplate.CurrentBlockDifficulty.ToString(), responseTime.TotalMilliseconds, "Invalid timestamp submitted");
                        break;

                    case MiningShareResult.LowDifficultyShare:
                        OnJobResult(false, blockTemplate.CurrentBlockDifficulty.ToString(), responseTime.TotalMilliseconds, "Low difficulty share submitted");
                        break;

                    case MiningShareResult.BlockAlreadyFound:
                        OnJobResult(false, blockTemplate.CurrentBlockDifficulty.ToString(), responseTime.TotalMilliseconds, "Block already found");
                        break;

                    case MiningShareResult.ValidShare:
                        OnJobResult(true, blockTemplate.CurrentBlockDifficulty.ToString(), responseTime.TotalMilliseconds);
                        break;

                    case MiningShareResult.ValidUnlockBlockShare:
                        OnJobResult(true, blockTemplate.CurrentBlockDifficulty.ToString(), responseTime.TotalMilliseconds);
                        break;

                    default:
                        throw new JsonException($"{nameof(MiningShareResult)} is not implemented.");
                }
            }
            catch (TaskCanceledException)
            {
                _connected = false;
                OnDisconnected("Timeout");
            }
            catch (OperationCanceledException)
            {
                _connected = false;
                OnDisconnected("Timeout");
            }
            catch (HttpRequestException)
            {
                _connected = false;
                OnDisconnected("Disconnected");
            }
            catch (Exception exception)
            {
                _connected = false;
                OnDisconnected(exception.Message, exception);
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
                if (!_connected)
                {
                    OnConnected();
                    _connected = true;
                }

                var requestTime = DateTime.Now;

                var response = await _httpClient.GetStreamAsync("get_block_template", CancellationToken);

                var jsonResponse = JsonSerializer.Deserialize<GetBlockTemplate.Response>(response, DefaultJsonSerializerOptions);
                if (jsonResponse == null) throw new JsonException();

                var blockTemplate = JsonSerializer.Deserialize<BlockTemplate>(jsonResponse.PacketObjectSerialized, DefaultJsonSerializerOptions);
                if (blockTemplate == null) throw new JsonException();

                var responseTime = DateTime.Now - requestTime;
                if (!CancellationToken.IsCancellationRequested) _downloadBlockTemplateTimer.Change((int) Math.Max((TimeSpan.FromSeconds(1) - responseTime).TotalMilliseconds, 0), Timeout.Infinite);

                if (_currentBlockHash == blockTemplate.CurrentBlockHash) return;
                _currentBlockHash = blockTemplate.CurrentBlockHash;

                OnNewJob(blockTemplate, blockTemplate.CurrentBlockDifficulty.ToString(), blockTemplate.CurrentBlockHeight.ToString("D"));
            }
            catch (TaskCanceledException)
            {
                _connected = false;
                OnDisconnected("Timeout");
                if (!CancellationToken.IsCancellationRequested) _downloadBlockTemplateTimer.Change(0, Timeout.Infinite);
            }
            catch (OperationCanceledException)
            {
                _connected = false;
                OnDisconnected("Timeout");
                if (!CancellationToken.IsCancellationRequested) _downloadBlockTemplateTimer.Change(0, Timeout.Infinite);
            }
            catch (HttpRequestException)
            {
                _connected = false;
                OnDisconnected("Disconnected");
                if (!CancellationToken.IsCancellationRequested) _downloadBlockTemplateTimer.Change(0, Timeout.Infinite);
            }
            catch (Exception exception)
            {
                _connected = false;
                OnDisconnected(exception.Message, exception);
            }
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);

            if (!disposing) return;

            _httpClient.Dispose();
            _downloadBlockTemplateTimer.Dispose();
        }
    }
}