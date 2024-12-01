#include "cpu_information.h"

#include <cpuinfo.h>

xenorig::utility::cpu_information::data xenorig::utility::cpu_information::get_cpu_information()
{
    data data;

    if (!cpuinfo_initialize())
    {
        return data;
    }

    if (const auto package = cpuinfo_get_package(0); package != nullptr)
    {
        data.processor_name = package->name;
    }

    if (const auto cache = cpuinfo_get_l2_cache(0); cache != nullptr)
    {
        data.l2_cache_size = cache->size * cpuinfo_get_cores_count();
    }

    if (const auto cache = cpuinfo_get_l3_cache(0); cache != nullptr)
    {
        data.l3_cache_size = cache->size;
    }

    data.processor_core_count = cpuinfo_get_cores_count();
    data.processor_count = cpuinfo_get_processors_count();

    cpuinfo_deinitialize();

    return data;
}
