#ifndef XENORIG_UTILITY_CPUINFORMATION_H
#define XENORIG_UTILITY_CPUINFORMATION_H

#include <cstdint>
#include <string>

namespace Xenorig::Utility
{
    struct CpuInformation
    {
        std::string ProcessorName;
        uint32_t L2CacheSize = 0;
        uint32_t L3CacheSize = 0;
        uint32_t ProcessorCoreCount = 0;
        uint32_t ProcessorCount = 0;

        static CpuInformation GetCpuInformation();
    };
} // namespace Xenorig::Utility

#endif // XENORIG_UTILITY_CPUINFORMATION_H
