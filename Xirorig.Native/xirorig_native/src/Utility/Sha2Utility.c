#include "openssl/evp.h"

#include "Utility/Sha2Utility.h"

int32_t Sha2Utility_TryComputeSha256Hash(const uint8_t *source, const size_t sourceLength, uint8_t *destination, uint32_t *bytesWritten)
{
    EVP_MD_CTX *context = EVP_MD_CTX_new();

    if (context == NULL)
    {
        return 0;
    }

    if (EVP_DigestInit_ex(context, EVP_sha256(), NULL) == 0)
    {
        EVP_MD_CTX_free(context);
        return 0;
    }

    if (EVP_DigestUpdate(context, source, sourceLength) == 0)
    {
        EVP_MD_CTX_free(context);
        return 0;
    }

    if (EVP_DigestFinal_ex(context, destination, bytesWritten) == 0)
    {
        EVP_MD_CTX_free(context);
        return 0;
    }

    EVP_MD_CTX_free(context);
    return 1;
}

int32_t Sha2Utility_TryComputeSha512Hash(const uint8_t *source, const size_t sourceLength, uint8_t *destination, uint32_t *bytesWritten)
{
    EVP_MD_CTX *context = EVP_MD_CTX_new();

    if (context == NULL)
    {
        return 0;
    }

    if (EVP_DigestInit_ex(context, EVP_sha512(), NULL) == 0)
    {
        EVP_MD_CTX_free(context);
        return 0;
    }

    if (EVP_DigestUpdate(context, source, sourceLength) == 0)
    {
        EVP_MD_CTX_free(context);
        return 0;
    }

    if (EVP_DigestFinal_ex(context, destination, bytesWritten) == 0)
    {
        EVP_MD_CTX_free(context);
        return 0;
    }

    EVP_MD_CTX_free(context);
    return 1;
}
