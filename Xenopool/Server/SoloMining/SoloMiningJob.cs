using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Text;
using Google.Protobuf;
using Xenolib.Algorithms.Xenophyte.Centralized.Networking.Pool;
using Xenolib.Algorithms.Xenophyte.Centralized.Utilities;
using Xenolib.Utilities;
using Xenopool.Server.Pool;
using SoloBlockHeader = Xenolib.Algorithms.Xenophyte.Centralized.Networking.Solo.BlockHeader;

namespace Xenopool.Server.SoloMining;

public sealed class SoloMiningJob
{
    public BlockHeaderResponse BlockHeaderResponse { get; }
    
    public Span<long> EasyBlockValues => _easyBlockValues.AsSpan(0, _easyBlockValuesLength);

    private static readonly char[] Operators = { '+', '-', '*', '/', '%' };
    private static readonly char[] Operators2 = { '+', '*', '%' };

    private readonly long[] _easyBlockValues = new long[256];
    private readonly int _easyBlockValuesLength;

    public SoloMiningJob(SoloBlockHeader blockHeader)
    {
        BlockHeaderResponse = new BlockHeaderResponse
        {
            Status = true,
            BlockHeader = new BlockHeaderResponse.Types.BlockHeader
            {
                BlockHeight = blockHeader.BlockHeight,
                BlockTimestampCreate = blockHeader.BlockTimestampCreate,
                BlockMethod = blockHeader.BlockMethod,
                BlockIndication = blockHeader.BlockIndication,
                BlockDifficulty = blockHeader.BlockDifficulty,
                BlockMinRange = blockHeader.BlockMinRange,
                BlockMaxRange = blockHeader.BlockMaxRange,
                XorKey = ByteString.CopyFrom(blockHeader.XorKey),
                AesKey = ByteString.CopyFrom(blockHeader.AesKey),
                AesIv = ByteString.CopyFrom(blockHeader.AesIv),
                AesRound = blockHeader.AesRound
            }
        };

        _easyBlockValuesLength = CpuMinerUtility.GenerateEasyBlockNumbers(blockHeader.BlockMinRange, blockHeader.BlockMaxRange, _easyBlockValues);
    }

    public bool TryGenerateSemiRandomPoolShare([MaybeNullWhen(false)] out PoolShare poolShare)
    {
        if (EasyBlockValues.Length < 256)
        {
            poolShare = null;
            return false;
        }

        var header = BlockHeaderResponse.BlockHeader;

        long firstNumber, secondNumber;
        char @operator;
        long solution;

        if (RandomNumberGeneratorUtility.GetRandomBetween(0, 99) < 50)
        {
            firstNumber = EasyBlockValues[RandomNumberGeneratorUtility.GetRandomBetween(0, 255)];

            do
            {
                secondNumber = RandomNumberGeneratorUtility.GetBiasRandomBetween(header.BlockMinRange, header.BlockMaxRange);
            } while (EasyBlockValues.Contains(secondNumber));
        }
        else
        {
            secondNumber = EasyBlockValues[RandomNumberGeneratorUtility.GetRandomBetween(0, 255)];

            do
            {
                firstNumber = RandomNumberGeneratorUtility.GetBiasRandomBetween(header.BlockMinRange, header.BlockMaxRange);
            } while (EasyBlockValues.Contains(firstNumber));
        }

        do
        {
            @operator = firstNumber > secondNumber ? Operators[RandomNumberGeneratorUtility.GetRandomBetween(0, Operators.Length - 1)] : Operators2[RandomNumberGeneratorUtility.GetRandomBetween(0, Operators2.Length - 1)];

            solution = @operator switch
            {
                '+' => firstNumber + secondNumber,
                '-' => firstNumber - secondNumber,
                '*' => firstNumber * secondNumber,
                '/' => firstNumber % secondNumber == 0 ? firstNumber / secondNumber : 0,
                '%' => firstNumber % secondNumber,
                var _ => 0
            };
        } while (solution >= header.BlockMinRange && solution <= header.BlockMaxRange);

        if (!TryGenerateHash(firstNumber, secondNumber, @operator, out var encryptedShare, out var encryptedShareHash))
        {
            poolShare = null;
            return false;
        }

        poolShare = new PoolShare
        {
            BlockHeight = header.BlockHeight,
            FirstNumber = firstNumber,
            SecondNumber = secondNumber,
            Operator = @operator,
            Solution = solution,
            EncryptedShare = encryptedShare,
            EncryptedShareHash = encryptedShareHash
        };

        return true;
    }

