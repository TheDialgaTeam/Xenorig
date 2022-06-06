#include "SymmetricAlgorithmUtility.h"

inline DOTNET_INT SymmetricAlgorithmUtility_GetPaddedLength(DOTNET_INT size)
{
    return 16 - size % 16;
}

DOTNET_INT SymmetricAlgorithmUtility_Encrypt_EVP_CIPHER(const EVP_CIPHER *type, DOTNET_READ_ONLY_SPAN_BYTE key, DOTNET_READ_ONLY_SPAN_BYTE iv, DOTNET_READ_ONLY_SPAN_BYTE source, DOTNET_INT sourceLength, DOTNET_SPAN_BYTE destination, DOTNET_BOOL padding) {
    if (type == NULL || key == NULL || iv == NULL || source == NULL || sourceLength == 0 || destination == NULL) {
        return 0;
    }

    EVP_CIPHER_CTX *context = EVP_CIPHER_CTX_new();

    if (context == NULL) {
        return 0;
    }

    EVP_CIPHER_CTX_set_padding(context, 0);

    if (!EVP_EncryptInit_ex(context, type, NULL, key, iv)) {
        EVP_CIPHER_CTX_free(context);
        return 0;
    }

    DOTNET_INT initialOutputLength;

    if (!EVP_EncryptUpdate(context, destination, &initialOutputLength, source, sourceLength)) {
        EVP_CIPHER_CTX_free(context);
        return 0;
    }

    DOTNET_BYTE paddingSizeRequired = SymmetricAlgorithmUtility_GetPaddedLength(sourceLength);
    DOTNET_INT paddingOutputLength = 0;

    if (paddingSizeRequired > 0 && padding) {
        DOTNET_INT tempOutputLength;

        for (DOTNET_INT i = paddingSizeRequired - 1; i >= 0; i--) {
            if (!EVP_EncryptUpdate(context, destination + initialOutputLength + paddingOutputLength, &tempOutputLength, &paddingSizeRequired, 1)) {
                EVP_CIPHER_CTX_free(context);
                return 0;
            }

            paddingOutputLength += tempOutputLength;
        }
    }

    DOTNET_INT finalOutputLength;

    if (!EVP_EncryptFinal_ex(context, destination + initialOutputLength + paddingOutputLength, &finalOutputLength)) {
        EVP_CIPHER_CTX_free(context);
        return 0;
    }

    EVP_CIPHER_CTX_free(context);

    return initialOutputLength + paddingOutputLength + finalOutputLength;
}

DOTNET_INT SymmetricAlgorithmUtility_Decrypt_EVP_CIPHER(const EVP_CIPHER *type, DOTNET_READ_ONLY_SPAN_BYTE key, DOTNET_READ_ONLY_SPAN_BYTE iv, DOTNET_READ_ONLY_SPAN_BYTE source, DOTNET_INT sourceLength, DOTNET_SPAN_BYTE destination) {
    if (type == NULL || key == NULL || iv == NULL || source == NULL || sourceLength == 0 || destination == NULL) {
        return 0;
    }

    EVP_CIPHER_CTX *context = EVP_CIPHER_CTX_new();

    if (context == NULL) {
        return 0;
    }

    if (!EVP_DecryptInit_ex(context, type, NULL, key, iv)) {
        EVP_CIPHER_CTX_free(context);
        return 0;
    }

    DOTNET_INT outputLength;

    if (!EVP_DecryptUpdate(context, destination, &outputLength, source, sourceLength)) {
        EVP_CIPHER_CTX_free(context);
        return 0;
    }

    DOTNET_INT finalOutputLength;

    if (!EVP_DecryptFinal_ex(context, destination + outputLength, &finalOutputLength)) {
        EVP_CIPHER_CTX_free(context);
        return 0;
    }

    EVP_CIPHER_CTX_free(context);

    return outputLength + finalOutputLength;
}

DOTNET_INT SymmetricAlgorithmUtility_Encrypt_AES_128_CBC(DOTNET_READ_ONLY_SPAN_BYTE key, DOTNET_READ_ONLY_SPAN_BYTE iv, DOTNET_READ_ONLY_SPAN_BYTE source, DOTNET_INT sourceLength, DOTNET_SPAN_BYTE destination, DOTNET_BOOL padding) {
    return SymmetricAlgorithmUtility_Encrypt_EVP_CIPHER(EVP_aes_128_cbc(), key, iv, source, sourceLength, destination, padding);
}

DOTNET_INT SymmetricAlgorithmUtility_Encrypt_AES_192_CBC(DOTNET_READ_ONLY_SPAN_BYTE key, DOTNET_READ_ONLY_SPAN_BYTE iv, DOTNET_READ_ONLY_SPAN_BYTE source, DOTNET_INT sourceLength, DOTNET_SPAN_BYTE destination, DOTNET_BOOL padding) {
    return SymmetricAlgorithmUtility_Encrypt_EVP_CIPHER(EVP_aes_192_cbc(), key, iv, source, sourceLength, destination, padding);
}

DOTNET_INT SymmetricAlgorithmUtility_Encrypt_AES_256_CBC(DOTNET_READ_ONLY_SPAN_BYTE key, DOTNET_READ_ONLY_SPAN_BYTE iv, DOTNET_READ_ONLY_SPAN_BYTE source, DOTNET_INT sourceLength, DOTNET_SPAN_BYTE destination, DOTNET_BOOL padding) {
    return SymmetricAlgorithmUtility_Encrypt_EVP_CIPHER(EVP_aes_256_cbc(), key, iv, source, sourceLength, destination, padding);
}

DOTNET_INT SymmetricAlgorithmUtility_Encrypt_AES_256_CFB_8(DOTNET_READ_ONLY_SPAN_BYTE key, DOTNET_READ_ONLY_SPAN_BYTE iv, DOTNET_READ_ONLY_SPAN_BYTE source, DOTNET_INT sourceLength, DOTNET_SPAN_BYTE destination, DOTNET_BOOL padding) {
    return SymmetricAlgorithmUtility_Encrypt_EVP_CIPHER(EVP_aes_256_cfb8(), key, iv, source, sourceLength, destination, padding);
}

DOTNET_INT SymmetricAlgorithmUtility_Decrypt_AES_256_CFB_8(DOTNET_READ_ONLY_SPAN_BYTE key, DOTNET_READ_ONLY_SPAN_BYTE iv, DOTNET_READ_ONLY_SPAN_BYTE source, DOTNET_INT sourceLength, DOTNET_SPAN_BYTE destination) {
    return SymmetricAlgorithmUtility_Decrypt_EVP_CIPHER(EVP_aes_256_cfb8(), key, iv, source, sourceLength, destination);
}
