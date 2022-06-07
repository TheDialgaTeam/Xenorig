#include "CpuInformationUtility.h"
#include "cpuinfo.h"

DOTNET_STRING CpuInformationUtility_GetProcessorName() {
    if (!cpuinfo_initialize()) {
        return "";
    }

    const struct cpuinfo_package *package = cpuinfo_get_package(0);

    if (package == NULL) {
        return "";
    }

    return package->name;
}

DOTNET_INT CpuInformationUtility_GetProcessorL2Cache() {
    if (!cpuinfo_initialize()) {
        return 0;
    }

    const struct cpuinfo_cache *cache = cpuinfo_get_l2_cache(0);

    if (cache == NULL) {
        return 0;
    }

    return cache->size * cpuinfo_get_cores_count();
}

DOTNET_INT CpuInformationUtility_GetProcessorL3Cache() {
    if (!cpuinfo_initialize()) {
        return 0;
    }

    const struct cpuinfo_cache *cache = cpuinfo_get_l3_cache(0);

    if (cache == NULL) {
        return 0;
    }

    return cache->size;
}

DOTNET_INT CpuInformationUtility_GetProcessorCoreCount() {
    if (!cpuinfo_initialize()) {
        return 0;
    }

    return cpuinfo_get_cores_count();
}
