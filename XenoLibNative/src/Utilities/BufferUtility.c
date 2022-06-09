#include "BufferUtility.h"

void BufferUtility_MemoryCopy_Long(DOTNET_SPAN_LONG destination, DOTNET_READ_ONLY_SPAN_LONG source, DOTNET_LONG length)
{
    memcpy(destination, source, sizeof(DOTNET_LONG) * length);
}
