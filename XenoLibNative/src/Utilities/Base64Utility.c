#include "Base64Utility.h"

DOTNET_PRIVATE DOTNET_BYTE Base64Characters[] = {
    'A', 'B', 'C', 'D', 'E', 'F', 'G', 'H', 'I', 'J', 'K', 'L', 'M',
    'N', 'O', 'P', 'Q', 'R', 'S', 'T', 'U', 'V', 'W', 'X', 'Y', 'Z',
    'a', 'b', 'c', 'd', 'e', 'f', 'g', 'h', 'i', 'j', 'k', 'l', 'm',
    'n', 'o', 'p', 'q', 'r', 's', 't', 'u', 'v', 'w', 'x', 'y', 'z',
    '0', '1', '2', '3', '4', '5', '6', '7', '8', '9', '+', '/', '='};

DOTNET_PRIVATE DOTNET_BYTE DecodeByte(DOTNET_BYTE value) {
    if (value >= 'A' && value <= 'Z') {
        return value - 'A';
    } else if (value >= 'a' && value <= 'z') {
        return value - 'a' + 26;
    } else if (value >= '0' && value <= '9') {
        return value - '0' + 52;
    } else if (value == '+') {
        return 62;
    } else {
        return 63;
    }
}

DOTNET_INT Base64Utility_EncodeLength(DOTNET_INT inputLength) {
    return 4 * ((inputLength + 3 - 1) / 3);
}

DOTNET_INT Base64Utility_DecodeLength(DOTNET_READ_ONLY_SPAN_BYTE input, DOTNET_INT inputLength) {
    if (input == NULL || inputLength % 4 != 0) {
        return 0;
    }

    DOTNET_INT paddingLength = 0;

    for (DOTNET_INT i = inputLength - 1; i >= 0; i--) {
        if (input[i] != '=') {
            break;
        }

        paddingLength++;
    }

    return 3 * (inputLength / 4) - paddingLength;
}

DOTNET_INT Base64Utility_Encode(DOTNET_READ_ONLY_SPAN_BYTE input, DOTNET_INT inputLength, DOTNET_SPAN_BYTE output) {
    if (input == NULL || inputLength == 0 || output == NULL) {
        return 0;
    }

    DOTNET_INT paddingLength = inputLength % 3;
    DOTNET_INT noPaddingLength = inputLength - paddingLength;
    DOTNET_INT encodeLength = 0;

    for (DOTNET_INT i = 0; i < noPaddingLength; i += 3) {
        output[encodeLength++] = Base64Characters[(input[i] & 0xfc) >> 2];
        output[encodeLength++] = Base64Characters[((input[i] & 0x03) << 4) | ((input[i + 1] & 0xf0) >> 4)];
        output[encodeLength++] = Base64Characters[((input[i + 1] & 0x0f) << 2) | ((input[i + 2] & 0xc0) >> 6)];
        output[encodeLength++] = Base64Characters[input[i + 2] & 0x3f];
    }

    switch (paddingLength) {
        case 2:
            output[encodeLength++] = Base64Characters[(input[noPaddingLength] & 0xfc) >> 2];
            output[encodeLength++] = Base64Characters[((input[noPaddingLength] & 0x03) << 4) | ((input[noPaddingLength + 1] & 0xf0) >> 4)];
            output[encodeLength++] = Base64Characters[(input[noPaddingLength + 1] & 0x0f) << 2];
            output[encodeLength++] = Base64Characters[64];
            break;

        case 1:
            output[encodeLength++] = Base64Characters[(input[noPaddingLength] & 0xfc) >> 2];
            output[encodeLength++] = Base64Characters[((input[noPaddingLength] & 0x03) << 4)];
            output[encodeLength++] = Base64Characters[64];
            output[encodeLength++] = Base64Characters[64];
            break;

        default:
            break;
    }

    return encodeLength;
}

DOTNET_INT Base64Utility_Decode(DOTNET_READ_ONLY_SPAN_BYTE input, DOTNET_INT inputLength, DOTNET_SPAN_BYTE output) {
    if (input == NULL || inputLength % 4 != 0 || output == NULL) {
        return 0;
    }

    DOTNET_INT paddingLength = 0;

    for (DOTNET_INT i = inputLength - 1; i >= 0; i--) {
        if (input[i] != '=') {
            break;
        }

        paddingLength++;
    }

    DOTNET_INT noPaddingLength = 4 * ((inputLength - paddingLength) / 4);
    DOTNET_INT decodeLength = 0;

    for (DOTNET_INT i = 0; i < noPaddingLength; i += 4) {
        output[decodeLength++] = (DecodeByte(input[i]) << 2) + (DecodeByte(input[i + 1]) >> 4);
        output[decodeLength++] = ((DecodeByte(input[i + 1]) & 0x0f) << 4) + (DecodeByte(input[i + 2]) >> 2);
        output[decodeLength++] = ((DecodeByte(input[i + 2]) & 0x03) << 6) + DecodeByte(input[i + 3]);
    }

    switch (paddingLength) {
        case 1:
            output[decodeLength++] = (DecodeByte(input[noPaddingLength]) << 2) + (DecodeByte(input[noPaddingLength + 1]) >> 4);
            output[decodeLength++] = ((DecodeByte(input[noPaddingLength + 1]) & 0x0f) << 4) + (DecodeByte(input[noPaddingLength + 2]) >> 2);
            break;

        case 2:
            output[decodeLength++] = (DecodeByte(input[noPaddingLength]) << 2) + (DecodeByte(input[noPaddingLength + 1]) >> 4);
            break;

        default:
            break;
    }

    return decodeLength;
}
