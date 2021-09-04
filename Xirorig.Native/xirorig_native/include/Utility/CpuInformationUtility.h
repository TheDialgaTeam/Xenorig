#pragma once

#include <stdint.h>

#include "xirorig_native_export.h"

/**
 * \brief Get the processor name.
 * \return Returns the processor name.
 */
XIRORIG_NATIVE_EXPORT const char* CpuInformationUtility_GetProcessorName(void);

/**
 * \brief Get the processor L2 cache.
 * \return Returns the processor L2 cache in bytes.
 */
XIRORIG_NATIVE_EXPORT uint32_t CpuInformationUtility_GetProcessorL2Cache(void);

/**
 * \brief Get the processor L3 cache.
 * \return Returns the processor L3 cache in bytes.
 */
XIRORIG_NATIVE_EXPORT uint32_t CpuInformationUtility_GetProcessorL3Cache(void);

/**
 * \brief Get the processor core count.
 * \return Returns the processor core count.
 */
XIRORIG_NATIVE_EXPORT uint32_t CpuInformationUtility_GetProcessorCoreCount(void);
