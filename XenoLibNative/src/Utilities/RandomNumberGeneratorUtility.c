#include "RandomNumberGeneratorUtility.h"
#include "openssl/rand.h"

DOTNET_INT RandomNumberGeneratorUtility_GetRandomBetween_Int(DOTNET_INT minimumValue, DOTNET_INT maximumValue) {
    DOTNET_UINT range = maximumValue - minimumValue;

    if (range == 0) {
        return minimumValue;
    }

    if (range <= DOTNET_BYTE_MAX) {
        DOTNET_BYTE maxValue = (range | (range >> 1));
        maxValue |= maxValue >> 2;
        maxValue |= maxValue >> 4;

        DOTNET_BYTE span[1];
        DOTNET_BYTE result;

        do {
            if (!RAND_bytes(span, 1)) {
                return minimumValue;
            }

            result = maxValue & span[0];
        } while (result > range);

        return result + minimumValue;
    } else if (range <= DOTNET_USHORT_MAX) {
        DOTNET_USHORT maxValue = (range | (range >> 1));
        maxValue |= maxValue >> 2;
        maxValue |= maxValue >> 4;
        maxValue |= maxValue >> 8;

        DOTNET_USHORT span[1];
        DOTNET_USHORT result;

        do {
            if (!RAND_bytes((DOTNET_SPAN_BYTE) span, 2)) {
                return minimumValue;
            }

            result = maxValue & span[0];
        } while (result > range);

        return result + minimumValue;
    } else {
        DOTNET_UINT maxValue = (range | (range >> 1));
        maxValue |= maxValue >> 2;
        maxValue |= maxValue >> 4;
        maxValue |= maxValue >> 8;
        maxValue |= maxValue >> 16;

        DOTNET_UINT span[1];
        DOTNET_UINT result;

        do {
            if (!RAND_bytes((DOTNET_SPAN_BYTE) span, 4)) {
                return minimumValue;
            }

            result = maxValue & span[0];
        } while (result > range);

        return result + minimumValue;
    }
}

DOTNET_LONG RandomNumberGeneratorUtility_GetRandomBetween_Long(DOTNET_LONG minimumValue, DOTNET_LONG maximumValue) {
    DOTNET_ULONG range = maximumValue - minimumValue;

    if (range == 0) {
        return minimumValue;
    }

    if (range <= DOTNET_BYTE_MAX) {
        DOTNET_BYTE maxValue = (range | (range >> 1));
        maxValue |= maxValue >> 2;
        maxValue |= maxValue >> 4;

        DOTNET_BYTE span[1];
        DOTNET_BYTE result;

        do {
            if (!RAND_bytes(span, 1)) {
                return minimumValue;
            }

            result = maxValue & span[0];
        } while (result > range);

        return result + minimumValue;
    } else if (range <= DOTNET_USHORT_MAX) {
        DOTNET_USHORT maxValue = (range | (range >> 1));
        maxValue |= maxValue >> 2;
        maxValue |= maxValue >> 4;
        maxValue |= maxValue >> 8;

        DOTNET_USHORT span[1];
        DOTNET_USHORT result;

        do {
            if (!RAND_bytes((DOTNET_SPAN_BYTE) span, 2)) {
                return minimumValue;
            }

            result = maxValue & span[0];
        } while (result > range);

        return result + minimumValue;
    } else if (range <= DOTNET_UINT_MAX) {
        DOTNET_UINT maxValue = (range | (range >> 1));
        maxValue |= maxValue >> 2;
        maxValue |= maxValue >> 4;
        maxValue |= maxValue >> 8;
        maxValue |= maxValue >> 16;

        DOTNET_UINT span[1];
        DOTNET_UINT result;

        do {
            if (!RAND_bytes((DOTNET_SPAN_BYTE) span, 4)) {
                return minimumValue;
            }

            result = maxValue & span[0];
        } while (result > range);

        return result + minimumValue;
    } else {
        DOTNET_ULONG maxValue = (range | (range >> 1));
        maxValue |= maxValue >> 2;
        maxValue |= maxValue >> 4;
        maxValue |= maxValue >> 8;
        maxValue |= maxValue >> 16;
        maxValue |= maxValue >> 32;

        DOTNET_ULONG span[1];
        DOTNET_ULONG result;

        do {
            if (!RAND_bytes((DOTNET_SPAN_BYTE) span, 8)) {
                return minimumValue;
            }

            result = maxValue & span[0];
        } while (result > range);

        return result + minimumValue;
    }
}