    public bool TryGenerateRandomPoolShare([MaybeNullWhen(false)] out PoolShare poolShare)
    {
        if (EasyBlockValues.Length < 256)
        {
            poolShare = null;
            return false;
        }
        
        var header = BlockHeaderResponse.BlockHeader;

        long firstNumber, secondNumber;
        char @operator;
        long solution;

        do
        {
            firstNumber = RandomNumberGeneratorUtility.GetBiasRandomBetween(header.BlockMinRange, header.BlockMaxRange);
        } while (EasyBlockValues.Contains(firstNumber));

        do
        {
            secondNumber = RandomNumberGeneratorUtility.GetBiasRandomBetween(header.BlockMinRange, header.BlockMaxRange);
        } while (EasyBlockValues.Contains(secondNumber));

        do
        {
            @operator = firstNumber > secondNumber ? Operators[RandomNumberGeneratorUtility.GetRandomBetween(0, Operators.Length - 1)] : Operators2[RandomNumberGeneratorUtility.GetRandomBetween(0, Operators2.Length - 1)];

            solution = @operator switch
            {
                '+' => firstNumber + secondNumber,
                '-' => firstNumber - secondNumber,
                '*' => firstNumber * secondNumber,
                '/' => firstNumber % secondNumber == 0 ? firstNumber / secondNumber : 0,
                '%' => firstNumber % secondNumber,
                var _ => 0
            };
        } while (solution >= header.BlockMinRange && solution <= header.BlockMaxRange);

        if (!TryGenerateHash(firstNumber, secondNumber, @operator, out var encryptedShare, out var encryptedShareHash))
        {
            poolShare = null;
            return false;
        }

        poolShare = new PoolShare
        {
            BlockHeight = header.BlockHeight,
            FirstNumber = firstNumber,
            SecondNumber = secondNumber,
            Operator = @operator,
            Solution = solution,
            EncryptedShare = encryptedShare,
            EncryptedShareHash = encryptedShareHash
        };

        return true;
    }

    [SkipLocalsInit]
    private bool TryGenerateHash(long firstNumber, long secondNumber, char op, out string encryptedShare, out string encryptedShareHash)
    {
        var header = BlockHeaderResponse.BlockHeader;
        Span<char> stringToEncrypt = stackalloc char[19 + 1 + 1 + 1 + 19 + 19];
        
        firstNumber.TryFormat(stringToEncrypt, out var firstNumberWritten);

        stringToEncrypt.GetRef(firstNumberWritten) = ' ';
        stringToEncrypt.GetRef(firstNumberWritten + 1) = op;
        stringToEncrypt.GetRef(firstNumberWritten + 2) = ' ';

        secondNumber.TryFormat(stringToEncrypt[(firstNumberWritten + 3)..], out var secondNumberWritten);
        header.BlockTimestampCreate.TryFormat(stringToEncrypt[(firstNumberWritten + 3 + secondNumberWritten)..], out var finalWritten);

        Span<byte> bytesToEncrypt = stackalloc byte[firstNumberWritten + 3 + secondNumberWritten + finalWritten];
        Encoding.ASCII.GetBytes(stringToEncrypt[..(firstNumberWritten + 3 + secondNumberWritten + finalWritten)], bytesToEncrypt);

        Span<byte> encryptedShareBytes = stackalloc byte[128];
        Span<byte> hashEncryptedShareBytes = stackalloc byte[128];

        if (!CpuMinerUtility.MakeEncryptedShare(bytesToEncrypt, encryptedShareBytes, hashEncryptedShareBytes, header.XorKey.Span, header.AesKey.Span, header.AesIv.Span, header.AesRound))
        {
            encryptedShare = string.Empty;
            encryptedShareHash = string.Empty;
            return false;
        }

        encryptedShare = Encoding.ASCII.GetString(encryptedShareBytes);
        encryptedShareHash = Encoding.ASCII.GetString(hashEncryptedShareBytes);

        return true;
    }
}