using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.Security.Cryptography;
using System.Text;
using Xiropht_Connector_All.Setting;

namespace Xiropht_Connector_All.Utils
{
    /// <summary>
    ///     Class contains some method for helps development.
    /// </summary>
    public class ClassUtils
    {
        private static readonly List<string> ListOfCharacters = new List<string>
        {
            "a", "b", "c", "d", "e", "f", "g", "h", "i", "j", "k", "l", "m", "n", "o", "p", "q", "r", "s", "t", "u",
            "v", "w", "x", "y", "z"
        };

        private static readonly List<string> ListOfNumbers = new List<string>
            {"0", "1", "2", "3", "4", "5", "6", "7", "8", "9"};

        private static readonly List<string> ListOfSpecialCharacters =
            new List<string> {"&", "~", "#", "@", "'", "(", "|", "\\", ")", "="};



        /// <summary>
        ///     Generate a dynamic certificate, include the static genesis secondary key
        /// </summary>
        /// <returns></returns>
        public static string GenerateCertificate()
        {
            var certificate = ClassConnectorSetting.NETWORK_GENESIS_SECONDARY_KEY;
            for (var i = 0;
                i < ClassConnectorSetting.MAJOR_UPDATE_1_SECURITY_CERTIFICATE_SIZE_ITEM;
                i++)
            {
                var randomType = GetRandomBetween(0, 100);
                if (randomType <= 15) // LowerCase character
                    certificate += ListOfCharacters[GetRandomBetween(0, ListOfCharacters.Count - 1)];
                if (randomType > 15 && randomType <= 45) // Number
                    certificate += ListOfNumbers[GetRandomBetween(0, ListOfNumbers.Count - 1)];
                if (randomType > 45 && randomType <= 65) // UpperCase character.
                    certificate += ListOfCharacters[GetRandomBetween(0, ListOfCharacters.Count - 1)].ToUpper();
                if (randomType > 65) // Special Characters
                    certificate += ListOfSpecialCharacters[GetRandomBetween(0, ListOfSpecialCharacters.Count - 1)];
            }

            return certificate;
        }

        public static string CompressData(string data)
        {
            var byteData = ClassUtils.GetByteArrayFromString(data);
            using (MemoryStream output = new MemoryStream())
            {
                using (DeflateStream dstream = new DeflateStream(output, CompressionMode.Compress))
                {
                    dstream.Write(byteData, 0, byteData.Length);
                }
                return Convert.ToBase64String(output.ToArray());
            }
        }


        public static string FromHex(string hex)
        {
            var ba = ClassUtils.GetByteArrayFromString(hex);

            return BitConverter.ToString(ba).Replace("-", "");
        }

        /// <summary>
        ///     Génère un nombre réellement aléatoire.
        /// </summary>
        /// <param name="minimumValue"></param>
        /// <param name="maximumValue"></param>
        /// <returns></returns>
        public static int GetRandomBetween(int minimumValue, int maximumValue)
        {
            using (RNGCryptoServiceProvider Generator = new RNGCryptoServiceProvider())
            {
                var randomNumber = new byte[sizeof(int)];

                Generator.GetBytes(randomNumber);

                var asciiValueOfRandomCharacter = Convert.ToDouble(randomNumber[0]);

                var multiplier = Math.Max(0, asciiValueOfRandomCharacter / 255d - 0.00000000001d);

                var range = maximumValue - minimumValue + 1;

                var randomValueInRange = Math.Floor(multiplier * range);

                return (int)(minimumValue + randomValueInRange);
            }
        }

        /// <summary>
        ///     Génère un nombre réellement aléatoire en long.
        /// </summary>
        /// <param name="minimumValue"></param>
        /// <param name="maximumValue"></param>
        /// <returns></returns>
        public static long GetRandomBetweenLong(long minimumValue, long maximumValue)
        {
            using (RNGCryptoServiceProvider Generator = new RNGCryptoServiceProvider())
            {
                var randomNumber = new byte[sizeof(long)];

                Generator.GetBytes(randomNumber);

                var asciiValueOfRandomCharacter = Convert.ToDouble(randomNumber[0]);

                var multiplier = Math.Max(0, asciiValueOfRandomCharacter / 255d - 0.00000000001d);

                var range = maximumValue - minimumValue + 1;

                var randomValueInRange = Math.Floor(multiplier * range);

                return (long)(minimumValue + randomValueInRange);
            }
        }

        public static CultureInfo GlobalCultureInfo = new CultureInfo("fr-FR");
        /// <summary>
        ///     Translate hashrate from H/s into KH/s or other units.
        /// </summary>
        /// <param name="hashrate"></param>
        /// <returns></returns>
        public static string GetTranslateHashrate(string info, int decimalNumberLimit)
        {
            if (info != null)
            {
                info = info.Replace(".", ",");
                var hashrate = float.Parse(info, GlobalCultureInfo);
                if (hashrate >= 1000)
                    info = Math.Round(hashrate / 1000, decimalNumberLimit) + " KH/s";
                else if (hashrate >= 1000000)
                    info = Math.Round(hashrate / 1000000, decimalNumberLimit) + " MH/s";
                else if (hashrate >= 1000000000)
                    info = Math.Round(hashrate / 1000000000, decimalNumberLimit) + " GH/s";
                else if (hashrate >= 1000000000000)
                    info = Math.Round(hashrate / 1000000000000, decimalNumberLimit) + " TH/s";
                else if (hashrate >= 1000000000000000)
                    info = Math.Round(hashrate / 1000000000000000, decimalNumberLimit) + " PH/s";
                else
                    info = hashrate + " H/s";
            }

            return info;
        }

