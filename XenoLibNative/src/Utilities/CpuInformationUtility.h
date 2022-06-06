#ifndef CPUINFORMATIONUTILITY_H
#define CPUINFORMATIONUTILITY_H

#include "global.h"

DOTNET_PUBLIC DOTNET_STRING CpuInformationUtility_GetProcessorName(void);
DOTNET_PUBLIC DOTNET_INT CpuInformationUtility_GetProcessorL2Cache(void);
DOTNET_PUBLIC DOTNET_INT CpuInformationUtility_GetProcessorL3Cache(void);
DOTNET_PUBLIC DOTNET_INT CpuInformationUtility_GetProcessorCoreCount(void);

#endif
