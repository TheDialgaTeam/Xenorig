#include <stddef.h>

#include "cpuinfo.h"

#include "Utility/CpuInformationUtility.h"

const char *CpuInformationUtility_GetProcessorName()
{
    if (cpuinfo_initialize() == 0)
    {
        return "";
    }

    const struct cpuinfo_package* package = cpuinfo_get_package(0);
    
    if (package == NULL)
    {
        return "";
    }

    return package->name;
}

uint32_t CpuInformationUtility_GetProcessorL2Cache()
{
    if (cpuinfo_initialize() == 0)
    {
        return 0;
    }

    const struct cpuinfo_cache* cache = cpuinfo_get_l2_cache(0);
    
    if (cache == NULL)
    {
        return 0;
    }

    return cache->size * cpuinfo_get_cores_count();
}

uint32_t CpuInformationUtility_GetProcessorL3Cache()
{
    if (cpuinfo_initialize() == 0)
    {
        return 0;
    }

    const struct cpuinfo_cache* cache = cpuinfo_get_l3_cache(0);

    if (cache == NULL)
    {
        return 0;
    }

    return cache->size;
}

uint32_t CpuInformationUtility_GetProcessorCoreCount()
{
    if (cpuinfo_initialize() == 0)
    {
        return 0;
    }

    return cpuinfo_get_cores_count();
}
