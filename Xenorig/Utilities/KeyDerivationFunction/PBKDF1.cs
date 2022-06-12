using System.Runtime.InteropServices;

namespace Xenorig.Utilities.KeyDerivationFunction;

internal class PBKDF1 : IDisposable
{
    private static class Native
    {
        [DllImport(Program.XenoNativeLibrary)]
        public static extern IntPtr KeyDerivationFunctionUtility_CreatePBKDF1(in byte password, int passwordLength, in byte salt, int saltLength, int iterations, [MarshalAs(UnmanagedType.LPStr)] string hashName);

        [DllImport(Program.XenoNativeLibrary)]
        public static extern int KeyDerivationFunctionUtility_GetBytes(IntPtr ctx, ref byte rgbOut, int cb);

        [DllImport(Program.XenoNativeLibrary)]
        public static extern void KeyDerivationFunctionUtility_Reset(IntPtr ctx);

        [DllImport(Program.XenoNativeLibrary)]
        public static extern void KeyDerivationFunctionUtility_Free(IntPtr ctx);
    }

    private readonly IntPtr _context;

    public PBKDF1(ReadOnlySpan<byte> password, ReadOnlySpan<byte> salt, int iterations = 100, string hashName = "SHA1")
    {
        _context = Native.KeyDerivationFunctionUtility_CreatePBKDF1(in MemoryMarshal.GetReference(password), password.Length, in MemoryMarshal.GetReference(salt), salt.Length, iterations, hashName);
    }

    ~PBKDF1()
    {
        ReleaseUnmanagedResources();
    }

    public int FillBytes(Span<byte> rgbOut)
    {
        return Native.KeyDerivationFunctionUtility_GetBytes(_context, ref MemoryMarshal.GetReference(rgbOut), rgbOut.Length);
    }

    public int GetBytes(Span<byte> rgbOut, int cb)
    {
        return Native.KeyDerivationFunctionUtility_GetBytes(_context, ref MemoryMarshal.GetReference(rgbOut), cb);
    }

    public void Reset()
    {
        Native.KeyDerivationFunctionUtility_Reset(_context);
    }

    private void ReleaseUnmanagedResources()
    {
        Native.KeyDerivationFunctionUtility_Free(_context);
    }

    public void Dispose()
    {
        ReleaseUnmanagedResources();
        GC.SuppressFinalize(this);
    }
}