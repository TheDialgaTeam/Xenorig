#ifndef BUFFERUTILITY_H
#define BUFFERUTILITY_H

#include "global.h"

#define MemoryCopyHeader(name, type) DOTNET_PUBLIC void BufferUtility_MemoryCopy_##name(DOTNET_SPAN_##type destination, DOTNET_READ_ONLY_SPAN_##type source, DOTNET_INT length)
#define MemoryCopySource(name, type) DOTNET_PUBLIC void BufferUtility_MemoryCopy_##name(DOTNET_SPAN_##type destination, DOTNET_READ_ONLY_SPAN_##type source, DOTNET_INT length) { memcpy(destination, source, sizeof(DOTNET_##type) * length); }

#define MemoryMoveHeader(name, type) DOTNET_PUBLIC void BufferUtility_MemoryMove_##name(DOTNET_SPAN_##type destination, DOTNET_READ_ONLY_SPAN_##type source, DOTNET_INT length)
#define MemoryMoveSource(name, type) DOTNET_PUBLIC void BufferUtility_MemoryMove_##name(DOTNET_SPAN_##type destination, DOTNET_READ_ONLY_SPAN_##type source, DOTNET_INT length) { memmove(destination, source, sizeof(DOTNET_##type) * length); }

MemoryCopyHeader(Byte, BYTE);
MemoryCopyHeader(Short, SHORT);
MemoryCopyHeader(UShort, USHORT);
MemoryCopyHeader(Int, INT);
MemoryCopyHeader(UInt, UINT);
MemoryCopyHeader(Long, LONG);
MemoryCopyHeader(ULong, ULONG);
MemoryCopyHeader(float, FLOAT);
MemoryCopyHeader(double, DOUBLE);

MemoryMoveHeader(Byte, BYTE);
MemoryMoveHeader(Short, SHORT);
MemoryMoveHeader(UShort, USHORT);
MemoryMoveHeader(Int, INT);
MemoryMoveHeader(UInt, UINT);
MemoryMoveHeader(Long, LONG);
MemoryMoveHeader(ULong, ULONG);
MemoryMoveHeader(float, FLOAT);
MemoryMoveHeader(double, DOUBLE);

#endif
