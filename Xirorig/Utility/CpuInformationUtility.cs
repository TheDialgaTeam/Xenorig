using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics.X86;

namespace Xirorig.Utility
{
    internal static class CpuInformationUtility
    {
        private static class Native
        {
            [DllImport(Program.XirorigNativeLibrary)]
            public static extern string CpuInformationUtility_GetProcessorName();

            [DllImport(Program.XirorigNativeLibrary)]
            public static extern ulong CpuInformationUtility_GetProcessorL2Cache();

            [DllImport(Program.XirorigNativeLibrary)]
            public static extern ulong CpuInformationUtility_GetProcessorL3Cache();

            [DllImport(Program.XirorigNativeLibrary)]
            public static extern ulong CpuInformationUtility_GetProcessorCoreCount();
        }

        public static string ProcessorName { get; }

        public static string ProcessorInstructionSetsSupported { get; }

        public static ulong ProcessorL2Cache { get; }

        public static ulong ProcessorL3Cache { get; }

        public static ulong ProcessorCoreCount { get; }

        public static ulong ProcessorThreadCount { get; } = Convert.ToUInt64(Environment.ProcessorCount);

        static CpuInformationUtility()
        {
            try
            {
                ProcessorName = Native.CpuInformationUtility_GetProcessorName();
            }
            catch (Exception)
            {
                ProcessorName = "Unknown CPU";
            }

            var cpuInstructionSets = new List<string>();

            if (Aes.IsSupported)
            {
                cpuInstructionSets.Add("AES-NI");
            }

            if (Sse.IsSupported)
            {
                cpuInstructionSets.Add("SSE");
            }

            if (Sse2.IsSupported)
            {
                cpuInstructionSets.Add("SSE-2");
            }

            if (Sse3.IsSupported)
            {
                cpuInstructionSets.Add("SSE-3");
            }

            if (Sse41.IsSupported)
            {
                cpuInstructionSets.Add("SSE4.1");
            }

            if (Sse42.IsSupported)
            {
                cpuInstructionSets.Add("SSE4.2");
            }

            if (Avx.IsSupported)
            {
                cpuInstructionSets.Add("AVX");
            }

            if (Avx2.IsSupported)
            {
                cpuInstructionSets.Add("AVX2");
            }

            ProcessorInstructionSetsSupported = string.Join(" ", cpuInstructionSets);

            try
            {
                ProcessorL2Cache = Native.CpuInformationUtility_GetProcessorL2Cache();
                ProcessorL3Cache = Native.CpuInformationUtility_GetProcessorL3Cache();
            }
            catch (Exception)
            {
                ProcessorL2Cache = 0;
                ProcessorL3Cache = 0;
            }

            try
            {
                ProcessorCoreCount = Native.CpuInformationUtility_GetProcessorCoreCount();
            }
            catch (Exception)
            {
                ProcessorCoreCount = Convert.ToUInt64(Environment.ProcessorCount) / 2;
            }
        }
    }
}