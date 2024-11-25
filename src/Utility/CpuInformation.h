#ifndef XENORIG_UTILITY_CPUINFORMATION_H
#define XENORIG_UTILITY_CPUINFORMATION_H

#include <cstdint>
#include <string>

namespace Xenorig::Utility
{
    class CpuInformation
    {
        std::string processorName;
        uint32_t l2CacheSize;
        uint32_t l3CacheSize;
        uint32_t processorCoreCount;

    public:
        CpuInformation();
        ~CpuInformation();

        const std::string &GetProcessorName() const;
        uint32_t GetL2CacheSize() const;
        uint32_t GetL3CacheSize() const;
        uint32_t GetProcessorCoreCount() const;
    };
} // namespace Xenorig::Utility

#endif // XENORIG_UTILITY_CPUINFORMATION_H
