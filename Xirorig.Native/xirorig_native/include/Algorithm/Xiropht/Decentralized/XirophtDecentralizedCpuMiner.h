#pragma once

#include <stdint.h>

#include "xirorig_native_export.h"

XIRORIG_NATIVE_EXPORT void XirophtDecentralizedCpuMiner_GeneratePocRandomData(uint8_t *pocRandomData, int32_t randomNumber, int32_t randomNumber2, int64_t timestamp, int32_t randomDataShareChecksumSize, const uint8_t *walletAddress, int32_t walletAddressSize, int64_t currentBlockHeight, int64_t nonce);

XIRORIG_NATIVE_EXPORT void XirophtDecentralizedCpuMiner_UpdatePocRandomData(uint8_t *pocRandomData, int64_t timestamp, int32_t randomDataShareChecksumSize, int32_t walletAddressSize, int64_t nonce);

XIRORIG_NATIVE_EXPORT void XirophtDecentralizedCpuMiner_DoNonceIvMiningInstruction(uint8_t *pocShareIv, int32_t *pocShareIvSize, int32_t pocRoundShaNonce);

XIRORIG_NATIVE_EXPORT void XirophtDecentralizedCpuMiner_DoNonceIvXorMiningInstruction(uint8_t *pocShareIv, int32_t pocShareIvSize);

XIRORIG_NATIVE_EXPORT int32_t XirophtDecentralizedCpuMiner_DoNonceIvEasySquareMathMiningInstruction(
    size_t pocShareNonceMaxSquareRetry,
    size_t pocShareNonceNoSquareFoundShaRounds,
    int64_t pocShareNonceMin,
    int64_t pocShareNonceMax,
    int64_t currentBlockHeight,
    uint8_t *pocShareIv,
    size_t *pocShareIvSize,
    uint8_t *pocShareWorkToDoBytes,
    const uint8_t *currentBlockDifficulty,
    size_t currentBlockDifficultyLength,
    const uint8_t *previousFinalBlockTransactionHashKey,
    size_t previousFinalBlockTransactionHashKeyLength
);

XIRORIG_NATIVE_EXPORT void XirophtDecentralizedCpuMiner_DoLz4CompressNonceIvMiningInstruction(uint8_t* pocShareIv, int32_t* pocShareIvSize);

XIRORIG_NATIVE_EXPORT int32_t XirophtDecentralizedCpuMiner_DoNonceIvIterationsMiningInstruction(const uint8_t *password, int32_t passwordLength, const uint8_t *salt, int32_t saltLength, int32_t iterations, int32_t keyLength, uint8_t *output);

XIRORIG_NATIVE_EXPORT int32_t XirophtDecentralizedCpuMiner_DoEncryptedPocShareMiningInstruction(const uint8_t* key, const uint8_t* iv, const int32_t iterations, uint8_t* data, int32_t* dataLength);
