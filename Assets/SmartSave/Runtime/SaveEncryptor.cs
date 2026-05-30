using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace SmartSave
{
    /// <summary>
    /// Cryptographic helper class to secure save data using AES encryption.
    /// </summary>
    public static class SaveEncryptor
    {
        private static readonly byte[] Salt = Encoding.ASCII.GetBytes("SmartSaveDefaultSaltValue");

        /// <summary>
        /// Encrypts plain text using AES with a given password key.
        /// </summary>
        public static string Encrypt(string plainText, string password)
        {
            if (string.IsNullOrEmpty(plainText)) return plainText;
            if (string.IsNullOrEmpty(password)) return plainText;

            try
            {
                byte[] plainBytes = Encoding.UTF8.GetBytes(plainText);
                byte[] encryptedBytes;

                using (Aes aes = Aes.Create())
                {
                    using (var rfc = new Rfc2898DeriveBytes(password, Salt, 1000))
                    {
                        aes.Key = rfc.GetBytes(32); // 256-bit key
                        aes.IV = rfc.GetBytes(16);  // 128-bit IV
                    }

                    using (MemoryStream ms = new MemoryStream())
                    {
                        using (CryptoStream cs = new CryptoStream(ms, aes.CreateEncryptor(), CryptoStreamMode.Write))
                        {
                            cs.Write(plainBytes, 0, plainBytes.Length);
                            cs.FlushFinalBlock();
                        }
                        encryptedBytes = ms.ToArray();
                    }
                }

                return Convert.ToBase64String(encryptedBytes);
            }
            catch (Exception ex)
            {
                UnityEngine.Debug.LogError($"[SmartSave] Encryption failed: {ex.Message}");
                return plainText;
            }
        }

        /// <summary>
        /// Decrypts encrypted text using AES with a given password key.
        /// </summary>
        public static string Decrypt(string cipherText, string password)
        {
            if (string.IsNullOrEmpty(cipherText)) return cipherText;
            if (string.IsNullOrEmpty(password)) return cipherText;

            try
            {
                byte[] cipherBytes = Convert.FromBase64String(cipherText);

                using (Aes aes = Aes.Create())
                {
                    using (var rfc = new Rfc2898DeriveBytes(password, Salt, 1000))
                    {
                        aes.Key = rfc.GetBytes(32);
                        aes.IV = rfc.GetBytes(16);
                    }

                    using (MemoryStream ms = new MemoryStream())
                    {
                        using (CryptoStream cs = new CryptoStream(ms, aes.CreateDecryptor(), CryptoStreamMode.Write))
                        {
                            cs.Write(cipherBytes, 0, cipherBytes.Length);
                            cs.FlushFinalBlock();
                        }
                        return Encoding.UTF8.GetString(ms.ToArray());
                    }
                }
            }
            catch (Exception ex)
            {
                UnityEngine.Debug.LogWarning($"[SmartSave] Decryption failed (file may not be encrypted): {ex.Message}");
                return cipherText;
            }
        }
    }
}
