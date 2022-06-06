#include <stdlib.h>
#include <string.h>

#include "lz4.h"
#include "openssl/evp.h"

#include "XirophtDecentralizedAlgorithm.h"

void XirophtDecentralizedAlgorithm_Solo_GeneratePocRandomData(
    uint8_t *pocRandomData,
    const RandomDataShareNumberType randomNumber,
    const RandomDataShareNumberType randomNumber2,
    const RandomDataShareTimestampType timestamp,
    const int32_t checksumSize,
    const uint8_t *walletAddress,
    const int32_t walletAddressSize,
    const RandomDataShareBlockHeightType blockHeight,
    const RandomDataShareNonceType nonce
)
{
    // randomNumber
    memcpy(pocRandomData, &randomNumber, sizeof randomNumber);

    // randomNumber2
    memcpy(pocRandomData + sizeof randomNumber, &randomNumber2, sizeof randomNumber2);

    // timestamp
    memcpy(pocRandomData + sizeof randomNumber + sizeof randomNumber2, &timestamp, sizeof timestamp);

    // walletAddress
    memcpy(pocRandomData + sizeof randomNumber + sizeof randomNumber2 + sizeof timestamp + checksumSize, walletAddress, walletAddressSize);

    // blockHeight
    memcpy(pocRandomData + sizeof randomNumber + sizeof randomNumber2 + sizeof timestamp + checksumSize + walletAddressSize, &blockHeight, sizeof blockHeight);

    // nonce
    memcpy(pocRandomData + sizeof randomNumber + sizeof randomNumber2 + sizeof timestamp + checksumSize + walletAddressSize + sizeof blockHeight, &nonce, sizeof nonce);
}

void XirophtDecentralizedAlgorithm_Solo_UpdatePocRandomData(
    uint8_t *pocRandomData,
    const RandomDataShareTimestampType timestamp,
    const int32_t checksumSize,
    const int32_t walletAddressSize,
    const RandomDataShareNonceType nonce
)
{
    // timestamp
    memcpy(pocRandomData + sizeof(RandomDataShareNumberType) + sizeof(RandomDataShareNumberType), &timestamp, sizeof timestamp);

    // nonce
    memcpy(pocRandomData + sizeof(RandomDataShareNumberType) + sizeof(RandomDataShareNumberType) + sizeof timestamp + checksumSize + walletAddressSize + sizeof(RandomDataShareBlockHeightType), &nonce, sizeof nonce);
}

void XirophtDecentralizedAlgorithm_Solo_DoNonceIvMiningInstruction(uint8_t *pocShareIv, int32_t *pocShareIvSize, const int32_t pocRoundShaNonce)
{
    for (int32_t i = pocRoundShaNonce - 1; i >= 0; --i)
    {
        Sha3Utility_TryComputeSha512Hash(pocShareIv, *pocShareIvSize, pocShareIv, (uint32_t*) pocShareIvSize);
    }
}

void XirophtDecentralizedAlgorithm_Solo_DoNonceIvXorMiningInstruction(uint8_t *pocShareIv, const int32_t pocShareIvSize)
{
    const div_t divResult = div(pocShareIvSize, 2);

    for (int32_t i = divResult.quot - 1; i >= 0; --i)
    {
        const uint8_t value = *(pocShareIv + i) ^ *(pocShareIv + pocShareIvSize - 1 - i);

        *(pocShareIv + i) = value;
        *(pocShareIv + pocShareIvSize - 1 - i) = value;
    }

    if (divResult.rem != 0)
    {
        *(pocShareIv + divResult.quot) = 0;
    }
}

