#include "xirorig_native.h"

int32_t computeSha3512Hash(const uint8_t *input, const size_t inputSize, uint8_t *output)
{
    EVP_MD_CTX *context = EVP_MD_CTX_new();

    if (context == NULL)
        return 0;

    const EVP_MD *sha3512 = EVP_sha3_512();

    if (sha3512 == NULL)
    {
        EVP_MD_CTX_free(context);
        return 0;
    }

    if (EVP_DigestInit_ex(context, sha3512, NULL) == 0)
    {
        EVP_MD_CTX_free(context);
        return 0;
    }

    if (EVP_DigestUpdate(context, input, inputSize) == 0)
    {
        EVP_MD_CTX_free(context);
        return 0;
    }

    if (EVP_DigestFinal_ex(context, output, NULL) == 0)
    {
        EVP_MD_CTX_free(context);
        return 0;
    }

    EVP_MD_CTX_free(context);

    return 1;
}

int32_t doNonceIvEasySquareMathMiningInstruction(
    const size_t pocShareNonceMaxSquareRetry,
    const size_t pocShareNonceNoSquareFoundShaRounds,
    const int64_t pocShareNonceMin,
    const int64_t pocShareNonceMax,
    const int64_t currentBlockHeight,
    uint8_t *pocShareIv,
    const size_t pocShareIvLength,
    const uint8_t *previousFinalBlockTransactionHashKey,
    const size_t previousFinalBlockTransactionHashKeyLength,
    const uint8_t *blockDifficulty,
    const size_t blockDifficultyLength
)
{
    size_t totalRetry = 0;
    int32_t newNonceGenerated = 0;
    int64_t newNonce = 0;

    const size_t minimumLength = pocShareIvLength + previousFinalBlockTransactionHashKeyLength + 8 + blockDifficultyLength;
    uint8_t *pocShareWorkToDoBytes = malloc(minimumLength);

    while (totalRetry < pocShareNonceMaxSquareRetry)
    {
        memcpy(pocShareWorkToDoBytes, pocShareIv, pocShareIvLength);
        memcpy(pocShareWorkToDoBytes + pocShareIvLength, blockDifficulty, blockDifficultyLength);

        size_t offset = pocShareIvLength + blockDifficultyLength;

        // Block Height
        *(pocShareWorkToDoBytes + offset++) = (uint8_t) (currentBlockHeight & 0xFF);
        *(pocShareWorkToDoBytes + offset++) = (uint8_t) ((currentBlockHeight & 0xFF00) >> 8);
        *(pocShareWorkToDoBytes + offset++) = (uint8_t) ((currentBlockHeight & 0xFF0000) >> 16);
        *(pocShareWorkToDoBytes + offset++) = (uint8_t) ((currentBlockHeight & 0xFF000000) >> 24);
        *(pocShareWorkToDoBytes + offset++) = (uint8_t) ((currentBlockHeight & 0xFF00000000) >> 32);
        *(pocShareWorkToDoBytes + offset++) = (uint8_t) ((currentBlockHeight & 0xFF0000000000) >> 40);
        *(pocShareWorkToDoBytes + offset++) = (uint8_t) ((currentBlockHeight & 0xFF000000000000) >> 48);
        *(pocShareWorkToDoBytes + offset++) = (uint8_t) (currentBlockHeight >> 56);

        memcpy(pocShareWorkToDoBytes + offset, previousFinalBlockTransactionHashKey, previousFinalBlockTransactionHashKeyLength);

        computeSha3512Hash(pocShareWorkToDoBytes, minimumLength, pocShareWorkToDoBytes);

        for (size_t i = 0; i < 64; i += 8)
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
            free(pocShareWorkToDoBytes);
            break;
        }

        computeSha3512Hash(pocShareIv, pocShareIvLength, pocShareIv);

        totalRetry++;
    }

    if (newNonceGenerated == 0)
    {
        for (size_t i = 0; i < pocShareNonceNoSquareFoundShaRounds; i++)
        {
            computeSha3512Hash(pocShareIv, pocShareIvLength, pocShareIv);
        }

        newNonce = (int64_t) *pocShareIv + ((int64_t) *(pocShareIv + 1) << 8) + ((int64_t) *(pocShareIv + 2) << 16) + ((int64_t) *(pocShareIv + 3) << 24);
    }

    if (newNonce >= pocShareNonceMin && newNonce <= pocShareNonceMax)
    {
        *pocShareIv++ = (uint8_t) (newNonce & 0xFF);
        *pocShareIv++ = (uint8_t) ((newNonce & 0xFF00) >> 8);
        *pocShareIv++ = (uint8_t) ((newNonce & 0xFF0000) >> 16);
        *pocShareIv++ = (uint8_t) ((newNonce & 0xFF000000) >> 24);
        *pocShareIv++ = (uint8_t) ((newNonce & 0xFF00000000) >> 32);
        *pocShareIv++ = (uint8_t) ((newNonce & 0xFF0000000000) >> 40);
        *pocShareIv++ = (uint8_t) ((newNonce & 0xFF000000000000) >> 48);
        *pocShareIv = (uint8_t) (newNonce >> 56);
        
        return 1;
    }

    return 0;
}

size_t getMaxCompressSize(const size_t inputSize)
{
    return LZ4_COMPRESSBOUND(inputSize);
}

int32_t doLz4CompressNonceIvMiningInstruction(const uint8_t *input, const size_t inputSize, uint8_t *output)
{
    const size_t compressMaxSize = LZ4_COMPRESSBOUND(inputSize);
    const size_t actualCompressSize = LZ4_compress_default(input, output, inputSize, compressMaxSize);
    
    if (actualCompressSize >= inputSize || actualCompressSize <= 0)
    {
        memcpy(output + 8, input, inputSize);
        
        *output++ = inputSize & 0xFF;
        *output++ = (inputSize >> 8) & 0xFF;
        *output++ = (inputSize >> 16) & 0xFF;
        *output++ = (inputSize >> 24) & 0xFF;
        
        *output++ = inputSize & 0xFF;
        *output++ = (inputSize >> 8) & 0xFF;
        *output++ = (inputSize >> 16) & 0xFF;
        *output = (inputSize >> 24) & 0xFF;
        
        return inputSize + 8;
    }
    else
    {
        memcpy(output + 8, output, actualCompressSize);
        
        *output++ = inputSize & 0xFF;
        *output++ = (inputSize >> 8) & 0xFF;
        *output++ = (inputSize >> 16) & 0xFF;
        *output++ = (inputSize >> 24) & 0xFF;
        
        *output++ = actualCompressSize & 0xFF;
        *output++ = (actualCompressSize >> 8) & 0xFF;
        *output++ = (actualCompressSize >> 16) & 0xFF;
        *output = (actualCompressSize >> 24) & 0xFF;
        
        return actualCompressSize + 8;
    }
}