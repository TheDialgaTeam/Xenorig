using System;
using System.Diagnostics;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using Xiropht_Connector_All.Seed;

namespace Xiropht_Connector_All.Utils
{
    public class ClassAlgoErrorEnumeration
    {
        public const string AlgoError = "WRONG";
    }

    public class ClassAlgoEnumeration
    {
        public const string Rijndael = "RIJNDAEL"; // 0
        public const string Xor = "XOR"; // 1
    }

    public class ClassAlgo
    {
        /// <summary>
        ///     Decrypt the result received and retrieve it.
        /// </summary>
        /// <param name="idAlgo"></param>
        /// <param name="result"></param>
        /// <param name="key"></param>
        /// <param name="size"></param>
        /// <returns></returns>
        public static string GetDecryptedResult(string idAlgo, string result, int size, byte[] AesIv, byte[] AesSalt)
        {
            if (result == ClassSeedNodeStatus.SeedNone)
            {
                return result;
            }

            try
            {

                switch (idAlgo)
                {
                    case ClassAlgoEnumeration.Rijndael:
                        using (var decrypt = new Rijndael())
                        {
                            return decrypt.DecryptString(result, size, AesIv, AesSalt);
                        }
                    case ClassAlgoEnumeration.Xor:
                        break;
                }
            }
            catch (Exception erreur)
            {

                return ClassAlgoErrorEnumeration.AlgoError;
            }

            return ClassAlgoErrorEnumeration.AlgoError;
        }


        /// <summary>
        ///     Decrypt the result received and retrieve it.
        /// </summary>
        /// <param name="idAlgo"></param>
        /// <param name="result"></param>
        /// <param name="key"></param>
        /// <param name="size"></param>
        /// <returns></returns>
        public static string GetDecryptedResultManual(string idAlgo, string result, string key, int size)
        {
            if (result == ClassSeedNodeStatus.SeedNone)
            {
                return result;
            }

            try
            {
                switch (idAlgo)
                {
                    case ClassAlgoEnumeration.Rijndael:
                        using (var decrypt = new Rijndael())
                        {
                            return decrypt.DecryptStringManual(result, key, size);
                        }
                    case ClassAlgoEnumeration.Xor:
                        break;
                }
            }
            catch (Exception erreur)
            {
#if DEBUG
                Debug.WriteLine("Error Decrypt of " + result + " with key: "+key+" : " + erreur.Message);
#endif
                return ClassAlgoErrorEnumeration.AlgoError;
            }

            return ClassAlgoErrorEnumeration.AlgoError;
        }

        /// <summary>
        ///     Encrypt the result received and retrieve it.
        /// </summary>
        /// <param name="idAlgo"></param>
        /// <param name="result"></param>
        /// <param name="key"></param>
        /// <param name="size"></param>
        /// <returns></returns>
        public static string GetEncryptedResult(string idAlgo, string result, int size, byte[] AesIv, byte[] AesSalt)
        {
            try
            {
                switch (idAlgo)
                {
                    case ClassAlgoEnumeration.Rijndael:
                        using (var encrypt = new Rijndael())
                        {
                            return encrypt.EncryptString(result, size, AesIv, AesSalt);
                        }
                    case ClassAlgoEnumeration.Xor:
                        break;
                }
            }
            catch (Exception erreur)
            {
#if DEBUG
                Debug.WriteLine("Error Encrypt of " + result + " : " + erreur.Message);
#endif
                return ClassAlgoErrorEnumeration.AlgoError;
            }

            return ClassAlgoErrorEnumeration.AlgoError;
        }

        /// <summary>
        ///     Encrypt the result received and retrieve it.
        /// </summary>
        /// <param name="idAlgo"></param>
        /// <param name="result"></param>
        /// <param name="key"></param>
        /// <param name="size"></param>
        /// <returns></returns>
        public static string GetEncryptedResultManual(string idAlgo, string result, string key, int size)
        {
            try
            {
                switch (idAlgo)
                {
                    case ClassAlgoEnumeration.Rijndael:
                        using (var encrypt = new Rijndael())
                        {
                            return encrypt.EncryptStringManual(result, key, size);
                        }
                    case ClassAlgoEnumeration.Xor:
                        break;
                }
            }
            catch (Exception erreur)
            {
#if DEBUG
                Debug.WriteLine("Error Encrypt of " + result + " : " + erreur.Message);
#endif
                return ClassAlgoErrorEnumeration.AlgoError;
            }

            return ClassAlgoErrorEnumeration.AlgoError;
        }

        /// <summary>
        ///     Return an algo name from id.
        /// </summary>
        /// <param name="idAlgo"></param>
        /// <returns></returns>
        public static string GetNameAlgoFromId(int idAlgo)
        {
            switch (idAlgo)
            {
                case 0:
                    return ClassAlgoEnumeration.Rijndael;
                case 1:
                    return ClassAlgoEnumeration.Xor;
            }

            return "NONE";
        }
    }


    public class Rijndael : IDisposable
    {

        private  bool disposed;