        /// <summary>
        ///     Translate big number string into readeable number string.
        /// </summary>
        /// <param name="info"></param>
        /// <returns></returns>
        public static string GetTranslateBigNumber(string info)
        {
            info = float.Parse(info).ToString("F8");
            return string.Format(info, GlobalCultureInfo);
        }

        public static long DateUnixTimeNowSecond()
        {
            var timeSpan = (DateTime.UtcNow - new DateTime(1970, 1, 1, 0, 0, 0));
            return (long)timeSpan.TotalSeconds;
        }

        public static long DateUnixTimeNowSecondConvertDate(DateTime date)
        {
            var timeSpan = (DateTime.UtcNow - date);
            return (long)timeSpan.TotalSeconds;
        }

        /// <summary>
        ///     Can clone a list object.
        /// </summary>
        /// <returns></returns>
        public static object CloneType(object objtype)
        {
            var lstfinal = new object();

            using (var memStream = new MemoryStream())
            {
                var binaryFormatter = new BinaryFormatter(null, new StreamingContext(StreamingContextStates.Clone));
                binaryFormatter.Serialize(memStream, objtype);
                memStream.Seek(0, SeekOrigin.Begin);
                lstfinal = binaryFormatter.Deserialize(memStream);
            }

            return lstfinal;
        }

   

        public static byte[] StrToByteArray(string str)
        {
            var hexindex = new Dictionary<string, byte>();
            for (var i = 0; i <= 255; i++)
                hexindex.Add(i.ToString("X2"), (byte) i);

            var hexres = new List<byte>();
            for (var i = 0; i < str.Length; i += 2)
                hexres.Add(hexindex[str.Substring(i, 2)]);

            return hexres.ToArray();
        }

        public static string DecompressData(string data)
        {
            using (MemoryStream input = new MemoryStream(Convert.FromBase64String(data)))
            {
                using (MemoryStream output = new MemoryStream())
                {
                    using (DeflateStream dstream = new DeflateStream(input, CompressionMode.Decompress))
                    {
                        dstream.CopyTo(output);
                    }
                    var outputData = output.ToArray();
                    return ClassUtils.GetStringFromByteArray(outputData, outputData.Length);
                }
            }
        }


        public static string GetStringFromByteArray(byte[] array, int received)
        {
            string result = string.Empty;

            for(int i = 0; i < received; i++)
            {
                if (i < received)
                {
                    result += (char)array[i];
                }
            }
            return result;
        }

        public static byte[] GetByteArrayFromString(string content)
        {
            byte[] result;
            List<byte> listByte = new List<byte>();
            for(int i = 0; i < content.Length; i++)
            {
                if (i < content.Length)
                {
                    listByte.Add((byte)content[i]);
                }
            }
            result = listByte.ToArray();
            listByte.Clear();
            return result;
        }

        public static string ConvertStringtoSHA512(string str)
        {
            var bytes = ClassUtils.GetByteArrayFromString(str);
            using (var hash = SHA512.Create())
            {
                var hashedInputBytes = hash.ComputeHash(bytes);

                var hashedInputStringBuilder = new StringBuilder(128);
                foreach (var b in hashedInputBytes)
                    hashedInputStringBuilder.Append(b.ToString("X2"));

                string hashToReturn = hashedInputStringBuilder.ToString();
                hashedInputStringBuilder.Clear();
                return hashToReturn;
            }
        }

        public static bool SocketIsConnected(TcpClient socket)
        {
            if (socket?.Client != null)
                try
                {
                    if (isClientConnected(socket))
                    {
                        return true;
                    }
                    return !(socket.Client.Poll(1, SelectMode.SelectRead) && socket.Available == 0);
                }
                catch
                {
                    return false;
                }

            return false;
        }

        public static bool isClientConnected(TcpClient ClientSocket)
        {
            try
            {
                var stateOfConnection = GetState(ClientSocket);


                if (stateOfConnection != TcpState.Closed && stateOfConnection != TcpState.CloseWait && stateOfConnection != TcpState.Closing && stateOfConnection != TcpState.Unknown)
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }
            catch
            {
                return false;
            }

        }

        public static TcpState GetState(TcpClient tcpClient)
        {
            var foo = IPGlobalProperties.GetIPGlobalProperties()
              .GetActiveTcpConnections()
              .FirstOrDefault(x => x.LocalEndPoint.Equals(tcpClient.Client.LocalEndPoint)
                                 && x.RemoteEndPoint.Equals(tcpClient.Client.RemoteEndPoint)
              );

            return foo != null ? foo.State : TcpState.Unknown;
        }
    }
}