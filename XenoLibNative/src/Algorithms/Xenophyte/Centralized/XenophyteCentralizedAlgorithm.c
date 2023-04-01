#include "XenophyteCentralizedAlgorithm.h"
#include "Utilities/MessageDigestUtility.h"
#include "Utilities/RandomNumberGeneratorUtility.h"
#include "Utilities/SymmetricAlgorithmUtility.h"

#define MAX_FLOAT_PRECISION 16777216
#define MAX_DOUBLE_PRECISION 9007199254740992

DOTNET_PRIVATE DOTNET_BYTE Base16Characters[] = {'0', '1', '2', '3', '4', '5', '6', '7', '8', '9', 'A', 'B', 'C', 'D', 'E', 'F'};

DOTNET_PRIVATE void ConvertByteArrayToHex(DOTNET_READ_ONLY_SPAN_BYTE input, DOTNET_INT inputLength, DOTNET_SPAN_BYTE output, DOTNET_BOOL withDash) {
    if (withDash) {
        {
            DOTNET_INT i = inputLength - 1;
            DOTNET_INT index = 3 * i;
            DOTNET_INT index2 = index + 1;

            output[index] = Base16Characters[input[i] >> 4];
            output[index2] = Base16Characters[input[i] & 15];
        }

        for (DOTNET_INT i = inputLength - 2; i >= 0; i--) {
            DOTNET_INT index = 3 * i;
            DOTNET_INT index2 = index + 1;
            DOTNET_INT index3 = index2 + 1;

            output[index] = Base16Characters[input[i] >> 4];
            output[index2] = Base16Characters[input[i] & 15];
            output[index3] = '-';
        }
    } else {
        for (DOTNET_INT i = inputLength - 1; i >= 0; i--) {
            DOTNET_INT index = 2 * i;
            DOTNET_INT index2 = index + 1;

            output[index] = Base16Characters[input[i] >> 4];
            output[index2] = Base16Characters[input[i] & 15];
        }
    }
}

DOTNET_PRIVATE void XorAndConvertByteArrayToHex(DOTNET_READ_ONLY_SPAN_BYTE input, DOTNET_INT inputLength, DOTNET_READ_ONLY_SPAN_BYTE xorKey, DOTNET_INT xorKeyLength, DOTNET_SPAN_BYTE output, DOTNET_BOOL withDash) {
    if (withDash) {
        {
            DOTNET_INT i = inputLength - 1;
            DOTNET_INT index = 3 * i;
            DOTNET_INT index2 = index + 1;

            output[index] = Base16Characters[input[i] >> 4] ^ xorKey[index % xorKeyLength];
            output[index2] = Base16Characters[input[i] & 15] ^ xorKey[index2 % xorKeyLength];
        }

        for (DOTNET_INT i = inputLength - 2; i >= 0; i--) {
            DOTNET_INT index = 3 * i;
            DOTNET_INT index2 = index + 1;
            DOTNET_INT index3 = index2 + 1;

            output[index] = Base16Characters[input[i] >> 4] ^ xorKey[index % xorKeyLength];
            output[index2] = Base16Characters[input[i] & 15] ^ xorKey[index2 % xorKeyLength];
            output[index3] = '-' ^ xorKey[index3 % xorKeyLength];
        }
    } else {
        for (DOTNET_INT i = inputLength - 1; i >= 0; i--) {
            DOTNET_INT index = 2 * i;
            DOTNET_INT index2 = index + 1;

            output[index] = Base16Characters[input[i] >> 4] ^ xorKey[index % xorKeyLength];
            output[index2] = Base16Characters[input[i] & 15] ^ xorKey[index2 % xorKeyLength];
        }
    }
}

inline DOTNET_PRIVATE DOTNET_FLOAT Max_Float(DOTNET_FLOAT a, DOTNET_FLOAT b) {
    return a > b ? a : b;
}

inline DOTNET_PRIVATE DOTNET_DOUBLE Max_Double(DOTNET_DOUBLE a, DOTNET_DOUBLE b) {
    return a > b ? a : b;
}

