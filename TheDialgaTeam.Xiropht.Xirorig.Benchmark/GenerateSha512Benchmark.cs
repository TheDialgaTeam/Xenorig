using System.Security.Cryptography;
using System.Text;
using BenchmarkDotNet.Attributes;

namespace TheDialgaTeam.Xiropht.Xirorig.Benchmark
{
    public class GenerateSha512Benchmark
    {
        private static char[] Base16CharRepresentation { get; } = { '0', '1', '2', '3', '4', '5', '6', '7', '8', '9', 'A', 'B', 'C', 'D', 'E', 'F' };

        private static SHA512 Sha512 { get; } = SHA512.Create();
        private string TestData { get; }

        public GenerateSha512Benchmark()
        {
            TestData = "A8-60-BE-12-D0-5A-AF-8C-9F-84-5A-37-79-08-68-D3-84-00-36-CC-E9-EC-49-FB-4D-8D-B6-48-2D-CA-63-B2-17-91-42-00-3E-D4-DD-20-03-5E-0D-F6-C7-AF-E6-F4-B4-1F-97-60-A6-45-16-47-18-B7-D7-0F-AC-43-8D-84-21-1A-8C-88-70-C9-30-EB-6E-20-BD-4B-16-5B-17-48-F7-9F-93-4E-48-64-C6-14-35-6B-F7-18-7B-21-26-67-29-8F-D8-EE-8A-27-BB-70-08-DA-3C-AC-14-3B-59-7B-77-8D-E4-26-4D-7C-A7-36-20-34-59-2A-FE-1C-25-1B-BA-23-C5-42-5D-86-C9-10-CF-09-AE-01-80-A1-E2-37";
        }

        private static unsafe string GenerateSha512_Mono(string value)
        {
            var base16CharRepresentation = Base16CharRepresentation;
            var hashedInputBytes = Sha512.ComputeHash(Encoding.UTF8.GetBytes(value));
            var hashedInputBytesLength = hashedInputBytes.Length;
            var result = new string('\0', 128);

            fixed (char* charResult = result)
            {
                var charPtr = charResult;

                for (var i = 0; i < hashedInputBytesLength; i++)
                {
                    *charPtr = base16CharRepresentation[hashedInputBytes[i] >> 4];
                    charPtr++;
                    *charPtr = base16CharRepresentation[hashedInputBytes[i] & 15];
                    charPtr++;
                }
            }

            return result;
        }

        private static string GenerateSha512_NetCore(string value)
        {
            return string.Create(128, Sha512.ComputeHash(Encoding.UTF8.GetBytes(value)), (result, bytes) =>
            {
                var base16CharRepresentation = Base16CharRepresentation;

                for (var i = 0; i < bytes.Length; i++)
                {
                    result[i * 2] = base16CharRepresentation[bytes[i] >> 4];
                    result[i * 2 + 1] = base16CharRepresentation[bytes[i] & 15];
                }
            });
        }

        [Benchmark]
        public string GenerateSha512_Mono()
        {
            return GenerateSha512_Mono(TestData);
        }

        [Benchmark]
        public string GenerateSha512_NetCore()
        {
            return GenerateSha512_NetCore(TestData);
        }
    }
}