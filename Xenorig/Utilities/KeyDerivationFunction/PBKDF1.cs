using System;
using System.Runtime.InteropServices;

namespace Xenorig.Utilities.KeyDerivationFunction;

public partial class PBKDF1 : IDisposable
{
    private static partial class Native
    {
        [LibraryImport(Program.XenoNativeLibrary)]
        public static partial nint KeyDerivationFunctionUtility_CreatePBKDF1(ReadOnlySpan<byte> password, int passwordLength, ReadOnlySpan<byte> salt, int saltLength, int iterations, [MarshalAs(UnmanagedType.LPStr)] string hashName);

        [LibraryImport(Program.XenoNativeLibrary)]
        public static partial int KeyDerivationFunctionUtility_GetBytes(nint ctx, Span<byte> rgbOut, int cb);

        [LibraryImport(Program.XenoNativeLibrary)]
        public static partial void KeyDerivationFunctionUtility_Reset(nint ctx);

        [LibraryImport(Program.XenoNativeLibrary)]
        public static partial void KeyDerivationFunctionUtility_Free(nint ctx);
    }

    private readonly nint _context;

    public PBKDF1(ReadOnlySpan<byte> password, ReadOnlySpan<byte> salt, int iterations = 100, string hashName = "SHA1")
    {
        _context = Native.KeyDerivationFunctionUtility_CreatePBKDF1(password, password.Length, salt, salt.Length, iterations, hashName);
    }

    ~PBKDF1()
    {
        ReleaseUnmanagedResources();
    }

    public int FillBytes(Span<byte> rgbOut)
    {
        return Native.KeyDerivationFunctionUtility_GetBytes(_context, rgbOut, rgbOut.Length);
    }

    public int GetBytes(Span<byte> rgbOut, int cb)
    {
        return Native.KeyDerivationFunctionUtility_GetBytes(_context, rgbOut, cb);
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