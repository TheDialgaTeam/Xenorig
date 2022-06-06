#ifndef GLOBAL_H
#define GLOBAL_H

#include <stdlib.h>
#include <stdint.h>
#include <string.h>

#include "xeno_native_export.h"

#define DOTNET_PRIVATE static
#define DOTNET_PUBLIC XENO_NATIVE_EXPORT

#define DOTNET_TRUE 1
#define DOTNET_FALSE 0

#define DOTNET_BOOL int32_t

#define DOTNET_CHAR char
#define DOTNET_CHAR_ARRAY char *
#define DOTNET_SPAN_CHAR char *
#define DOTNET_READ_ONLY_SPAN_CHAR const char *

#define DOTNET_STRING const char *

#define DOTNET_BYTE uint8_t
#define DOTNET_BYTE_ARRAY uint8_t *
#define DOTNET_SPAN_BYTE uint8_t *
#define DOTNET_READ_ONLY_SPAN_BYTE const uint8_t *
#define DOTNET_BYTE_MIN 0
#define DOTNET_BYTE_MAX UINT8_MAX

#define DOTNET_SHORT int16_t
#define DOTNET_SHORT_ARRAY int16_t *
#define DOTNET_SPAN_SHORT int16_t *
#define DOTNET_READ_ONLY_SPAN_SHORT const int16_t *
#define DOTNET_SHORT_MIN INT16_MIN
#define DOTNET_SHORT_MAX INT16_MAX

#define DOTNET_USHORT uint16_t
#define DOTNET_USHORT_ARRAY uint16_t *
#define DOTNET_SPAN_USHORT uint16_t *
#define DOTNET_READ_ONLY_SPAN_USHORT const uint16_t *
#define DOTNET_USHORT_MIN 0
#define DOTNET_USHORT_MAX UINT16_MAX

#define DOTNET_INT int32_t
#define DOTNET_INT_ARRAY int32_t *
#define DOTNET_SPAN_INT int32_t *
#define DOTNET_READ_ONLY_SPAN_INT const int32_t *
#define DOTNET_INT_MIN INT32_MIN
#define DOTNET_INT_MAX INT32_MAX

#define DOTNET_UINT uint32_t
#define DOTNET_UINT_ARRAY uint32_t *
#define DOTNET_SPAN_UINT uint32_t *
#define DOTNET_READ_ONLY_SPAN_UINT const uint32_t *
#define DOTNET_UINT_MIN 0
#define DOTNET_UINT_MAX UINT32_MAX

#define DOTNET_LONG int64_t
#define DOTNET_LONG_ARRAY int64_t *
#define DOTNET_SPAN_LONG int64_t *
#define DOTNET_READ_ONLY_SPAN_LONG const int64_t *
#define DOTNET_LONG_MIN INT64_MIN
#define DOTNET_LONG_MAX INT64_MAX

#define DOTNET_ULONG uint64_t
#define DOTNET_ULONG_ARRAY uint64_t *
#define DOTNET_SPAN_ULONG uint64_t *
#define DOTNET_READ_ONLY_SPAN_ULONG const uint64_t *
#define DOTNET_ULONG_MIN 0
#define DOTNET_ULONG_MAX UINT64_MAX

#define DOTNET_FLOAT float
#define DOTNET_FLOAT_ARRAY float *
#define DOTNET_SPAN_FLOAT float *
#define DOTNET_READ_ONLY_SPAN_FLOAT const float *

#define DOTNET_DOUBLE double
#define DOTNET_DOUBLE_ARRAY double *
#define DOTNET_SPAN_DOUBLE double *
#define DOTNET_READ_ONLY_SPAN_DOUBLE const double *

#endif