int32_t XirophtDecentralizedAlgorithm_Solo_DoNonceIvEasySquareMathMiningInstruction(
    const int32_t pocShareNonceMaxSquareRetry,
    const int32_t pocShareNonceNoSquareFoundShaRounds,
    const int64_t pocShareNonceMin,
    const int64_t pocShareNonceMax,
    const RandomDataShareBlockHeightType blockHeight,
    uint8_t *pocShareIv,
    int32_t *pocShareIvSize,
    uint8_t *pocShareWorkToDoBytes,
    const uint8_t *blockDifficulty,
    const int32_t blockDifficultyLength,
    const uint8_t *previousFinalBlockTransactionHashKey,
    const int32_t previousFinalBlockTransactionHashKeyLength
)
{
    int32_t totalRetry = 0;
    int32_t newNonceGenerated = 0;
    int64_t newNonce = 0;

    while (totalRetry < pocShareNonceMaxSquareRetry)
    {
        memcpy(pocShareWorkToDoBytes, pocShareIv, *pocShareIvSize);
        memcpy(pocShareWorkToDoBytes + *pocShareIvSize, blockDifficulty, blockDifficultyLength);
        memcpy(pocShareWorkToDoBytes + *pocShareIvSize + blockDifficultyLength, &blockHeight, sizeof blockHeight);
        memcpy(pocShareWorkToDoBytes + *pocShareIvSize + blockDifficultyLength + sizeof blockHeight, previousFinalBlockTransactionHashKey, previousFinalBlockTransactionHashKeyLength);

        Sha3Utility_TryComputeSha512Hash(pocShareWorkToDoBytes, *pocShareIvSize + blockDifficultyLength + sizeof blockHeight + previousFinalBlockTransactionHashKeyLength, pocShareWorkToDoBytes, NULL);

        for (int32_t i = 0; i < 64; i += 8)
        {
            const int32_t x1 = *(pocShareWorkToDoBytes + i) + (*(pocShareWorkToDoBytes + i + 1) << 8);
            const int32_t y1 = *(pocShareWorkToDoBytes + i + 1) + (*(pocShareWorkToDoBytes + i) << 8);

            const int32_t x2 = *(pocShareWorkToDoBytes + i + 2) + (*(pocShareWorkToDoBytes + i + 3) << 8);
            const int32_t y2 = *(pocShareWorkToDoBytes + i + 3) + (*(pocShareWorkToDoBytes + i + 2) << 8);

            const int32_t x3 = *(pocShareWorkToDoBytes + i + 4) + (*(pocShareWorkToDoBytes + i + 5) << 8);
            const int32_t y3 = *(pocShareWorkToDoBytes + i + 5) + (*(pocShareWorkToDoBytes + i + 4) << 8);

            const int32_t x4 = *(pocShareWorkToDoBytes + i + 6) + (*(pocShareWorkToDoBytes + i + 7) << 8);
            const int32_t y4 = *(pocShareWorkToDoBytes + i + 7) + (*(pocShareWorkToDoBytes + i + 6) << 8);

            if (abs(y2 - y1) == abs(x3 - x1) && abs(x2 - x1) == abs(y3 - y1) && abs(y2 - y4) == abs(x3 - x4) && abs(x2 - x4) == abs(y3 - y4) ||
                abs(y2 - y1) == abs(x4 - x1) && abs(x2 - x1) == abs(y4 - y3) && abs(y2 - y3) == abs(x4 - x3) && abs(x2 - x3) == abs(y4 - y3) ||
                abs(y3 - y1) == abs(x4 - x1) && abs(x3 - x1) == abs(y4 - y1) && abs(y3 - y2) == abs(x4 - x2) && abs(x3 - x2) == abs(y4 - y2))
            {
                newNonce = (int64_t) (*(pocShareWorkToDoBytes + i) + *(pocShareWorkToDoBytes + i) & 0xFF) + ((int64_t) (*(pocShareWorkToDoBytes + i + 2) + *(pocShareWorkToDoBytes + i + 2) & 0xFF) << 8) + ((int64_t) (*(pocShareWorkToDoBytes + i + 4) + *(pocShareWorkToDoBytes + i + 4) & 0xFF) << 16) + ((int64_t) (*(pocShareWorkToDoBytes + i + 6) + *(pocShareWorkToDoBytes + i + 6) & 0xFF) << 24);
                newNonceGenerated = 1;
                break;
            }
        }

        if (newNonceGenerated)
        {
            break;
        }

        Sha3Utility_TryComputeSha512Hash(pocShareIv, *pocShareIvSize, pocShareIv, (uint32_t*) pocShareIvSize);
        totalRetry++;
    }

    if (newNonceGenerated == 0)
    {
        for (int32_t i = pocShareNonceNoSquareFoundShaRounds - 1; i >= 0; --i)
        {
            Sha3Utility_TryComputeSha512Hash(pocShareIv, *pocShareIvSize, pocShareIv, (uint32_t*) pocShareIvSize);
        }

        memcpy(&newNonce, pocShareIv, sizeof(uint32_t));
    }

    if (newNonce < pocShareNonceMin || newNonce > pocShareNonceMax)
    {
        return 0;
    }

    memcpy(pocShareIv, &newNonce, sizeof(int64_t));
    *pocShareIvSize = sizeof(int64_t);

    return 1;
}

