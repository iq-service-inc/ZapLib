using System;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace ZapLib.Security
{
    /// <summary>
    /// 訊息加解密輔助工具
    /// </summary>
    public class Crypto
    {
        /// <summary>
        /// AES 加密初始化向量。預設加密時會自動產生 Base64 格式的 16-byte IV。
        /// </summary>
        public string IV { get; set; }

        /// <summary>
        /// 加密類別使用的編碼方式，可自行指定為 UTF8 (預設為 ASCII)
        /// </summary>
        public Encoding CryptoEncoding { get; set; }

        /// <summary>
        /// 建構子
        /// </summary>
        /// <param name="encoding">編碼方式，預設為 ASCII，可自行修改為 UTF8</param>
        public Crypto(Encoding encoding = null)
        {
            if (encoding == null) encoding = Encoding.ASCII;
            CryptoEncoding = encoding;
        }

        /// <summary>
        /// MD5 雜湊資料
        /// </summary>
        /// <param name="content">資料</param>
        /// <returns>雜湊後的資料</returns>
        public string Md5(string content = "")
        {
            byte[] source = CryptoEncoding.GetBytes(content);
            byte[] crypto = Utility.MD5.ComputeHash(source);
            return Convert.ToBase64String(crypto);
        }

        /// <summary>
        /// AES 加密。使用 AES-CBC 搭配 HMAC-SHA256 驗證密文完整性。
        /// </summary>
        /// <param name="content">原始內容</param>
        /// <param name="iv">初始化向量，可傳入 Base64 格式的 16-byte IV 或 16-byte 字串</param>
        /// <returns>Base64 格式的加密內容，內含 HMAC 驗證碼</returns>
        public string AESEncryption(string content, string iv = null)
        {
            byte[] ivBytes = GetIVBytes(iv, true);
            byte[] source = CryptoEncoding.GetBytes(content);
            byte[] encrypted;

            using (Aes aes = Aes.Create())
            {
                aes.Key = DeriveKey("ZapLib.Crypto.AES.EncryptionKey.v1", 32);
                aes.IV = ivBytes;
                aes.Mode = CipherMode.CBC;
                aes.Padding = PaddingMode.PKCS7;

                using (ICryptoTransform encryptor = aes.CreateEncryptor())
                {
                    encrypted = encryptor.TransformFinalBlock(source, 0, source.Length);
                }
            }

            byte[] tag = ComputeHMAC(ivBytes, encrypted);
            return Convert.ToBase64String(Concat(encrypted, tag));
        }

        /// <summary>
        /// AES 解密。密文若遭竄改、IV 錯誤或金鑰不同，會丟出 <see cref="CryptographicException"/>。
        /// </summary>
        /// <param name="content">加密後的內容</param>
        /// <param name="iv">初始化向量</param>
        /// <returns>解密後的內容</returns>
        public string AESDecryption(string content, string iv)
        {
            byte[] ivBytes = GetIVBytes(iv, false);
            byte[] payload = Convert.FromBase64String(content);
            const int tagSize = 32;
            if (payload.Length <= tagSize) throw new CryptographicException("AES payload is invalid.");

            byte[] encrypted = new byte[payload.Length - tagSize];
            byte[] expectedTag = new byte[tagSize];
            Buffer.BlockCopy(payload, 0, encrypted, 0, encrypted.Length);
            Buffer.BlockCopy(payload, encrypted.Length, expectedTag, 0, expectedTag.Length);

            byte[] actualTag = ComputeHMAC(ivBytes, encrypted);
            if (!FixedTimeEquals(actualTag, expectedTag))
                throw new CryptographicException("AES payload authentication failed.");

            using (Aes aes = Aes.Create())
            {
                aes.Key = DeriveKey("ZapLib.Crypto.AES.EncryptionKey.v1", 32);
                aes.IV = ivBytes;
                aes.Mode = CipherMode.CBC;
                aes.Padding = PaddingMode.PKCS7;

                using (ICryptoTransform decryptor = aes.CreateDecryptor())
                {
                    byte[] decrypted = decryptor.TransformFinalBlock(encrypted, 0, encrypted.Length);
                    return CryptoEncoding.GetString(decrypted);
                }
            }
        }

        /// <summary>
        /// 產生指定長度的亂數字串 [A-Za-z0-9]
        /// </summary>
        /// <param name="len">指定長度</param>
        /// <returns>亂數字串</returns>
        public string RandomString(int len)
        {
            Random random = new Random();
            const string chars = "AaBbCcDdEeFfGgHhIiJjKkLlMmNnOoPpQqRrSsTtUuVvWwXxYyZz0123456789";
            return new string(Enumerable.Repeat(chars, len).Select(s => s[random.Next(s.Length)]).ToArray());
        }

        private byte[] GetIVBytes(string iv, bool generateIfEmpty)
        {
            if (iv != null) IV = iv;
            if (string.IsNullOrWhiteSpace(IV))
            {
                if (!generateIfEmpty) throw new CryptographicException("AES IV is required.");

                byte[] newIV = new byte[16];
                using (RandomNumberGenerator rng = RandomNumberGenerator.Create())
                {
                    rng.GetBytes(newIV);
                }
                IV = Convert.ToBase64String(newIV);
                return newIV;
            }

            byte[] ivBytes = TryParseBase64IV(IV) ?? CryptoEncoding.GetBytes(IV);
            if (ivBytes.Length != 16)
                throw new CryptographicException("AES IV must be 16 bytes or a Base64 encoded 16-byte value.");

            return ivBytes;
        }

        private byte[] TryParseBase64IV(string iv)
        {
            try
            {
                byte[] ivBytes = Convert.FromBase64String(iv);
                return ivBytes.Length == 16 ? ivBytes : null;
            }
            catch (FormatException)
            {
                return null;
            }
        }

        private byte[] DeriveKey(string purpose, int length)
        {
            if (string.IsNullOrEmpty(Const.Key))
                throw new CryptographicException("Const.Key can not be null or empty.");

            byte[] keyBytes = CryptoEncoding.GetBytes(Const.Key);
            using (HMACSHA256 hmac = new HMACSHA256(keyBytes))
            {
                byte[] derived = hmac.ComputeHash(Encoding.ASCII.GetBytes(purpose));
                if (length > derived.Length)
                    throw new ArgumentOutOfRangeException("length", "Requested key length is too long.");

                byte[] result = new byte[length];
                Buffer.BlockCopy(derived, 0, result, 0, length);
                return result;
            }
        }

        private byte[] ComputeHMAC(byte[] iv, byte[] encrypted)
        {
            byte[] key = DeriveKey("ZapLib.Crypto.AES.AuthenticationKey.v1", 32);
            byte[] version = Encoding.ASCII.GetBytes("ZapLib.Crypto.AES-CBC-HMACSHA256.v1");
            using (HMACSHA256 hmac = new HMACSHA256(key))
            {
                return hmac.ComputeHash(Concat(version, iv, encrypted));
            }
        }

        private static byte[] Concat(params byte[][] chunks)
        {
            int len = chunks.Sum(chunk => chunk.Length);
            byte[] result = new byte[len];
            int offset = 0;
            foreach (byte[] chunk in chunks)
            {
                Buffer.BlockCopy(chunk, 0, result, offset, chunk.Length);
                offset += chunk.Length;
            }
            return result;
        }

        private static bool FixedTimeEquals(byte[] left, byte[] right)
        {
            if (left == null || right == null || left.Length != right.Length) return false;
            int diff = 0;
            for (int i = 0; i < left.Length; i++) diff |= left[i] ^ right[i];
            return diff == 0;
        }
    }
}
