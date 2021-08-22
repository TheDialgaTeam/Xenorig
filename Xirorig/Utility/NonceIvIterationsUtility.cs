using System;
using System.Runtime.InteropServices;
using System.Security.Cryptography;

namespace Xirorig.Utility
{
    internal static class NonceIvIterationsUtility
    {
        private const int KeyLength = 16;

        private static bool? _useSoftwareImplementation;

        public static unsafe bool IsNativeImplementationAvailable
        {
            get
            {
                if (_useSoftwareImplementation != null) return !_useSoftwareImplementation.GetValueOrDefault(false);

                try
                {
                    fixed (byte* passwordPtr = RandomNumberGenerator.GetBytes(1), saltPtr = RandomNumberGenerator.GetBytes(1), outputPtr = stackalloc byte[1])
                    {
                        doNonceIvIterationsMiningInstruction(passwordPtr, 1, saltPtr, 1, 1, 1, outputPtr);
                        _useSoftwareImplementation = false;
                    }
                }
                catch (Exception ex) when (ex is DllNotFoundException or EntryPointNotFoundException or BadImageFormatException)
                {
                    _useSoftwareImplementation = true;
                }

                return !_useSoftwareImplementation.GetValueOrDefault(false);
            }
        }

        public static unsafe byte[] DoNonceIvIterationsMiningInstruction(byte[] password, byte[] salt, int iterations)
        {
            if (!IsNativeImplementationAvailable) return SoftwareDoNonceIvIterationsMiningInstruction(password, salt, iterations);

            var output = new byte[KeyLength];

            fixed (byte* passwordPtr = password, saltPtr = salt, outputPtr = output)
            {
                doNonceIvIterationsMiningInstruction(passwordPtr, password.Length, saltPtr, salt.Length, iterations, KeyLength, outputPtr);
            }

            return output;
        }

        [DllImport("xirorig_native")]
        private static extern unsafe int doNonceIvIterationsMiningInstruction(byte* password, int passwordLength, byte* salt, int saltLength, int iterations, int keyLength, byte* output);

        private static byte[] SoftwareDoNonceIvIterationsMiningInstruction(byte[] password, byte[] salt, int iterations)
        {
            using var passwordDeriveBytes = new Rfc2898DeriveBytes(password, salt, iterations);
            return passwordDeriveBytes.GetBytes(KeyLength);
        }
    }
}