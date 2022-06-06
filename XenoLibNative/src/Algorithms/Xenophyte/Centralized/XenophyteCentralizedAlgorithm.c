#include "XenophyteCentralizedAlgorithm.h"
#include "Utilities/MessageDigestUtility.h"
#include "Utilities/RandomNumberGeneratorUtility.h"
#include "Utilities/SymmetricAlgorithmUtility.h"

#define MAX_FLOAT_PRECISION 16777216
#define MAX_DOUBLE_PRECISION 9007199254740992

DOTNET_PRIVATE DOTNET_BYTE CertificateSupportedCharacters[] = {
    'A', 'B', 'C', 'D', 'E', 'F', 'G', 'H', 'I', 'J', 'K', 'L', 'M', 'N', 'O', 'P', 'Q', 'R', 'S', 'T', 'U', 'V', 'W', 'X', 'Y', 'Z',
    'a', 'b', 'c', 'd', 'e', 'f', 'g', 'h', 'i', 'j', 'k', 'l', 'm', 'n', 'o', 'p', 'q', 'r', 's', 't', 'u', 'v', 'w', 'x', 'y', 'z',
    '0', '1', '2', '3', '4', '5', '6', '7', '8', '9',
    '&', '~', '#', '@', '\'', '(', '\\', ')', '='};

DOTNET_PRIVATE DOTNET_BYTE Base16Characters[] = {'0', '1', '2', '3', '4', '5', '6', '7', '8', '9', 'A', 'B', 'C', 'D', 'E', 'F'};

DOTNET_PRIVATE void ConvertByteArrayToHex(DOTNET_READ_ONLY_SPAN_BYTE input, DOTNET_INT inputLength, DOTNET_SPAN_BYTE output, DOTNET_BOOL withDash) {
    if (withDash) {
        for (DOTNET_INT i = inputLength - 1; i >= 0; i--) {
            output[3 * i] = Base16Characters[input[i] >> 4];
            output[3 * i + 1] = Base16Characters[input[i] & 15];

            if (i == inputLength - 1) {
                continue;
            }

            output[3 * i + 2] = '-';
        }
    } else {
        for (DOTNET_INT i = inputLength - 1; i >= 0; i--) {
            output[2 * i] = Base16Characters[input[i] >> 4];
            output[2 * i + 1] = Base16Characters[input[i] & 15];
        }
    }
}

DOTNET_PRIVATE void XorAndConvertByteArrayToHex(DOTNET_READ_ONLY_SPAN_BYTE input, DOTNET_INT inputLength, DOTNET_READ_ONLY_SPAN_BYTE xorKey, DOTNET_INT xorKeyLength, DOTNET_SPAN_BYTE output, DOTNET_BOOL withDash) {
    if (withDash) {
        for (DOTNET_INT i = inputLength - 1; i >= 0; i--) {
            output[3 * i] = Base16Characters[input[i] >> 4] ^ xorKey[(3 * i) % xorKeyLength];
            output[3 * i + 1] = Base16Characters[input[i] & 15] ^ xorKey[(3 * i + 1) % xorKeyLength];

            if (i == inputLength - 1) {
                continue;
            }

            output[3 * i + 2] = '-' ^ xorKey[(3 * i + 2) % xorKeyLength];
        }
    } else {
        for (DOTNET_INT i = inputLength - 1; i >= 0; i--) {
            output[2 * i] = Base16Characters[input[i] >> 4] ^ xorKey[(2 * i) % xorKeyLength];
            output[2 * i + 1] = Base16Characters[input[i] & 15] ^ xorKey[(2 * i + 1) % xorKeyLength];
        }
    }
}

DOTNET_PRIVATE DOTNET_FLOAT Max_Float(DOTNET_FLOAT a, DOTNET_FLOAT b) {
    return a > b ? a : b;
}

DOTNET_PRIVATE DOTNET_DOUBLE Max_Double(DOTNET_DOUBLE a, DOTNET_DOUBLE b) {
    return a > b ? a : b;
}

