using System.Runtime.InteropServices;

namespace Xenorig.Utilities;

internal static class RandomNumberGeneratorUtility
{
    private static class Native
    {
        [DllImport(Program.XenoNativeLibrary)]
        public static extern int RandomNumberGeneratorUtility_GetRandomBetween_Int(int minimumValue, int maximumValue);

        [DllImport(Program.XenoNativeLibrary)]
        public static extern long RandomNumberGeneratorUtility_GetRandomBetween_Long(long minimumValue, long maximumValue);
    }

    public static int GetRandomBetween(int minimumValue, int maximumValue)
    {
        return Native.RandomNumberGeneratorUtility_GetRandomBetween_Int(minimumValue, maximumValue);
    }

    public static long GetRandomBetween(long minimumValue, long maximumValue)
    {
        return Native.RandomNumberGeneratorUtility_GetRandomBetween_Long(minimumValue, maximumValue);
    }
}