#pragma once

#include <stdint.h>

#include "xirorig_native_export.h"

/**
 * \brief Compute SHA-2 256 hash.
 * \param source The source array.
 * \param sourceLength The length of the source array.
 * \param destination The destination array.
 * \param bytesWritten The length of bytes written.
 * \return Returns 1 for success and 0 for failure.
 */
XIRORIG_NATIVE_EXPORT int32_t Sha2Utility_TryComputeSha256Hash(const uint8_t *source, size_t sourceLength, uint8_t *destination, uint32_t *bytesWritten);

/**
 * \brief Compute SHA-2 512 hash.
 * \param source The source array.
 * \param sourceLength The length of the source array.
 * \param destination The destination array.
 *\param bytesWritten The length of bytes written.
 * \return Returns 1 for success and 0 for failure.
 */
XIRORIG_NATIVE_EXPORT int32_t Sha2Utility_TryComputeSha512Hash(const uint8_t *source, size_t sourceLength, uint8_t *destination, uint32_t* bytesWritten);
