#ifndef MESSAGEDIGESTUTILITY_H
#define MESSAGEDIGESTUTILITY_H

#include "global.h"
#include "openssl/evp.h"

DOTNET_INT MessageDigestUtility_ComputeSha2_256Hash(DOTNET_READ_ONLY_SPAN_BYTE source, DOTNET_INT sourceLength, DOTNET_SPAN_BYTE destination);
DOTNET_INT MessageDigestUtility_ComputeSha2_512Hash(DOTNET_READ_ONLY_SPAN_BYTE source, DOTNET_INT sourceLength, DOTNET_SPAN_BYTE destination);
DOTNET_INT MessageDigestUtility_ComputeSha3_512Hash(DOTNET_READ_ONLY_SPAN_BYTE source, DOTNET_INT sourceLength, DOTNET_SPAN_BYTE destination);

#endif
