#ifndef XENORIG_UTILITY_CPUINFORMATION_H
#define XENORIG_UTILITY_CPUINFORMATION_H

#include <cstdint>
#include <string>

namespace Xenorig::Utility::CpuInformation
{
    extern std::string ProcessorName;
    extern uint32_t L2CacheSize;
    extern uint32_t L3CacheSize;
    extern uint32_t ProcessorCoreCount;

    void Initialize();
} // namespace Xenorig::Utility

#endif // XENORIG_UTILITY_CPUINFORMATION_H
