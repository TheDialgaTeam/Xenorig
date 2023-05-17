using System.Runtime.CompilerServices;
using System.Text;
using Microsoft.Extensions.Options;
using Xenolib.Algorithms.Xenophyte.Centralized.Networking.Solo;
using Xenolib.Algorithms.Xenophyte.Centralized.Utilities;
using Xenolib.Utilities;
using Xenopool.Server.Options;

namespace Xenopool.Server.SoloMining;

public sealed class SoloMiningNetwork : IDisposable
{
    public const string JobTypeEasy = "Easy Block";
    public const string JobTypeSemiRandom = "Semi Random";
    public const string JobTypeRandom = "Random";
    
    public SoloMiningJob? CurrentMiningJob { get; private set; }
    
    private readonly IOptions<XenopoolOptions> _options;
    private readonly ILogger<SoloMiningNetwork> _logger;

    private readonly Network _network = new();
    private readonly NetworkConnection _networkConnection;

    public SoloMiningNetwork(IOptions<XenopoolOptions> options, ILogger<SoloMiningNetwork> logger)
    {
        _options = options;
        _logger = logger;

        _networkConnection = new NetworkConnection
        {
            Uri = new UriBuilder(options.Value.SoloMining.Host) { Port = options.Value.SoloMining.Port }.Uri,
            WalletAddress = options.Value.RpcWallet.WalletAddress,
            TimeoutDuration = TimeSpan.FromSeconds(options.Value.SoloMining.NetworkTimeoutDuration)
        };
    }

    public async Task StartAsync(CancellationToken cancellationToken = default)
    {
        _network.Disconnected += NetworkOnDisconnected;
        _network.Ready += NetworkOnReady;
        _network.HasNewBlock += NetworkOnHasNewBlock;

        await _network.ConnectAsync(_networkConnection, cancellationToken);
    }

    private async Task StopAsync(CancellationToken cancellationToken = default)
    {
        _network.Disconnected -= NetworkOnDisconnected;
        _network.Ready -= NetworkOnReady;
        _network.HasNewBlock -= NetworkOnHasNewBlock;

        await _network.DisconnectAsync(cancellationToken);
    }

    private async void NetworkOnDisconnected(string reason)
    {
        Logger.PrintDisconnected(_logger, _networkConnection.Uri.Host, reason);
        await _network.ConnectAsync(_networkConnection);
    }

    private void NetworkOnReady()
    {
        Logger.PrintConnected(_logger, "SOLO", _networkConnection.Uri.Host);
    }

    private void NetworkOnHasNewBlock(BlockHeader blockHeader)
    {
        Logger.PrintJob(_logger, "new job", _networkConnection.Uri.Host, blockHeader.BlockDifficulty, blockHeader.BlockMethod, blockHeader.BlockHeight);

        CurrentMiningJob = new SoloMiningJob(blockHeader);

        var easyBlockValues = CurrentMiningJob.EasyBlockValues;

        Span<long> chunkData = stackalloc long[easyBlockValues.Length];
        BufferUtility.MemoryCopy(easyBlockValues, chunkData, easyBlockValues.Length);

        for (var i = easyBlockValues.Length - 1; i >= 0; i--)
        {
            var choseRandom = RandomNumberGeneratorUtility.GetRandomBetween(0, i);

            for (var j = easyBlockValues.Length - 1; j >= 0; j--)
            {
                var choseRandom2 = RandomNumberGeneratorUtility.GetRandomBetween(0, j);

                DoMathCalculations(chunkData.GetRef(choseRandom), easyBlockValues.GetRef(choseRandom2), JobTypeEasy, blockHeader);

                (easyBlockValues.GetRef(j), easyBlockValues.GetRef(choseRandom2)) = (easyBlockValues.GetRef(choseRandom2), easyBlockValues.GetRef(j));
            }

            chunkData.GetRef(choseRandom) = chunkData.GetRef(i);
        }
    }