DOTNET_PUBLIC DOTNET_INT XenophyteCentralizedAlgorithm_GenerateEasyBlockNumbers(DOTNET_LONG minValue, DOTNET_LONG maxValue, DOTNET_SPAN_LONG output) {
    DOTNET_LONG range = maxValue - minValue + 1;

    if (range > 256) {
        if (range <= MAX_FLOAT_PRECISION) {
            for (DOTNET_INT i = 255; i >= 0; i--) {
                output[i] = minValue + (DOTNET_LONG) (Max_Float(0, (DOTNET_FLOAT) i / 255.0f - 0.0000001f) * (DOTNET_FLOAT) range);
            }
        } else if (range <= MAX_DOUBLE_PRECISION) {
            for (DOTNET_INT i = 255; i >= 0; i--) {
                output[i] = minValue + (DOTNET_LONG) (Max_Double(0, i / 255.0 - 0.00000000001) * (DOTNET_DOUBLE) range);
            }
        }
        else
        {
            return 0;
        }

        return 256;
    } else {
        DOTNET_INT index = 0;

        for (DOTNET_LONG i = minValue; i <= maxValue; i++) {
            output[index] = i;
            index += 1;
        }

        return index;
    }
}

DOTNET_PUBLIC DOTNET_INT XenophyteCentralizedAlgorithm_GenerateNonEasyBlockNumbers(DOTNET_LONG minValue, DOTNET_LONG maxValue, DOTNET_SPAN_LONG output, DOTNET_SPAN_LONG output2) {
    DOTNET_LONG easyBlockRange = maxValue - minValue + 1;

    if (easyBlockRange <= 256) {
        return 0;
    }

    DOTNET_LONG range = easyBlockRange - 256;

    if (range >= DOTNET_INT_MAX) {
        return 0;
    }

    DOTNET_INT amount = 0;
    DOTNET_INT easyBlockValuesIndex = 0;

    if (range <= MAX_FLOAT_PRECISION) {
        DOTNET_LONG value = minValue + (DOTNET_LONG) (Max_Float(0, (DOTNET_FLOAT) easyBlockValuesIndex / 255.0f - 0.0000001f) * (DOTNET_FLOAT) range);

        for (DOTNET_LONG i = minValue; i <= maxValue; i++) {
            if (i < value) {
                output[amount] = i;
                output2[amount] = i;
                amount += 1;
            } else {
                easyBlockValuesIndex += 1;
                value = minValue + (DOTNET_LONG) (Max_Float(0, (DOTNET_FLOAT) easyBlockValuesIndex / 255.0f - 0.0000001f) * (DOTNET_FLOAT) range);
            }
        }
    } else {
        DOTNET_LONG value = minValue + (DOTNET_LONG) (Max_Double(0, easyBlockValuesIndex / 255.0 - 0.00000000001) * (DOTNET_DOUBLE) range);

        for (DOTNET_LONG i = minValue; i <= maxValue; i++) {
            if (i < value) {
                output[amount] = i;
                output2[amount] = i;
                amount += 1;
            } else {
                easyBlockValuesIndex += 1;
                value = minValue + (DOTNET_LONG) (Max_Double(0, easyBlockValuesIndex / 255.0 - 0.00000000001) * (DOTNET_DOUBLE) range);
            }
        }
    }

    return amount;
}

