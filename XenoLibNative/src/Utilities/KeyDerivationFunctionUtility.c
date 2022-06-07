#include "KeyDerivationFunctionUtility.h"

DOTNET_PRIVATE DOTNET_BOOL ComputeBaseValue(KDF_PBKDF1_CTX *ctx) {
    if (ctx == NULL) {
        return DOTNET_FALSE;
    }

    DOTNET_SPAN_BYTE tempBaseValue = malloc(EVP_MD_get_size(ctx->Hash));

    if (tempBaseValue == NULL) {
        return DOTNET_FALSE;
    }

    EVP_MD_CTX *context = EVP_MD_CTX_new();

    if (context == NULL) {
        return DOTNET_FALSE;
    }

    if (!EVP_DigestInit_ex(context, ctx->Hash, NULL)) {
        EVP_MD_CTX_free(context);
        return DOTNET_FALSE;
    }

    if (!EVP_DigestUpdate(context, ctx->Password, ctx->PasswordLength)) {
        EVP_MD_CTX_free(context);
        return DOTNET_FALSE;
    }

    if (ctx->Salt != NULL) {
        if (!EVP_DigestUpdate(context, ctx->Salt, ctx->SaltLength)) {
            EVP_MD_CTX_free(context);
            return DOTNET_FALSE;
        }
    }

    DOTNET_UINT bytesWritten;

    if (!EVP_DigestFinal_ex(context, tempBaseValue, &bytesWritten)) {
        EVP_MD_CTX_free(context);
        return DOTNET_FALSE;
    }

    DOTNET_INT iterations = ctx->Iterations - 1;

    for (DOTNET_INT i = 1; i < iterations; i++) {
        if (!EVP_DigestInit_ex(context, ctx->Hash, NULL)) {
            EVP_MD_CTX_free(context);
            return DOTNET_FALSE;
        }

        if (!EVP_DigestUpdate(context, tempBaseValue, bytesWritten)) {
            EVP_MD_CTX_free(context);
            return DOTNET_FALSE;
        }

        if (!EVP_DigestFinal_ex(context, tempBaseValue, &bytesWritten)) {
            EVP_MD_CTX_free(context);
            return DOTNET_FALSE;
        }
    }

    EVP_MD_CTX_free(context);

    ctx->BaseValue = tempBaseValue;
    ctx->BaseValueLength = bytesWritten;

    return DOTNET_TRUE;
}

DOTNET_PRIVATE DOTNET_BOOL HashPrefix(KDF_PBKDF1_CTX *ctx, EVP_MD_CTX *context) {
    if (ctx == NULL || context == NULL) {
        return DOTNET_FALSE;
    }

    DOTNET_INT prefix = ctx->Prefix;

    if (prefix > 999) {
        return DOTNET_FALSE;
    }

    DOTNET_INT cb = 0;
    DOTNET_BYTE rgb[] = {'0', '0', '0'};

    if (prefix >= 100) {
        rgb[0] += prefix / 100;
        cb += 1;
    }

    if (prefix >= 10) {
        rgb[cb] += ((prefix % 100) / 10);
        cb += 1;
    }

    if (prefix > 0) {
        rgb[cb] += prefix % 10;
        cb += 1;

        if (!EVP_DigestUpdate(context, rgb, cb)) {
            return DOTNET_FALSE;
        }
    }

    ctx->Prefix += 1;

    return DOTNET_TRUE;
}

DOTNET_PRIVATE DOTNET_BOOL ComputeBytes(KDF_PBKDF1_CTX *ctx, DOTNET_INT cb, DOTNET_SPAN_BYTE rgb) {
    if (ctx == NULL || cb == 0 || rgb == NULL) {
        return DOTNET_FALSE;
    }

    DOTNET_INT cbHash = EVP_MD_get_size(ctx->Hash);
    DOTNET_INT ib = 0;
    DOTNET_SPAN_BYTE baseValue = ctx->BaseValue;
    DOTNET_INT baseValueLength = ctx->BaseValueLength;

    EVP_MD_CTX *context = EVP_MD_CTX_new();

    if (context == NULL) {
        return DOTNET_FALSE;
    }

    if (!EVP_DigestInit_ex(context, ctx->Hash, NULL)) {
        EVP_MD_CTX_free(context);
        return DOTNET_FALSE;
    }

    if (!HashPrefix(ctx, context)) {
        EVP_MD_CTX_free(context);
        return DOTNET_FALSE;
    }

    if (!EVP_DigestUpdate(context, baseValue, baseValueLength)) {
        EVP_MD_CTX_free(context);
        return DOTNET_FALSE;
    }

    DOTNET_UINT bytesWritten;

    if (!EVP_DigestFinal_ex(context, rgb + ib, &bytesWritten)) {
        EVP_MD_CTX_free(context);
        return DOTNET_FALSE;
    }

    ib += cbHash;

    while (cb > ib) {
        if (!EVP_DigestInit_ex(context, ctx->Hash, NULL)) {
            EVP_MD_CTX_free(context);
            return DOTNET_FALSE;
        }

        if (!HashPrefix(ctx, context)) {
            EVP_MD_CTX_free(context);
            return DOTNET_FALSE;
        }

        if (!EVP_DigestUpdate(context, baseValue, baseValueLength)) {
            EVP_MD_CTX_free(context);
            return DOTNET_FALSE;
        }

        if (!EVP_DigestFinal_ex(context, rgb + ib, &bytesWritten)) {
            EVP_MD_CTX_free(context);
            return DOTNET_FALSE;
        }

        ib += cbHash;
    }

    EVP_MD_CTX_free(context);

    return DOTNET_TRUE;
}