        ~Rijndael()
        {
            Dispose(false);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        // Protected implementation of Dispose pattern.
        protected virtual void Dispose(bool disposing)
        {
            if (disposed)
                return;

            disposed = true;
        }

        /// <summary>
        /// Encrypt string from Rijndael.
        /// </summary>
        /// <param name="plainText"></param>
        /// <param name="passPhrase"></param>
        /// <param name="keysize"></param>
        /// <returns></returns>
        public string EncryptString(string plainText,  int keysize, byte[] AesIv, byte[] AesSalt)
        {

            using (var symmetricKey = new AesCryptoServiceProvider() { Mode = CipherMode.CFB })
            {
                symmetricKey.BlockSize = 128;
                symmetricKey.KeySize = keysize;
                symmetricKey.Padding = PaddingMode.PKCS7;
                symmetricKey.Key = AesIv;
                using (ICryptoTransform encryptor = symmetricKey.CreateEncryptor(AesIv, AesSalt))
                {
                    using (MemoryStream memoryStream = new MemoryStream())
                    {
                        using (CryptoStream cryptoStream = new CryptoStream(memoryStream, encryptor, CryptoStreamMode.Write))
                        {
                            byte[] plainTextBytes = Encoding.UTF8.GetBytes(plainText);
                            cryptoStream.Write(plainTextBytes, 0, plainTextBytes.Length);
                            cryptoStream.FlushFinalBlock();
                            byte[] cipherTextBytes = memoryStream.ToArray();
                            return Convert.ToBase64String(cipherTextBytes);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Decrypt string with Rijndael.
        /// </summary>
        /// <param name="cipherText"></param>
        /// <param name="passPhrase"></param>
        /// <param name="keysize"></param>
        /// <returns></returns>
        public string DecryptString(string cipherText, int keysize, byte[] AesIv, byte[] AesSalt)
        {
            using (var symmetricKey = new AesCryptoServiceProvider() { Mode = CipherMode.CFB })
            {
                symmetricKey.BlockSize = 128;
                symmetricKey.KeySize = keysize;
                symmetricKey.Padding = PaddingMode.PKCS7;
                symmetricKey.Key = AesIv;
                using (ICryptoTransform decryptor = symmetricKey.CreateDecryptor(AesIv, AesSalt))
                {
                    byte[] cipherTextBytes = Convert.FromBase64String(cipherText);
                    using (MemoryStream memoryStream = new MemoryStream(cipherTextBytes))
                    {
                        using (CryptoStream cryptoStream = new CryptoStream(memoryStream, decryptor, CryptoStreamMode.Read))
                        {
                            byte[] plainTextBytes = new byte[cipherTextBytes.Length];
                            int decryptedByteCount = cryptoStream.Read(plainTextBytes, 0, plainTextBytes.Length);
                            return Encoding.UTF8.GetString(plainTextBytes, 0, decryptedByteCount);
                        }
                    }
                }
            }
        }


        /// <summary>
        /// Encrypt string from Rijndael.
        /// </summary>
        /// <param name="plainText"></param>
        /// <param name="passPhrase"></param>
        /// <param name="keysize"></param>
        /// <returns></returns>
        public string EncryptStringManual(string plainText, string passPhrase, int keysize)
        {
            using (PasswordDeriveBytes password = new PasswordDeriveBytes(passPhrase, Encoding.UTF8.GetBytes(ClassUtils.FromHex(passPhrase.Substring(0, 8)))))
            {
                byte[] keyBytes = password.GetBytes(keysize / 8);
                using (var symmetricKey = new AesCryptoServiceProvider() { Mode = CipherMode.CFB })
                {
                    byte[] initVectorBytes = password.GetBytes(16);
                    symmetricKey.BlockSize = 128;
                    symmetricKey.KeySize = keysize;
                    symmetricKey.Padding = PaddingMode.PKCS7;
                    symmetricKey.Key = keyBytes;
                    using (ICryptoTransform encryptor = symmetricKey.CreateEncryptor(keyBytes, initVectorBytes))
                    {
                        using (MemoryStream memoryStream = new MemoryStream())
                        {
                            using (CryptoStream cryptoStream = new CryptoStream(memoryStream, encryptor, CryptoStreamMode.Write))
                            {
                                byte[] plainTextBytes = Encoding.UTF8.GetBytes(plainText);
                                cryptoStream.Write(plainTextBytes, 0, plainTextBytes.Length);
                                cryptoStream.FlushFinalBlock();
                                byte[] cipherTextBytes = memoryStream.ToArray();
                                return Convert.ToBase64String(cipherTextBytes);
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Decrypt string with Rijndael.
        /// </summary>
        /// <param name="cipherText"></param>
        /// <param name="passPhrase"></param>
        /// <param name="keysize"></param>
        /// <returns></returns>
        public string DecryptStringManual(string cipherText, string passPhrase, int keysize)
        {
            using (PasswordDeriveBytes password = new PasswordDeriveBytes(passPhrase, Encoding.UTF8.GetBytes(ClassUtils.FromHex(passPhrase.Substring(0, 8)))))
            {

                byte[] keyBytes = password.GetBytes(keysize / 8);
                using (var symmetricKey = new AesCryptoServiceProvider() { Mode = CipherMode.CFB })
                {
                    byte[] initVectorBytes = password.GetBytes(16);
                    symmetricKey.BlockSize = 128;
                    symmetricKey.KeySize = keysize;
                    symmetricKey.Padding = PaddingMode.PKCS7;
                    symmetricKey.Key = keyBytes;
                    using (ICryptoTransform decryptor = symmetricKey.CreateDecryptor(keyBytes, initVectorBytes))
                    {
                        byte[] cipherTextBytes = Convert.FromBase64String(cipherText);
                        using (MemoryStream memoryStream = new MemoryStream(cipherTextBytes))
                        {
                            using (CryptoStream cryptoStream = new CryptoStream(memoryStream, decryptor, CryptoStreamMode.Read))
                            {
                                byte[] plainTextBytes = new byte[cipherTextBytes.Length];
                                int decryptedByteCount = cryptoStream.Read(plainTextBytes, 0, plainTextBytes.Length);
                                return Encoding.UTF8.GetString(plainTextBytes, 0, decryptedByteCount);
                            }
                        }
                    }
                }
            }
        }
    }
}