#include "CpuInformationUtility.h"

#ifndef NO_CPU_INFO
#include "cpuinfo.h"
#endif

DOTNET_STRING CpuInformationUtility_GetProcessorName() {
#ifdef NO_CPU_INFO
    return "Unknown CPU";
#else
    if (!cpuinfo_initialize()) {
        return "";
    }

    const struct cpuinfo_package *package = cpuinfo_get_package(0);

    if (package == NULL) {
        return "";
    }

    return package->name;
#endif
}

DOTNET_INT CpuInformationUtility_GetProcessorL2Cache() {
#ifdef NO_CPU_INFO
    return 0;
#else
    if (!cpuinfo_initialize()) {
        return 0;
    }

    const struct cpuinfo_cache *cache = cpuinfo_get_l2_cache(0);

    if (cache == NULL) {
        return 0;
    }

    return cache->size * cpuinfo_get_cores_count();
#endif
}

DOTNET_INT CpuInformationUtility_GetProcessorL3Cache() {
#ifdef NO_CPU_INFO
    return 0;
#else
    if (!cpuinfo_initialize()) {
        return 0;
    }

    const struct cpuinfo_cache *cache = cpuinfo_get_l3_cache(0);

    if (cache == NULL) {
        return 0;
    }

    return cache->size;
#endif
}

DOTNET_INT CpuInformationUtility_GetProcessorCoreCount() {
#ifdef NO_CPU_INFO
    return 0;
#else
    if (!cpuinfo_initialize()) {
        return 0;
    }

    return cpuinfo_get_cores_count();
#endif
}
