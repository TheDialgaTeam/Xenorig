using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using TheDialgaTeam.Extensions.Logging.LoggingTemplate;
using TheDialgaTeam.Serilog.Formatting.Ansi;
using Xirorig.Algorithm;
using Xirorig.Network.Api;
using Xirorig.Network.Api.Models;
using Xirorig.Options;
using Xirorig.Options.Xirorig;
using Xirorig.Utility;

namespace Xirorig.Network
{
    internal class XirorigNetwork : IDisposable
    {
        public event Action<PeerNode>? Connected;
        public event Action<PeerNode>? Disconnected;
        public event Action<PeerNode, BlockTemplate>? NewJob;
        public event Action<bool, string>? BlockResult;

        private readonly ILoggerTemplate<XirorigNetwork> _logger;
        private readonly IAlgorithm[] _algorithms;

        private readonly PeerNode[] _peerNodes;
        private int _currentPeerIndex;
        private int _currentPeerRetryCount;

        private ConnectionStatus _connectionStatus = ConnectionStatus.Disconnected;
        private HttpClient? _httpClient;
        private readonly int _maxPing;
        private readonly CancellationToken _cancellationToken;

        private readonly Timer _downloadBlockTemplateTimer;

        private bool _isActive;

        private string _currentBlockHash = string.Empty;

        public PeerNode CurrentPeerNode => _peerNodes[_currentPeerIndex];

        public XirorigNetwork(ILoggerTemplate<XirorigNetwork> logger, IOptions<XirorigOptions> options, IHostApplicationLifetime hostApplicationLifetime, IEnumerable<IAlgorithm> algorithms)
        {
            _logger = logger;
            _algorithms = algorithms.ToArray();
            _peerNodes = options.Value.PeerNodes;
            _maxPing = options.Value.MaxPing;
            _cancellationToken = hostApplicationLifetime.ApplicationStopping;
            _downloadBlockTemplateTimer = new Timer(DownloadBlockTemplateCallback, null, Timeout.Infinite, Timeout.Infinite);
        }

        public void StartNetwork()
        {
            if (_isActive) return;
            _isActive = true;

            _connectionStatus = ConnectionStatus.Disconnected;
            _downloadBlockTemplateTimer.Change(0, 1000);
        }

        public void StopNetwork()
        {
            if (!_isActive) return;
            _isActive = false;

            _downloadBlockTemplateTimer.Change(0, Timeout.Infinite);

            _connectionStatus = ConnectionStatus.Disconnected;
            Disconnected?.Invoke(_peerNodes[_currentPeerIndex]);
        }