void XenophyteCentralizedAlgorithm_GenerateCertificate(DOTNET_STRING header, DOTNET_INT keySize, DOTNET_SPAN_BYTE output) {
    DOTNET_INT headerSize = strlen(header);
    memcpy(output, header, headerSize);

    for (DOTNET_INT i = keySize - 1; i >= 0; i--) {
        output[i + headerSize] = CertificateSupportedCharacters[RandomNumberGeneratorUtility_GetRandomBetween_Int(0, sizeof(CertificateSupportedCharacters) - 1)];
    }
}

DOTNET_INT XenophyteCentralizedAlgorithm_GenerateEasyBlockNumbers(DOTNET_LONG minValue, DOTNET_LONG maxValue, DOTNET_SPAN_LONG output) {
    DOTNET_LONG range = maxValue - minValue + 1;

    if (range <= 256) {
        DOTNET_INT index = 0;

        for (DOTNET_INT i = minValue; i <= maxValue; i++) {
            output[index++] = i;
        }

        return index;
    } else {
        for (DOTNET_INT i = 255; i >= 0; i--) {
            if (range <= MAX_FLOAT_PRECISION) {
                output[i] = minValue + (DOTNET_LONG) Max_Float(0, i / 255.0f - 0.0000001f);
            } else {
                output[i] = minValue + (DOTNET_LONG) Max_Double(0, i / 255.0 - 0.00000000001);
            }
        }

        return 256;
    }
}

DOTNET_INT XenophyteCentralizedAlgorithm_GenerateNonEasyBlockNumbers(DOTNET_LONG minValue, DOTNET_LONG maxValue, DOTNET_READ_ONLY_SPAN_LONG easyBlockValues, DOTNET_INT easyBlockValuesLength, DOTNET_SPAN_LONG output) {
    DOTNET_LONG range = maxValue - minValue + 1 - easyBlockValuesLength;

    if (range == 0) {
        return 0;
    }

    DOTNET_INT amount = 0;
    DOTNET_INT easyBlockValuesIndex = 0;

    for (DOTNET_LONG i = minValue; i <= maxValue; i++) {
        if (i < easyBlockValues[easyBlockValuesIndex]) {
            output[amount++] = i;
        } else {
            easyBlockValuesIndex++;
        }
    }

    return amount;
}

