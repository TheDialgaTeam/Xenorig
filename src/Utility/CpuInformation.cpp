#include "CpuInformation.h"

#include <cpuinfo.h>

std::string Xenorig::Utility::CpuInformation::ProcessorName = "";
uint32_t Xenorig::Utility::CpuInformation::L2CacheSize = 0;
uint32_t Xenorig::Utility::CpuInformation::L3CacheSize = 0;
uint32_t Xenorig::Utility::CpuInformation::ProcessorCoreCount = 0;

void Xenorig::Utility::CpuInformation::Initialize()
{
    if (!cpuinfo_initialize()) return;

    if (const auto package = cpuinfo_get_package(0); package != nullptr)
    {
        Xenorig::Utility::CpuInformation::ProcessorName = package->name;
    }

    if (const auto cache = cpuinfo_get_l2_cache(0); cache != nullptr)
    {
        Xenorig::Utility::CpuInformation::L2CacheSize = cache->size * cpuinfo_get_cores_count();
    }

    if (const auto cache = cpuinfo_get_l3_cache(0); cache != nullptr)
    {
        Xenorig::Utility::CpuInformation::L3CacheSize = cache->size;
    }

    Xenorig::Utility::CpuInformation::ProcessorCoreCount = cpuinfo_get_cores_count();

    cpuinfo_deinitialize();
}
