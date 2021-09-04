#include "openssl/evp.h"

#include "Utility/Sha2Utility.h"

int32_t Sha2Utility_ComputeSha256Hash(const uint8_t* input, const size_t inputSize, uint8_t* output)
{
    EVP_MD_CTX* context = EVP_MD_CTX_new();

    if (context == NULL)
    {
        return 0;
    }

    if (EVP_DigestInit_ex(context, EVP_sha256(), NULL) == 0)
    {
        EVP_MD_CTX_free(context);
        return 0;
    }

    if (EVP_DigestUpdate(context, input, inputSize) == 0)
    {
        EVP_MD_CTX_free(context);
        return 0;
    }

    if (EVP_DigestFinal_ex(context, output, NULL) == 0)
    {
        EVP_MD_CTX_free(context);
        return 0;
    }

    EVP_MD_CTX_free(context);
    return 1;
}

int32_t Sha2Utility_ComputeSha512Hash(const uint8_t* input, const size_t inputSize, uint8_t* output)
{
    EVP_MD_CTX* context = EVP_MD_CTX_new();

    if (context == NULL)
    {
        return 0;
    }

    if (EVP_DigestInit_ex(context, EVP_sha512(), NULL) == 0)
    {
        EVP_MD_CTX_free(context);
        return 0;
    }

    if (EVP_DigestUpdate(context, input, inputSize) == 0)
    {
        EVP_MD_CTX_free(context);
        return 0;
    }

    if (EVP_DigestFinal_ex(context, output, NULL) == 0)
    {
        EVP_MD_CTX_free(context);
        return 0;
    }

    EVP_MD_CTX_free(context);
    return 1;
}