DOTNET_PUBLIC DOTNET_BOOL XenophyteCentralizedAlgorithm_MakeEncryptedShare(DOTNET_READ_ONLY_SPAN_BYTE input, DOTNET_INT inputLength, DOTNET_SPAN_BYTE encryptedShare, DOTNET_SPAN_BYTE hashEncryptedShare, DOTNET_READ_ONLY_SPAN_BYTE xorKey, DOTNET_INT xorKeyLength, DOTNET_INT aesKeySize, DOTNET_READ_ONLY_SPAN_BYTE aesKey, DOTNET_READ_ONLY_SPAN_BYTE aesIv, DOTNET_INT aesRound) {
    // First encryption phase convert to hex and xor each result.

    DOTNET_INT firstOutputLength = inputLength * 2;
    DOTNET_BYTE firstOutput[firstOutputLength];

    XorAndConvertByteArrayToHex(input, inputLength, xorKey, xorKeyLength, firstOutput, DOTNET_FALSE);

    // Second encryption phase: run through aes per round and apply xor at the final round.

    DOTNET_INT secondInputLength = firstOutputLength;
    DOTNET_INT secondOutputLength = firstOutputLength;

    for (DOTNET_INT i = aesRound; i >= 0; i--) {
        secondOutputLength += SymmetricAlgorithmUtility_GetPaddedLength(secondOutputLength);
        secondOutputLength = secondOutputLength * 2 + (secondOutputLength - 1);
    }

    DOTNET_BYTE secondOutput[secondOutputLength];
    memcpy(secondOutput, firstOutput, secondInputLength);

    for (DOTNET_INT i = aesRound; i >= 0; i--) {
        if (i == 1) {
            switch (aesKeySize) {
                case 128:
                    secondInputLength = SymmetricAlgorithmUtility_Encrypt_AES_128_CBC(aesKey, aesIv, secondOutput, secondInputLength, secondOutput, 1);
                    break;

                case 192:
                    secondInputLength = SymmetricAlgorithmUtility_Encrypt_AES_192_CBC(aesKey, aesIv, secondOutput, secondInputLength, secondOutput, 1);
                    break;

                case 256:
                    secondInputLength = SymmetricAlgorithmUtility_Encrypt_AES_256_CBC(aesKey, aesIv, secondOutput, secondInputLength, secondOutput, 1);
                    break;

                default:
                    return DOTNET_FALSE;
            }

            if (secondInputLength == 0) {
                return DOTNET_FALSE;
            }

            DOTNET_INT tempSize = secondInputLength * 2 + (secondInputLength - 1);
            DOTNET_BYTE temp[tempSize];

            XorAndConvertByteArrayToHex(secondOutput, secondInputLength, xorKey, xorKeyLength, temp, DOTNET_TRUE);

            memcpy(secondOutput, temp, tempSize);
            secondInputLength = tempSize;
        } else {
            switch (aesKeySize) {
                case 128:
                    secondInputLength = SymmetricAlgorithmUtility_Encrypt_AES_128_CBC(aesKey, aesIv, secondOutput, secondInputLength, secondOutput, 1);
                    break;

                case 192:
                    secondInputLength = SymmetricAlgorithmUtility_Encrypt_AES_192_CBC(aesKey, aesIv, secondOutput, secondInputLength, secondOutput, 1);
                    break;

                case 256:
                    secondInputLength = SymmetricAlgorithmUtility_Encrypt_AES_256_CBC(aesKey, aesIv, secondOutput, secondInputLength, secondOutput, 1);
                    break;

                default:
                    return DOTNET_FALSE;
            }

            if (secondInputLength == 0) {
                return DOTNET_FALSE;
            }

            DOTNET_INT tempSize = secondInputLength * 2 + (secondInputLength - 1);
            DOTNET_BYTE temp[tempSize];

            ConvertByteArrayToHex(secondOutput, secondInputLength, temp, DOTNET_TRUE);

            memcpy(secondOutput, temp, tempSize);
            secondInputLength = tempSize;
        }
    }

    // Third encryption phase: compute hash
    DOTNET_BYTE thirdOutput[64];

    if (!MessageDigestUtility_ComputeSha2_512Hash(secondOutput, secondOutputLength, thirdOutput)) {
        return DOTNET_FALSE;
    }

    ConvertByteArrayToHex(thirdOutput, 64, encryptedShare, DOTNET_FALSE);

    if (!MessageDigestUtility_ComputeSha2_512Hash(encryptedShare, 64 * 2, thirdOutput)) {
        return DOTNET_FALSE;
    }

    ConvertByteArrayToHex(thirdOutput, 64, hashEncryptedShare, DOTNET_FALSE);

    return DOTNET_TRUE;
}
