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
using Xirorig.Utilities;

namespace Xirorig.Algorithm.Xiropht.Decentralized
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

        public XirophtDecentralizedNetwork(ProgramContext context, Pool pool) : base(context, pool)
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
                Timeout = TimeSpan.FromMilliseconds(context.Options.GetMaxPingTime())
            };

            _downloadBlockTemplateTimer = new Timer(DownloadBlockTemplateCallback, null, Timeout.Infinite, Timeout.Infinite);
        }

        public override void StartNetwork()
        {
            base.StartNetwork();

            _downloadBlockTemplateTimer.Change(TimeSpan.FromSeconds(1), Timeout.InfiniteTimeSpan);
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
                        OnJobResult(false, "Empty share submitted", blockTemplate.CurrentBlockDifficulty.ToString(), responseTime.TotalMilliseconds);
                        break;

                    case MiningShareResult.InvalidWalletAddress:
                        OnJobResult(false, "Invalid wallet address submitted", blockTemplate.CurrentBlockDifficulty.ToString(), responseTime.TotalMilliseconds);
                        break;

                    case MiningShareResult.InvalidBlockHash:
                        OnJobResult(false, "Invalid block hash submitted", blockTemplate.CurrentBlockDifficulty.ToString(), responseTime.TotalMilliseconds);
                        break;

                    case MiningShareResult.InvalidBlockHeight:
                        OnJobResult(false, "Invalid block height submitted", blockTemplate.CurrentBlockDifficulty.ToString(), responseTime.TotalMilliseconds);
                        break;

                    case MiningShareResult.InvalidNonceShare:
                        OnJobResult(false, "Invalid nonce share submitted", blockTemplate.CurrentBlockDifficulty.ToString(), responseTime.TotalMilliseconds);
                        break;

                    case MiningShareResult.InvalidShareFormat:
                        OnJobResult(false, "Invalid share format submitted", blockTemplate.CurrentBlockDifficulty.ToString(), responseTime.TotalMilliseconds);
                        break;

                    case MiningShareResult.InvalidShareDifficulty:
                        OnJobResult(false, "Invalid share difficulty submitted", blockTemplate.CurrentBlockDifficulty.ToString(), responseTime.TotalMilliseconds);
                        break;

                    case MiningShareResult.InvalidShareEncryption:
                        OnJobResult(false, "Invalid share encryption submitted", blockTemplate.CurrentBlockDifficulty.ToString(), responseTime.TotalMilliseconds);
                        break;

                    case MiningShareResult.InvalidShareData:
                        OnJobResult(false, "Invalid share data submitted", blockTemplate.CurrentBlockDifficulty.ToString(), responseTime.TotalMilliseconds);
                        break;

                    case MiningShareResult.InvalidShareDataSize:
                        OnJobResult(false, "Invalid share data size submitted", blockTemplate.CurrentBlockDifficulty.ToString(), responseTime.TotalMilliseconds);
                        break;

                    case MiningShareResult.InvalidShareCompatibility:
                        OnJobResult(false, "Invalid share compatibility submitted", blockTemplate.CurrentBlockDifficulty.ToString(), responseTime.TotalMilliseconds);
                        break;

                    case MiningShareResult.InvalidTimestampShare:
                        OnJobResult(false, "Invalid timestamp submitted", blockTemplate.CurrentBlockDifficulty.ToString(), responseTime.TotalMilliseconds);
                        break;

                    case MiningShareResult.LowDifficultyShare:
                        OnJobResult(false, "Low difficulty share submitted", blockTemplate.CurrentBlockDifficulty.ToString(), responseTime.TotalMilliseconds);
                        break;

                    case MiningShareResult.BlockAlreadyFound:
                        OnJobResult(false, "Block already found", blockTemplate.CurrentBlockDifficulty.ToString(), responseTime.TotalMilliseconds);
                        break;

                    case MiningShareResult.ValidShare:
                        OnJobResult(true, string.Empty, blockTemplate.CurrentBlockDifficulty.ToString(), responseTime.TotalMilliseconds);
                        break;

                    case MiningShareResult.ValidUnlockBlockShare:
                        OnJobResult(true, string.Empty, blockTemplate.CurrentBlockDifficulty.ToString(), responseTime.TotalMilliseconds);
                        break;

                    default:
                        throw new JsonException($"{nameof(MiningShareResult)} is not implemented.");
                }
            }
            catch (Exception exception)
            {
                _connected = false;
                OnDisconnected(exception);
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
                var requestTime = DateTime.Now;

                var response = await _httpClient.GetStreamAsync("get_block_template", CancellationToken);

                var jsonResponse = JsonSerializer.Deserialize<GetBlockTemplate.Response>(response, DefaultJsonSerializerOptions);
                if (jsonResponse == null) throw new JsonException();

                var blockTemplate = JsonSerializer.Deserialize<BlockTemplate>(jsonResponse.PacketObjectSerialized, DefaultJsonSerializerOptions);
                if (blockTemplate == null) throw new JsonException();

                var responseTime = DateTime.Now - requestTime;
                if (!CancellationToken.IsCancellationRequested) _downloadBlockTemplateTimer.Change((int) Math.Max((TimeSpan.FromSeconds(1) - responseTime).TotalMilliseconds, 0), Timeout.Infinite);

                if (!_connected)
                {
                    OnConnected();
                    _connected = true;
                }

                if (_currentBlockHash == blockTemplate.CurrentBlockHash) return;
                _currentBlockHash = blockTemplate.CurrentBlockHash;

                OnNewJob(blockTemplate, blockTemplate.CurrentBlockDifficulty.ToString(), (ulong) blockTemplate.CurrentBlockHeight);
            }
            catch (Exception exception)
            {
                _connected = false;
                OnDisconnected(exception);
                if (!CancellationToken.IsCancellationRequested) _downloadBlockTemplateTimer.Change(1000, Timeout.Infinite);
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