        public async Task SendMiningShareAsync(string packet)
        {
            try
            {
                var response = await _httpClient!.PostAsync(string.Empty, new StringContent(JsonConvert.SerializeObject(new SendMiningShare.Request
                {
                    PacketContentObjectSerialized = packet
                })), _cancellationToken);

                var jsonResponse = JsonConvert.DeserializeObject<SendMiningShare.Response>(await response.Content.ReadAsStringAsync(_cancellationToken));
                if (jsonResponse == null) throw new NullReferenceException();

                var miningShareResponse = JsonConvert.DeserializeObject<MiningShareResponse>(jsonResponse.PacketObjectSerialized);
                if (miningShareResponse == null) throw new NullReferenceException();

                switch (miningShareResponse.MiningShareResult)
                {
                    case MiningShareResult.EmptyShare:
                        BlockResult?.Invoke(false, "Empty share submitted");
                        break;

                    case MiningShareResult.InvalidWalletAddress:
                        BlockResult?.Invoke(false, "Invalid wallet address submitted");
                        break;

                    case MiningShareResult.InvalidBlockHash:
                        BlockResult?.Invoke(false, "Invalid block hash submitted");
                        break;

                    case MiningShareResult.InvalidBlockHeight:
                        BlockResult?.Invoke(false, "Invalid block height submitted");
                        break;

                    case MiningShareResult.InvalidNonceShare:
                        BlockResult?.Invoke(false, "Invalid nonce share submitted");
                        break;

                    case MiningShareResult.InvalidShareFormat:
                        BlockResult?.Invoke(false, "Invalid share format submitted");
                        break;

                    case MiningShareResult.InvalidShareDifficulty:
                        BlockResult?.Invoke(false, "Invalid share difficulty submitted");
                        break;

                    case MiningShareResult.InvalidShareEncryption:
                        BlockResult?.Invoke(false, "Invalid share encryption submitted");
                        break;

                    case MiningShareResult.InvalidShareData:
                        BlockResult?.Invoke(false, "Invalid share data submitted");
                        break;

                    case MiningShareResult.InvalidShareDataSize:
                        BlockResult?.Invoke(false, "Invalid share data size submitted");
                        break;

                    case MiningShareResult.InvalidShareCompatibility:
                        BlockResult?.Invoke(false, "Invalid share compatibility submitted");
                        break;

                    case MiningShareResult.InvalidTimestampShare:
                        BlockResult?.Invoke(false, "Invalid timestamp submitted");
                        break;

                    case MiningShareResult.LowDifficultyShare:
                        BlockResult?.Invoke(false, "Low difficulty share submitted");
                        break;

                    case MiningShareResult.BlockAlreadyFound:
                        BlockResult?.Invoke(false, "Block already found");
                        break;

                    case MiningShareResult.ValidShare:
                        BlockResult?.Invoke(true, string.Empty);
                        break;

                    case MiningShareResult.ValidUnlockBlockShare:
                        BlockResult?.Invoke(true, string.Empty);
                        break;
                }
            }
            catch (Exception)
            {
                _currentPeerRetryCount++;

                if (_currentPeerRetryCount >= 5)
                {
                    _connectionStatus = ConnectionStatus.Disconnected;
                    _httpClient?.Dispose();

                    _currentBlockHash = string.Empty;

                    Disconnected?.Invoke(_peerNodes[_currentPeerIndex]);

                    _currentPeerIndex++;
                    if (_currentPeerIndex >= _peerNodes.Length) _currentPeerIndex = 0;
                }
            }
        }

        private async void DownloadBlockTemplateCallback(object? state)
        {
            try
            {
                if (_connectionStatus == ConnectionStatus.Disconnected)
                {
                    _httpClient = new HttpClient { BaseAddress = new Uri(new UriBuilder(_peerNodes[_currentPeerIndex].Url).ToString()), Timeout = TimeSpan.FromMilliseconds(_maxPing) };

                    if (!WalletUtility.IsValidWalletAddress(_peerNodes[_currentPeerIndex].WalletAddress, _algorithms.First(algorithm => _peerNodes[_currentPeerIndex].Algorithm == algorithm.AlgorithmType)))
                    {
                        _logger.LogInformation($"{AnsiEscapeCodeConstants.RedForegroundColor}Error: Invalid wallet address for the specified pool.{AnsiEscapeCodeConstants.Reset}", true);
                        throw new ArgumentException();
                    }
                }

                var response = await _httpClient!.GetStringAsync("get_block_template", _cancellationToken);

                var jsonResponse = JsonConvert.DeserializeObject<GetBlockTemplate.Response>(response);
                if (jsonResponse == null) throw new NullReferenceException();

                var blockTemplate = JsonConvert.DeserializeObject<BlockTemplate>(jsonResponse.PacketObjectSerialized);
                if (blockTemplate == null) throw new NullReferenceException();

                if (_connectionStatus == ConnectionStatus.Disconnected)
                {
                    _connectionStatus = ConnectionStatus.Connected;
                    Connected?.Invoke(_peerNodes[_currentPeerIndex]);
                    _currentPeerRetryCount = 0;
                }

                if (_currentBlockHash != blockTemplate.CurrentBlockHash)
                {
                    _currentBlockHash = blockTemplate.CurrentBlockHash;
                    NewJob?.Invoke(_peerNodes[_currentPeerIndex], blockTemplate);
                }
            }
            catch (Exception)
            {
                _currentPeerRetryCount++;

                if (_currentPeerRetryCount >= 5)
                {
                    _connectionStatus = ConnectionStatus.Disconnected;
                    _httpClient?.Dispose();

                    _currentBlockHash = string.Empty;

                    Disconnected?.Invoke(_peerNodes[_currentPeerIndex]);

                    _currentPeerIndex++;
                    if (_currentPeerIndex >= _peerNodes.Length) _currentPeerIndex = 0;
                }
            }
        }

        public void Dispose()
        {
            _httpClient?.Dispose();
            _downloadBlockTemplateTimer.Dispose();
        }
    }
}