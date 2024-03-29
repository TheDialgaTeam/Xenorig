#ifndef BASE64UTILITY_H
#define BASE64UTILITY_H

#include "global.h"

DOTNET_INT Base64Utility_EncodeLength(DOTNET_INT inputLength);
DOTNET_INT Base64Utility_DecodeLength(DOTNET_READ_ONLY_SPAN_BYTE input, DOTNET_INT inputLength);
DOTNET_INT Base64Utility_Encode(DOTNET_READ_ONLY_SPAN_BYTE input, DOTNET_INT inputLength, DOTNET_SPAN_BYTE output);
DOTNET_INT Base64Utility_Decode(DOTNET_READ_ONLY_SPAN_BYTE input, DOTNET_INT inputLength, DOTNET_SPAN_BYTE output);

#endif
