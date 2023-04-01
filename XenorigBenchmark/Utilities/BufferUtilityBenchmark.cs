using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using Xenorig.Utilities;

namespace XenorigBenchmark.Utilities;

[SimpleJob(RuntimeMoniker.Net70)]
public class BufferUtilityBenchmark
{
    [Benchmark]
    [SkipLocalsInit]
    public void MemoryCopyLong()
    {
        Span<long> source = stackalloc long[256];
        Span<long> destination = stackalloc long[256];
        
        RandomNumberGenerator.Fill(MemoryMarshal.AsBytes(source));
        source[..256].CopyTo(destination);
    }

    [Benchmark]
    [SkipLocalsInit]
    public void MemoryCopyLongNative()
    {
        Span<long> source = stackalloc long[256];
        Span<long> destination = stackalloc long[256];
        
        RandomNumberGenerator.Fill(MemoryMarshal.AsBytes(source));
        BufferUtility.MemoryCopy(source, destination, 256);
    }
}