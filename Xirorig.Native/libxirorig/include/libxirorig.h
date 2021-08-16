#pragma once

#include <openssl/evp.h>

#if (_MSC_VER >= 1900)
#define EXPORT __declspec(dllexport)  
#else
#define EXPORT __attribute__((visibility("default")))  
#endif

/**
 * \brief Compute SHA-3 512 hash.
 * \param input Input array
 * \param inputSize The size of the input array.
 * \param output The output array.
 * \return Returns 1 for success and 0 for failure.
 */
EXPORT int32_t computeSha3512Hash(const uint8_t* input, const size_t inputSize, uint8_t* output);

/**
 * \brief Do Nonce Iv Easy Square Math Mining instructions.
 */
EXPORT int32_t doNonceIvEasySquareMathMiningInstruction(
    const size_t pocShareNonceMaxSquareRetry,
    const size_t pocShareNonceNoSquareFoundShaRounds,
    const int64_t pocShareNonceMin,
    const int64_t pocShareNonceMax,
    const int64_t currentBlockHeight,
    uint8_t* pocShareIv,
    const size_t pocShareIvLength,
    const uint8_t* previousFinalBlockTransactionHashKey,
    const size_t previousFinalBlockTransactionHashKeyLength,
    const uint8_t* blockDifficulty,
    const size_t blockDifficultyLength
);