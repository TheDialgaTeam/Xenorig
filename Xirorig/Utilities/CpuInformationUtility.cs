// Xirorig
// Copyright 2021 Yong Jian Ming
// 
// This program is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
// 
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
// 
// You should have received a copy of the GNU General Public License
// along with this program.  If not, see <http://www.gnu.org/licenses/>.

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics.X86;

namespace Xirorig.Utilities
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