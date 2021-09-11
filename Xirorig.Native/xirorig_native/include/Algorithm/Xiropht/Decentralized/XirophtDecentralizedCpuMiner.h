#pragma once

#include <stdint.h>

#include "xirorig_native_export.h"

XIRORIG_NATIVE_EXPORT void XirophtDecentralizedCpuMiner_GeneratePocRandomData(uint8_t *pocRandomData, int32_t randomNumber, int32_t randomNumber2, int64_t timestamp, int32_t randomDataShareChecksumSize, const uint8_t *walletAddress, int32_t walletAddressSize, int64_t currentBlockHeight, int64_t nonce);

XIRORIG_NATIVE_EXPORT void XirophtDecentralizedCpuMiner_UpdatePocRandomData(uint8_t *pocRandomData, int64_t timestamp, int32_t randomDataShareChecksumSize, int32_t walletAddressSize, int64_t nonce);

XIRORIG_NATIVE_EXPORT void XirophtDecentralizedCpuMiner_DoNonceIvMiningInstruction(uint8_t *pocShareIv, int32_t *pocShareIvSize, int32_t pocRoundShaNonce);

XIRORIG_NATIVE_EXPORT void XirophtDecentralizedCpuMiner_DoNonceIvXorMiningInstruction(uint8_t *pocShareIv, int32_t pocShareIvSize);

XIRORIG_NATIVE_EXPORT int32_t XirophtDecentralizedCpuMiner_DoNonceIvEasySquareMathMiningInstruction(
    int32_t pocShareNonceMaxSquareRetry,
    int32_t pocShareNonceNoSquareFoundShaRounds,
    int64_t pocShareNonceMin,
    int64_t pocShareNonceMax,
    int64_t currentBlockHeight,
    uint8_t *pocShareIv,
    int32_t *pocShareIvSize,
    uint8_t *pocShareWorkToDoBytes,
    const uint8_t *currentBlockDifficulty,
    int32_t currentBlockDifficultyLength,
    const uint8_t *previousFinalBlockTransactionHashKey,
    int32_t previousFinalBlockTransactionHashKeyLength
);

XIRORIG_NATIVE_EXPORT void XirophtDecentralizedCpuMiner_DoLz4CompressNonceIvMiningInstruction(uint8_t *pocShareIv, int32_t *pocShareIvSize);

XIRORIG_NATIVE_EXPORT int32_t XirophtDecentralizedCpuMiner_DoNonceIvIterationsMiningInstruction(uint8_t *pocShareIv, int32_t *pocShareIvSize, const uint8_t *blockchainMarkKey, int32_t blockchainMarkKeySize, int32_t pocShareNonceIvIteration, int32_t keyLength);

XIRORIG_NATIVE_EXPORT int32_t XirophtDecentralizedCpuMiner_DoEncryptedPocShareMiningInstruction(const uint8_t *key, const uint8_t *iv, int32_t iterations, uint8_t *data, int32_t *dataLength);
