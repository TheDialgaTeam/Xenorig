#include "MessageDigestUtility.h"

DOTNET_INT MessageDigestUtility_ComputeHash_EVP_MD(const EVP_MD *type, DOTNET_READ_ONLY_SPAN_BYTE source, DOTNET_INT sourceLength, DOTNET_SPAN_BYTE destination) {
    EVP_MD_CTX *context = EVP_MD_CTX_new();

    if (context == NULL) {
        return 0;
    }

    if (!EVP_DigestInit_ex(context, type, NULL)) {
        EVP_MD_CTX_free(context);
        return 0;
    }

    if (!EVP_DigestUpdate(context, source, sourceLength)) {
        EVP_MD_CTX_free(context);
        return 0;
    }

    DOTNET_UINT bytesWritten;

    if (!EVP_DigestFinal_ex(context, destination, &bytesWritten)) {
        EVP_MD_CTX_free(context);
        return 0;
    }

    EVP_MD_CTX_free(context);

    return (DOTNET_INT) bytesWritten;
}

inline DOTNET_PUBLIC DOTNET_INT MessageDigestUtility_ComputeSha2_256Hash(DOTNET_READ_ONLY_SPAN_BYTE source, DOTNET_INT sourceLength, DOTNET_SPAN_BYTE destination) {
    return MessageDigestUtility_ComputeHash_EVP_MD(EVP_sha256(), source, sourceLength, destination);
}

inline DOTNET_PUBLIC DOTNET_INT MessageDigestUtility_ComputeSha2_512Hash(DOTNET_READ_ONLY_SPAN_BYTE source, DOTNET_INT sourceLength, DOTNET_SPAN_BYTE destination) {
    return MessageDigestUtility_ComputeHash_EVP_MD(EVP_sha512(), source, sourceLength, destination);
}

inline DOTNET_PUBLIC DOTNET_INT MessageDigestUtility_ComputeSha3_512Hash(DOTNET_READ_ONLY_SPAN_BYTE source, DOTNET_INT sourceLength, DOTNET_SPAN_BYTE destination) {
    return MessageDigestUtility_ComputeHash_EVP_MD(EVP_sha3_512(), source, sourceLength, destination);
}
