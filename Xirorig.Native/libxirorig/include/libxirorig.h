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
EXPORT int computeSha3512Hash(const uint8_t* input, size_t inputSize, uint8_t* output);
