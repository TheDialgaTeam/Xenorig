#include "CpuInformation.h"

#include <cpuinfo.h>

namespace Xenorig::Utility
{
    CpuInformation::CpuInformation()
    {
        if (!cpuinfo_initialize()) return;

        if (const auto package = cpuinfo_get_package(0); package != nullptr)
        {
            processorName = package->name;
        }

        if (const auto cache = cpuinfo_get_l2_cache(0); cache != nullptr)
        {
            l2CacheSize = cache->size * cpuinfo_get_cores_count();
        }

        if (const auto cache = cpuinfo_get_l3_cache(0); cache != nullptr)
        {
            l3CacheSize = cache->size;
        }

        processorCoreCount = cpuinfo_get_cores_count();
    }

    CpuInformation::~CpuInformation()
    {
        cpuinfo_deinitialize();
    }

    const std::string &CpuInformation::GetProcessorName() const
    {
        return processorName;
    }

    uint32_t CpuInformation::GetL2CacheSize() const
    {
        return l2CacheSize;
    }

    uint32_t CpuInformation::GetL3CacheSize() const
    {
        return l3CacheSize;
    }

    uint32_t CpuInformation::GetProcessorCoreCount() const
    {
        return processorCoreCount;
    }
} // namespace Xenorig::Utility