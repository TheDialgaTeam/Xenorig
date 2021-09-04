#include "openssl/evp.h"

#include "Utility/Sha3Utility.h"

int32_t Sha3Utility_ComputeSha512Hash(const uint8_t* input, const size_t inputSize, uint8_t* output)
{
    EVP_MD_CTX* context = EVP_MD_CTX_new();

    if (context == NULL)
    {
        return 0;
    }

    if (EVP_DigestInit_ex(context, EVP_sha3_512(), NULL) == 0)
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