KDF_PBKDF1_CTX *KeyDerivationFunctionUtility_CreatePBKDF1(DOTNET_READ_ONLY_SPAN_BYTE password, DOTNET_INT passwordLength, DOTNET_READ_ONLY_SPAN_BYTE salt, DOTNET_INT saltLength, DOTNET_INT iterations, DOTNET_STRING hashName) {
    if (password == NULL || passwordLength == 0 || (salt != NULL && saltLength == 0) || iterations == 0 || hashName == NULL) {
        return NULL;
    }

    KDF_PBKDF1_CTX *ctx = malloc(sizeof(KDF_PBKDF1_CTX));

    if (ctx == NULL) {
        return NULL;
    }

    ctx->Password = password;
    ctx->PasswordLength = passwordLength;

    ctx->Salt = salt;
    ctx->SaltLength = saltLength;

    ctx->Iterations = iterations;

    ctx->BaseValue = NULL;
    ctx->BaseValueLength = 0;

    ctx->Extra = NULL;
    ctx->ExtraLength = 0;
    ctx->ExtraCount = 0;

    ctx->Prefix = 0;

    ctx->Hash = EVP_MD_fetch(NULL, hashName, NULL);

    if (ctx->Hash == NULL) {
        free(ctx);
        return NULL;
    }

    return ctx;
}

DOTNET_INT KeyDerivationFunctionUtility_GetBytes(KDF_PBKDF1_CTX *ctx, DOTNET_SPAN_BYTE rgbOut, DOTNET_INT cb) {
    if (ctx == NULL || rgbOut == NULL || cb == 0) {
        return 0;
    }

    DOTNET_INT ib = 0;

    if (ctx->BaseValue == NULL) {
        if (!ComputeBaseValue(ctx)) {
            return 0;
        }
    } else if (ctx->Extra != NULL) {
        DOTNET_SPAN_BYTE extra = ctx->Extra;
        DOTNET_INT extraCount = ctx->ExtraCount;

        ib = ctx->ExtraLength - extraCount;

        if (ib >= cb) {
            memcpy(rgbOut, extra + extraCount, cb);

            if (ib > cb) {
                ctx->ExtraCount += cb;
            } else {
                free(extra);
                ctx->Extra = NULL;
                ctx->ExtraLength = 0;
            }

            return cb;
        }

        memcpy(rgbOut, extra + ib, ib);
        free(extra);
        ctx->Extra = NULL;
        ctx->ExtraLength = 0;
    }

    DOTNET_INT cbHash = EVP_MD_get_size(ctx->Hash);
    DOTNET_INT rgbLength = ((cb - ib + cbHash - 1) / cbHash * cbHash);
    DOTNET_SPAN_BYTE rgb = malloc(rgbLength);

    if (!ComputeBytes(ctx, cb - ib, rgb)) {
        free(rgb);
        return 0;
    }

    memcpy(rgbOut + ib, rgb, cb - ib);

    if (rgbLength + ib > cb) {
        DOTNET_SPAN_BYTE extra = ctx->Extra;

        if (extra != NULL) {
            free(extra);
        }

        ctx->Extra = rgb;
        ctx->ExtraLength = rgbLength;
        ctx->ExtraCount = cb - ib;
    }

    return cb;
}

void KeyDerivationFunctionUtility_Reset(KDF_PBKDF1_CTX *ctx) {
    if (ctx == NULL) {
        return;
    }

    ctx->Prefix = 0;

    DOTNET_SPAN_BYTE extra = ctx->Extra;

    if (extra != NULL) {
        free(extra);
    }

    ctx->Extra = NULL;
    ctx->ExtraLength = 0;

    DOTNET_SPAN_BYTE baseValue = ctx->BaseValue;

    if (baseValue != NULL) {
        free(baseValue);
    }

    ctx->BaseValue = NULL;
    ctx->BaseValueLength = 0;
}

void KeyDerivationFunctionUtility_Free(KDF_PBKDF1_CTX *ctx) {
    if (ctx == NULL) {
        return;
    }

    DOTNET_SPAN_BYTE baseValue = ctx->BaseValue;

    if (baseValue != NULL) {
        free(baseValue);
    }

    DOTNET_SPAN_BYTE extra = ctx->Extra;

    if (extra != NULL) {
        free(extra);
    }

    EVP_MD *hash = ctx->Hash;

    if (hash != NULL) {
        EVP_MD_free(hash);
    }

    free(ctx);
}
