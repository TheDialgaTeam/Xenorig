#pragma once

#include "global.h"

typedef int32_t RandomDataShareNumberType;
typedef int64_t RandomDataShareTimestampType;
typedef int64_t RandomDataShareBlockHeightType;
typedef int64_t RandomDataShareNonceType;

XENO_NATIVE_EXPORT void XirophtDecentralizedAlgorithm_Solo_GeneratePocRandomData(
    uint8_t *pocRandomData,
    RandomDataShareNumberType randomNumber,
    RandomDataShareNumberType randomNumber2,
    RandomDataShareTimestampType timestamp,
    int32_t checksumSize,
    const uint8_t *walletAddress,
    int32_t walletAddressSize,
    RandomDataShareBlockHeightType blockHeight,
    RandomDataShareNonceType nonce
);

XENO_NATIVE_EXPORT void XirophtDecentralizedAlgorithm_Solo_UpdatePocRandomData(
    uint8_t *pocRandomData,
    RandomDataShareTimestampType timestamp,
    int32_t checksumSize,
    int32_t walletAddressSize,
    RandomDataShareNonceType nonce
);

XENO_NATIVE_EXPORT void XirophtDecentralizedAlgorithm_Solo_DoNonceIvMiningInstruction(
    uint8_t *pocShareIv,
    int32_t *pocShareIvSize,
    int32_t pocRoundShaNonce
);

XENO_NATIVE_EXPORT void XirophtDecentralizedAlgorithm_Solo_DoNonceIvXorMiningInstruction(uint8_t *pocShareIv, int32_t pocShareIvSize);

XENO_NATIVE_EXPORT int32_t XirophtDecentralizedAlgorithm_Solo_DoNonceIvEasySquareMathMiningInstruction(
    int32_t pocShareNonceMaxSquareRetry,
    int32_t pocShareNonceNoSquareFoundShaRounds,
    int64_t pocShareNonceMin,
    int64_t pocShareNonceMax,
    RandomDataShareBlockHeightType blockHeight,
    uint8_t *pocShareIv,
    int32_t *pocShareIvSize,
    uint8_t *pocShareWorkToDoBytes,
    const uint8_t *blockDifficulty,
    int32_t blockDifficultyLength,
    const uint8_t *previousFinalBlockTransactionHashKey,
    int32_t previousFinalBlockTransactionHashKeyLength
);

XENO_NATIVE_EXPORT void XirophtDecentralizedAlgorithm_Solo_DoLz4CompressNonceIvMiningInstruction(uint8_t *pocShareIv, int32_t *pocShareIvSize);

XENO_NATIVE_EXPORT int32_t XirophtDecentralizedAlgorithm_Solo_DoNonceIvIterationsMiningInstruction(
    uint8_t *pocShareIv, 
    int32_t *pocShareIvSize, 
    const uint8_t *blockchainMarkKey, 
    int32_t blockchainMarkKeySize, 
    int32_t pocShareNonceIvIteration, 
    int32_t keyLength
);

XENO_NATIVE_EXPORT int32_t XirophtDecentralizedAlgorithm_Solo_DoEncryptedPocShareMiningInstruction(
    const uint8_t *key, 
    const uint8_t *iv,
    int32_t iterations,
    uint8_t *data,
    int32_t *dataLength
);
