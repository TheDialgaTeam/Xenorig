#include "CpuInformation.h"

#include <cpuinfo.h>

Xenorig::Utility::CpuInformation Xenorig::Utility::CpuInformation::GetCpuInformation()
{
    CpuInformation data;

    if (!cpuinfo_initialize()) return data;

    if (const auto package = cpuinfo_get_package(0); package != nullptr)
    {
        data.ProcessorName = package->name;
    }

    if (const auto cache = cpuinfo_get_l2_cache(0); cache != nullptr)
    {
        data.L2CacheSize = cache->size * cpuinfo_get_cores_count();
    }

    if (const auto cache = cpuinfo_get_l3_cache(0); cache != nullptr)
    {
        data.L3CacheSize = cache->size;
    }

    data.ProcessorCoreCount = cpuinfo_get_cores_count();
    data.ProcessorCount = cpuinfo_get_processors_count();

    cpuinfo_deinitialize();

    return data;
}