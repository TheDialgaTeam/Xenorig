#ifndef KEYDERIVATIONFUNCTIONUTILITY_H
#define KEYDERIVATIONFUNCTIONUTILITY_H

#include "global.h"
#include "openssl/evp.h"

typedef struct KDF_PBKDF1_CTX {
    DOTNET_READ_ONLY_SPAN_BYTE Password;
    DOTNET_INT PasswordLength;

    DOTNET_READ_ONLY_SPAN_BYTE Salt;
    DOTNET_INT SaltLength;

    DOTNET_INT Iterations;

    DOTNET_SPAN_BYTE BaseValue;
    DOTNET_INT BaseValueLength;

    DOTNET_SPAN_BYTE Extra;
    DOTNET_INT ExtraLength;
    DOTNET_INT ExtraCount;

    DOTNET_INT Prefix;

    EVP_MD *Hash;
} KDF_PBKDF1_CTX;

KDF_PBKDF1_CTX *KeyDerivationFunctionUtility_CreatePBKDF1(DOTNET_READ_ONLY_SPAN_BYTE password, DOTNET_INT passwordLength, DOTNET_READ_ONLY_SPAN_BYTE salt, DOTNET_INT saltLength, DOTNET_INT iterations, DOTNET_STRING hashName);
DOTNET_INT KeyDerivationFunctionUtility_GetBytes(KDF_PBKDF1_CTX *ctx, DOTNET_SPAN_BYTE rgbOut, DOTNET_INT cb);
void KeyDerivationFunctionUtility_Reset(KDF_PBKDF1_CTX *ctx);
void KeyDerivationFunctionUtility_Free(KDF_PBKDF1_CTX *ctx);

#endif