    private void DoMathCalculations(long firstNumber, long secondNumber, string jobType, BlockHeader blockHeader)
    {
        if (firstNumber > secondNumber)
        {
            // Subtraction Rule:
            var subtractionResult = firstNumber - secondNumber;

            if (subtractionResult >= blockHeader.BlockMinRange)
            {
                ValidateAndSubmitShare(firstNumber, secondNumber, subtractionResult, '-', jobType, blockHeader);
            }

            // Division Rule:
            var (integerDivideResult, integerDivideRemainder) = Math.DivRem(firstNumber, secondNumber);

            if (integerDivideRemainder == 0 && integerDivideResult >= blockHeader.BlockMinRange)
            {
                ValidateAndSubmitShare(firstNumber, secondNumber, integerDivideResult, '/', jobType, blockHeader);
            }

            // Modulo Rule:
            if (integerDivideRemainder >= blockHeader.BlockMinRange)
            {
                ValidateAndSubmitShare(firstNumber, secondNumber, integerDivideRemainder, '%', jobType, blockHeader);
            }
        }
        else
        {
            var integerDivideRemainder = firstNumber % secondNumber;

            // Modulo Rule:
            if (integerDivideRemainder >= blockHeader.BlockMinRange)
            {
                ValidateAndSubmitShare(firstNumber, secondNumber, integerDivideRemainder, '%', jobType, blockHeader);
            }
        }

        // Addition Rule:
        var additionResult = firstNumber + secondNumber;

        if (additionResult <= blockHeader.BlockMaxRange)
        {
            ValidateAndSubmitShare(firstNumber, secondNumber, additionResult, '+', jobType, blockHeader);
        }

        // Multiplication Rule:
        var multiplicationResult = firstNumber * secondNumber;

        if (multiplicationResult <= blockHeader.BlockMaxRange)
        {
            ValidateAndSubmitShare(firstNumber, secondNumber, multiplicationResult, '*', jobType, blockHeader);
        }
    }

    [SkipLocalsInit]
    private void ValidateAndSubmitShare(long firstNumber, long secondNumber, long solution, char op, string jobType, Xenolib.Algorithms.Xenophyte.Centralized.Networking.Solo.BlockHeader blockHeader)
    {
        Span<char> stringToEncrypt = stackalloc char[19 + 1 + 1 + 1 + 19 + 19 + 1];

        firstNumber.TryFormat(stringToEncrypt, out var firstNumberWritten);

        stringToEncrypt.GetRef(firstNumberWritten) = ' ';
        stringToEncrypt.GetRef(firstNumberWritten + 1) = op;
        stringToEncrypt.GetRef(firstNumberWritten + 2) = ' ';

        secondNumber.TryFormat(stringToEncrypt[(firstNumberWritten + 3)..], out var secondNumberWritten);
        blockHeader.BlockTimestampCreate.TryFormat(stringToEncrypt[(firstNumberWritten + 3 + secondNumberWritten)..], out var finalWritten);

        Span<byte> bytesToEncrypt = stackalloc byte[firstNumberWritten + 3 + secondNumberWritten + finalWritten];
        Encoding.ASCII.GetBytes(stringToEncrypt[..(firstNumberWritten + 3 + secondNumberWritten + finalWritten)], bytesToEncrypt);

        Span<byte> encryptedShare = stackalloc byte[64 * 2];
        Span<byte> hashEncryptedShare = stackalloc byte[64 * 2];

        if (!CpuMinerUtility.MakeEncryptedShare(bytesToEncrypt, encryptedShare, hashEncryptedShare, blockHeader.XorKey, blockHeader.AesKey, blockHeader.AesIv, blockHeader.AesRound))
        {
            return;
        }

        Span<char> hashEncryptedShareString = stackalloc char[Encoding.ASCII.GetCharCount(hashEncryptedShare)];
        Encoding.ASCII.GetChars(hashEncryptedShare, hashEncryptedShareString);

        if (!hashEncryptedShareString.SequenceEqual(blockHeader.BlockIndication)) return;

        /*
        _network.SendPacketToNetwork(new PacketData($"{NetworkConstants.ReceiveJob}|{Encoding.UTF8.GetString(encryptedShare)}|{solution}|{firstNumber} {op} {secondNumber}|{hashEncryptedShareString}|{blockHeader.BlockHeight}|{_options.Value.SoloMining.UserAgent}", true, (packet, time) =>
        {
            Span<char> temp = stackalloc char[Encoding.UTF8.GetCharCount(packet)];
            Encoding.UTF8.GetChars(packet, temp);

            if (!temp.StartsWith(NetworkConstants.SendJobStatus)) return;
            temp = temp[(NetworkConstants.SendJobStatus.Length + 1)..];

            if (temp.StartsWith(NetworkConstants.ShareWrong))
            {
                //FoundBlock?.Invoke(cpuMinerJob.BlockHeight, jobType, false, InvalidShare, time.TotalMilliseconds);
            }
            else if (temp.StartsWith(NetworkConstants.ShareUnlock))
            {
                //FoundBlock?.Invoke(cpuMinerJob.BlockHeight, jobType, true, string.Empty, time.TotalMilliseconds);
            }
            else if (temp.StartsWith(NetworkConstants.ShareAleady))
            {
                //FoundBlock?.Invoke(cpuMinerJob.BlockHeight, jobType, false, OrphanShare, time.TotalMilliseconds);
            }
            else if (temp.StartsWith(NetworkConstants.ShareNotExist))
            {
                //FoundBlock?.Invoke(cpuMinerJob.BlockHeight, jobType, false, InvalidShare, time.TotalMilliseconds);
            }
        }));
        */

        Logger.PrintBlockFound(_logger, jobType, firstNumber, op, secondNumber, solution);
    }

    public void Dispose()
    {
        StopAsync().Wait();
        _network.Dispose();
    }
}