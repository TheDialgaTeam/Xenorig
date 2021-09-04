#pragma once

#include <stdint.h>

#include "xirorig_native_export.h"

XIRORIG_NATIVE_EXPORT void XirophtDecentralizedCpuMiner_GeneratePocRandomData(uint8_t *pocRandomData, int32_t randomNumber, int32_t previousBlockTransactionCount, int64_t timestamp, size_t randomDataShareChecksumSize, const uint8_t *walletAddress, size_t walletAddressSize, int64_t currentBlockHeight, int64_t nonce);

XIRORIG_NATIVE_EXPORT void XirophtDecentralizedCpuMiner_UpdatePocRandomData(uint8_t *pocRandomData, int64_t timestamp, size_t randomDataShareChecksumSize, size_t walletAddressSize, int64_t nonce);

XIRORIG_NATIVE_EXPORT int32_t XirophtDecentralizedCpuMiner_DoNonceIvEasySquareMathMiningInstruction(
    size_t pocShareNonceMaxSquareRetry,
    size_t pocShareNonceNoSquareFoundShaRounds,
    int64_t pocShareNonceMin,
    int64_t pocShareNonceMax,
    int64_t currentBlockHeight,
    uint8_t *pocShareIv,
    size_t pocShareIvLength,
    const uint8_t *previousFinalBlockTransactionHashKey,
    size_t previousFinalBlockTransactionHashKeyLength,
    const uint8_t *blockDifficulty,
    size_t blockDifficultyLength
);

XIRORIG_NATIVE_EXPORT int32_t XirophtDecentralizedCpuMiner_GetMaxLz4CompressSize(int32_t inputSize);

XIRORIG_NATIVE_EXPORT int32_t XirophtDecentralizedCpuMiner_DoLz4CompressNonceIvMiningInstruction(const uint8_t *input, int32_t inputSize, uint8_t *output);

XIRORIG_NATIVE_EXPORT int32_t XirophtDecentralizedCpuMiner_DoNonceIvIterationsMiningInstruction(const uint8_t *password, int32_t passwordLength, const uint8_t *salt, int32_t saltLength, int32_t iterations, int32_t keyLength, uint8_t *output);

XIRORIG_NATIVE_EXPORT int32_t XirophtDecentralizedCpuMiner_GetAes256Cfb128OutputSize(int32_t iterations, int32_t dataLength);

XIRORIG_NATIVE_EXPORT int32_t XirophtDecentralizedCpuMiner_DoEncryptedPocShareMiningInstruction(const uint8_t *key, const uint8_t *iv, int32_t iterations, const uint8_t *data, int32_t dataLength, uint8_t *output);