DOTNET_BOOL XenophyteCentralizedAlgorithm_MakeEncryptedShare(DOTNET_READ_ONLY_SPAN_BYTE input, DOTNET_INT inputLength, DOTNET_SPAN_BYTE encryptedShare, DOTNET_SPAN_BYTE hashEncryptedShare, DOTNET_READ_ONLY_SPAN_BYTE xorKey, DOTNET_INT xorKeyLength, DOTNET_INT aesKeySize, DOTNET_READ_ONLY_SPAN_BYTE aesKey, DOTNET_READ_ONLY_SPAN_BYTE aesIv, DOTNET_INT aesRound) {
    // First encryption phase convert to hex and xor each result.

    DOTNET_INT firstOutputLength = inputLength * 2;
    DOTNET_BYTE_ARRAY firstOutput = malloc(firstOutputLength);

    if (firstOutput == NULL) {
        return DOTNET_FALSE;
    }

    XorAndConvertByteArrayToHex(input, inputLength, xorKey, xorKeyLength, firstOutput, DOTNET_FALSE);

    // Second encryption phase: run through aes per round and apply xor at the final round.

    DOTNET_INT secondInputLength = firstOutputLength;
    DOTNET_INT secondInputPaddedLength = firstOutputLength + SymmetricAlgorithmUtility_GetPaddedLength(firstOutputLength);
    DOTNET_INT secondOutputLength = secondInputPaddedLength * 2 + (secondInputPaddedLength - 1);

    for (DOTNET_INT i = aesRound - 1; i >= 0; i--) {
        DOTNET_INT temp = secondOutputLength + SymmetricAlgorithmUtility_GetPaddedLength(secondOutputLength);
        secondOutputLength = temp * 2 + (temp - 1);
    }

    DOTNET_BYTE_ARRAY secondOutput = malloc(secondOutputLength);

    if (secondOutput == NULL) {
        return DOTNET_FALSE;
    }

    memcpy(secondOutput, firstOutput, firstOutputLength);
    free(firstOutput);

    DOTNET_INT bytesWritten;
    DOTNET_INT tempSize;

    for (DOTNET_INT i = aesRound; i >= 0; i--) {
        if (i == 1) {
            switch (aesKeySize) {
                case 128:
                    bytesWritten = SymmetricAlgorithmUtility_Encrypt_AES_128_CBC(aesKey, aesIv, secondOutput, secondInputLength, secondOutput, 1);
                    break;

                case 192:
                    bytesWritten = SymmetricAlgorithmUtility_Encrypt_AES_192_CBC(aesKey, aesIv, secondOutput, secondInputLength, secondOutput, 1);
                    break;

                case 256:
                    bytesWritten = SymmetricAlgorithmUtility_Encrypt_AES_256_CBC(aesKey, aesIv, secondOutput, secondInputLength, secondOutput, 1);
                    break;

                default:
                    return DOTNET_FALSE;
            }

            if (bytesWritten == 0) {
                return DOTNET_FALSE;
            }

            tempSize = bytesWritten * 2 + (bytesWritten - 1);
            DOTNET_BYTE_ARRAY temp = malloc(tempSize);

            if (temp == NULL) {
                return DOTNET_FALSE;
            }

            XorAndConvertByteArrayToHex(secondOutput, bytesWritten, xorKey, xorKeyLength, temp, DOTNET_TRUE);

            memcpy(secondOutput, temp, tempSize);
            free(temp);

            secondInputLength = tempSize;
        } else {
            switch (aesKeySize) {
                case 128:
                    bytesWritten = SymmetricAlgorithmUtility_Encrypt_AES_128_CBC(aesKey, aesIv, secondOutput, secondInputLength, secondOutput, 1);
                    break;

                case 192:
                    bytesWritten = SymmetricAlgorithmUtility_Encrypt_AES_192_CBC(aesKey, aesIv, secondOutput, secondInputLength, secondOutput, 1);
                    break;

                case 256:
                    bytesWritten = SymmetricAlgorithmUtility_Encrypt_AES_256_CBC(aesKey, aesIv, secondOutput, secondInputLength, secondOutput, 1);
                    break;

                default:
                    return DOTNET_FALSE;
            }

            if (bytesWritten == 0) {
                return DOTNET_FALSE;
            }

            tempSize = bytesWritten * 2 + (bytesWritten - 1);
            DOTNET_BYTE_ARRAY temp = malloc(tempSize);

            if (temp == NULL) {
                return DOTNET_FALSE;
            }

            ConvertByteArrayToHex(secondOutput, bytesWritten, temp, DOTNET_TRUE);

            memcpy(secondOutput, temp, tempSize);
            free(temp);

            secondInputLength = tempSize;
        }
    }

    // Third encryption phase: compute hash
    DOTNET_BYTE_ARRAY thirdOutput = malloc(64);

    if (thirdOutput == NULL) {
        return DOTNET_FALSE;
    }

    if (!MessageDigestUtility_ComputeSha2_512Hash(secondOutput, secondOutputLength, thirdOutput)) {
        return DOTNET_FALSE;
    }

    free(secondOutput);

    for (DOTNET_INT i = 64 - 1; i >= 0; i--) {
        encryptedShare[2 * i] = Base16Characters[thirdOutput[i] >> 4];
        encryptedShare[2 * i + 1] = Base16Characters[thirdOutput[i] & 15];
    }

    free(thirdOutput);

    DOTNET_BYTE_ARRAY finalOutput = malloc(64);

    if (finalOutput == NULL) {
        return DOTNET_FALSE;
    }

    if (!MessageDigestUtility_ComputeSha2_512Hash(encryptedShare, 64 * 2, finalOutput)) {
        return DOTNET_FALSE;
    }

    for (DOTNET_INT i = 64 - 1; i >= 0; i--) {
        hashEncryptedShare[2 * i] = Base16Characters[finalOutput[i] >> 4];
        hashEncryptedShare[2 * i + 1] = Base16Characters[finalOutput[i] & 15];
    }

    free(finalOutput);

    return DOTNET_TRUE;
}
