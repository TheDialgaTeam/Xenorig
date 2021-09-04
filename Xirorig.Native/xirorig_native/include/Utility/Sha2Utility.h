#pragma once

#include <stdint.h>

#include "xirorig_native_export.h"

/**
 * \brief Compute SHA-2 256 hash.
 * \param input Input byte array.
 * \param inputSize The size of the input array.
 * \param output The output array.
 * \return Returns 1 for success and 0 for failure.
 */
XIRORIG_NATIVE_EXPORT int32_t Sha2Utility_ComputeSha256Hash(const uint8_t *input, size_t inputSize, uint8_t *output);

/**
 * \brief Compute SHA-2 512 hash.
 * \param input Input byte array.
 * \param inputSize The size of the input array.
 * \param output The output array.
 * \return Returns 1 for success and 0 for failure.
 */
XIRORIG_NATIVE_EXPORT int32_t Sha2Utility_ComputeSha512Hash(const uint8_t* input, size_t inputSize, uint8_t* output);
