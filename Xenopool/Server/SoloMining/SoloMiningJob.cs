using System.Runtime.CompilerServices;
using System.Text;
using Google.Protobuf;
using Xenolib.Algorithms.Xenophyte.Centralized.Networking.Pool;
using Xenolib.Algorithms.Xenophyte.Centralized.Utilities;
using Xenolib.Utilities;
using Xenopool.Server.Pool;
using BlockHeader = Xenolib.Algorithms.Xenophyte.Centralized.Networking.Solo.BlockHeader;

namespace Xenopool.Server.SoloMining;

public sealed class SoloMiningJob
{
    public BlockHeaderResponse BlockHeaderResponse { get; }

    public Span<long> EasyBlockValues => _easyBlockValues.AsSpan(0, _easyBlockValuesLength);

    private static readonly char[] Operators = { '+', '-', '*', '/', '%' };
    private static readonly char[] Operators2 = { '+', '*', '%' };

    private readonly BlockHeader _blockHeader;

    private readonly long[] _easyBlockValues = new long[256];
    private readonly int _easyBlockValuesLength;

    public SoloMiningJob(BlockHeader blockHeader)
    {
        _blockHeader = blockHeader;

        BlockHeaderResponse = new BlockHeaderResponse
        {
            Status = true,
            Header = new Xenolib.Algorithms.Xenophyte.Centralized.Networking.Pool.BlockHeader
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
    
    public bool TryGenerateSemiRandomPoolShare(out PoolShare poolShare)
    {
        long firstNumber, secondNumber;
        char @operator;
        long solution;

        if (RandomNumberGeneratorUtility.GetRandomBetween(1, 100) <= 50)
        {
            firstNumber = EasyBlockValues[RandomNumberGeneratorUtility.GetRandomBetween(0, 255)];

            do
            {
                secondNumber = RandomNumberGeneratorUtility.GetBiasRandomBetween(_blockHeader.BlockMinRange, _blockHeader.BlockMaxRange);
            } while (EasyBlockValues.Contains(secondNumber));
        }
        else
        {
            secondNumber = EasyBlockValues[RandomNumberGeneratorUtility.GetRandomBetween(0, 255)];

            do
            {
                firstNumber = RandomNumberGeneratorUtility.GetBiasRandomBetween(_blockHeader.BlockMinRange, _blockHeader.BlockMaxRange);
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
        } while (solution >= _blockHeader.BlockMinRange && solution <= _blockHeader.BlockMaxRange);

        if (!TryGenerateHash(firstNumber, secondNumber, @operator, out var encryptedShare, out var encryptedShareHash))
        {
            poolShare = default;
            return false;
        }

        poolShare = new PoolShare
        {
            BlockHeight = _blockHeader.BlockHeight,
            FirstNumber = firstNumber,
            SecondNumber = secondNumber,
            Operator = @operator,
            Solution = solution,
            EncryptedShare = encryptedShare,
            EncryptedShareHash = encryptedShareHash
        };

        return true;
    }
    
    public bool TryGenerateRandomPoolShare(out PoolShare poolShare)
    {
        long firstNumber, secondNumber;
        char @operator;
        long solution;

        do
        {
            firstNumber = RandomNumberGeneratorUtility.GetBiasRandomBetween(_blockHeader.BlockMinRange, _blockHeader.BlockMaxRange);
        } while (EasyBlockValues.Contains(firstNumber));
            
        do
        {
            secondNumber = RandomNumberGeneratorUtility.GetBiasRandomBetween(_blockHeader.BlockMinRange, _blockHeader.BlockMaxRange);
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
        } while (solution >= _blockHeader.BlockMinRange && solution <= _blockHeader.BlockMaxRange);

        if (!TryGenerateHash(firstNumber, secondNumber, @operator, out var encryptedShare, out var encryptedShareHash))
        {
            poolShare = default;
            return false;
        }

        poolShare = new PoolShare
        {
            BlockHeight = _blockHeader.BlockHeight,
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
        Span<char> stringToEncrypt = stackalloc char[19 + 1 + 1 + 1 + 19 + 19 + 1];

        firstNumber.TryFormat(stringToEncrypt, out var firstNumberWritten);

        stringToEncrypt.GetRef(firstNumberWritten) = ' ';
        stringToEncrypt.GetRef(firstNumberWritten + 1) = op;
        stringToEncrypt.GetRef(firstNumberWritten + 2) = ' ';

        secondNumber.TryFormat(stringToEncrypt[(firstNumberWritten + 3)..], out var secondNumberWritten);
        _blockHeader.BlockTimestampCreate.TryFormat(stringToEncrypt[(firstNumberWritten + 3 + secondNumberWritten)..], out var finalWritten);

        Span<byte> bytesToEncrypt = stackalloc byte[firstNumberWritten + 3 + secondNumberWritten + finalWritten];
        Encoding.ASCII.GetBytes(stringToEncrypt[..(firstNumberWritten + 3 + secondNumberWritten + finalWritten)], bytesToEncrypt);

        Span<byte> encryptedShareBytes = stackalloc byte[64 * 2];
        Span<byte> hashEncryptedShareBytes = stackalloc byte[64 * 2];

        if (!CpuMinerUtility.MakeEncryptedShare(bytesToEncrypt, encryptedShareBytes, hashEncryptedShareBytes, _blockHeader.XorKey, _blockHeader.AesKey, _blockHeader.AesIv, _blockHeader.AesRound))
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