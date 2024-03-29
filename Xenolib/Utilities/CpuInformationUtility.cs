﻿using System.Runtime.InteropServices;
using System.Runtime.Intrinsics.X86;
using System.Runtime.Versioning;

namespace Xenolib.Utilities;

[UnsupportedOSPlatform("browser")]
public static partial class CpuInformationUtility
{
    private static partial class Native
    {
        [LibraryImport(Program.XenoNativeLibrary)]
        [return: MarshalAs(UnmanagedType.LPStr)]
        public static partial string CpuInformationUtility_GetProcessorName();

        [LibraryImport(Program.XenoNativeLibrary)]
        public static partial int CpuInformationUtility_GetProcessorL2Cache();

        [LibraryImport(Program.XenoNativeLibrary)]
        public static partial int CpuInformationUtility_GetProcessorL3Cache();

        [LibraryImport(Program.XenoNativeLibrary)]
        public static partial int CpuInformationUtility_GetProcessorCoreCount();
    }

    public static string ProcessorName { get; }

    public static string ProcessorInstructionSetsSupported { get; }

    public static int ProcessorL2Cache { get; }

    public static int ProcessorL3Cache { get; }

    public static int ProcessorCoreCount { get; }

    public static int ProcessorThreadCount { get; } = Environment.ProcessorCount;

    static CpuInformationUtility()
    {
        ProcessorName = Native.CpuInformationUtility_GetProcessorName();

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

        ProcessorL2Cache = Native.CpuInformationUtility_GetProcessorL2Cache();
        ProcessorL3Cache = Native.CpuInformationUtility_GetProcessorL3Cache();

        ProcessorCoreCount = Native.CpuInformationUtility_GetProcessorCoreCount();

        if (ProcessorCoreCount == 0)
        {
            ProcessorCoreCount = Environment.ProcessorCount / 2;
        }
    }
}