void XirophtDecentralizedAlgorithm_Solo_DoLz4CompressNonceIvMiningInstruction(uint8_t *pocShareIv, int32_t *pocShareIvSize)
{
    const int32_t compressMaxSize = LZ4_COMPRESSBOUND(*pocShareIvSize);
    uint8_t *output = pocShareIv + *pocShareIvSize;
    const int32_t actualCompressSize = LZ4_compress_default((const char*) pocShareIv, (char*) output, *pocShareIvSize, compressMaxSize);

    if (actualCompressSize >= *pocShareIvSize || actualCompressSize <= 0)
    {
        memcpy(output, pocShareIvSize, sizeof(int32_t));
        memcpy(output + 4, pocShareIvSize, sizeof(int32_t));
        memcpy(output + 8, pocShareIv, *pocShareIvSize);

        *pocShareIvSize = *pocShareIvSize + 8;

        memmove(pocShareIv, output, *pocShareIvSize);
        return;
    }

    memmove(output + 8, output, actualCompressSize);
    memcpy(output, pocShareIvSize, sizeof(int32_t));
    memcpy(output + 4, &actualCompressSize, sizeof(int32_t));

    *pocShareIvSize = actualCompressSize + 8;
    memmove(pocShareIv, output, *pocShareIvSize);
}

int32_t XirophtDecentralizedAlgorithm_Solo_DoNonceIvIterationsMiningInstruction(
    uint8_t *pocShareIv,
    int32_t *pocShareIvSize,
    const uint8_t *blockchainMarkKey,
    const int32_t blockchainMarkKeySize,
    const int32_t pocShareNonceIvIteration,
    const int32_t keyLength
)
{
    const int32_t passwordLength = *pocShareIvSize;

    if (PKCS5_PBKDF2_HMAC_SHA1((const char*) pocShareIv, passwordLength, blockchainMarkKey, blockchainMarkKeySize, pocShareNonceIvIteration, keyLength, pocShareIv) == 1)
    {
        *pocShareIvSize = keyLength;
        return 1;
    }

    return 0;
}

int32_t XirophtDecentralizedAlgorithm_Solo_DoEncryptedPocShareMiningInstruction(
    const uint8_t *key,
    const uint8_t *iv,
    const int32_t iterations,
    uint8_t *data,
    int32_t *dataLength
)
{
    EVP_CIPHER_CTX *ctx = EVP_CIPHER_CTX_new();

    if (ctx == NULL)
    {
        return 0;
    }

    memcpy(data + *dataLength, data, *dataLength);
    uint8_t *newData = data + *dataLength;

    for (int32_t i = iterations - 1; i >= 0; --i)
    {
        if (EVP_CIPHER_CTX_set_padding(ctx, 0) == 0)
        {
            EVP_CIPHER_CTX_free(ctx);
            return 0;
        }

        if (EVP_EncryptInit_ex(ctx, EVP_aes_256_cfb128(), NULL, key, iv) == 0)
        {
            EVP_CIPHER_CTX_free(ctx);
            return 0;
        }

        const uint8_t paddingSizeRequired = 16 - *dataLength % 16;

        for (int j = paddingSizeRequired - 1; j >= 0; --j)
        {
            *(newData + *dataLength + j) = paddingSizeRequired;
        }

        *dataLength = *dataLength + paddingSizeRequired;

        int32_t updateLength = 0;

        if (EVP_EncryptUpdate(ctx, newData, &updateLength, newData, *dataLength) == 0)
        {
            EVP_CIPHER_CTX_free(ctx);
            return 0;
        }

        int32_t finalLength = 0;

        if (EVP_EncryptFinal_ex(ctx, newData + updateLength, &finalLength) == 0)
        {
            EVP_CIPHER_CTX_free(ctx);
            return 0;
        }

        *dataLength = updateLength + finalLength;

        if (EVP_CIPHER_CTX_reset(ctx) == 0)
        {
            EVP_CIPHER_CTX_free(ctx);
            return 0;
        }
    }

    EVP_CIPHER_CTX_free(ctx);

    return 1;
}
