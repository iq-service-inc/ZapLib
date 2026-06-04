using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;

namespace ZapLib.Security.Tests
{
    [TestClass()]
    public class CryptoTests
    {
        [TestMethod()]
        public void AESEncryptionTest()
        {
            Crypto c = new Crypto(Encoding.UTF8);
            string exp = "你好我是大衛";
            string s = c.AESEncryption(exp);
            Trace.WriteLine(s);
            string ds = c.AESDecryption(s, c.IV);
            Trace.WriteLine(ds);
            Assert.AreEqual(exp, ds);
        }

        [TestMethod()]
        public void AESEncryptionUsesRandomIVTest()
        {
            Crypto c1 = new Crypto(Encoding.UTF8);
            Crypto c2 = new Crypto(Encoding.UTF8);

            string s1 = c1.AESEncryption("same message");
            string s2 = c2.AESEncryption("same message");

            Assert.AreNotEqual(c1.IV, c2.IV);
            Assert.AreNotEqual(s1, s2);
            Assert.AreEqual("same message", c1.AESDecryption(s1, c1.IV));
            Assert.AreEqual("same message", c2.AESDecryption(s2, c2.IV));
        }

        [TestMethod()]
        public void AESDecryptionRejectsTamperedPayloadTest()
        {
            Crypto c = new Crypto(Encoding.UTF8);
            string s = c.AESEncryption("safe message");
            byte[] payload = Convert.FromBase64String(s);
            payload[0] = (byte)(payload[0] ^ 1);
            string tampered = Convert.ToBase64String(payload);

            Assert.ThrowsException<CryptographicException>(() => c.AESDecryption(tampered, c.IV));
        }

        [TestMethod()]
        public void Md5Test()
        {
            Crypto c = new Crypto(Encoding.UTF8);
            string exp = "Tom";
            string actual = c.Md5(exp);
            string expected = _Md5(exp);

            Trace.WriteLine(actual);
            Trace.WriteLine(expected);

            Assert.AreEqual(expected, actual);

        }

        [TestMethod()]
        public void Md5Test2()
        {
            Crypto c = new Crypto(Encoding.UTF8);
            string exp = "English is a West Germanic language in the Indo-European language family, whose speakers, called Anglophones, originated in early medieval EnglandEnglish is a West Germanic language in the Indo-European language family, whose speakers, called Anglophones, originated in early medieval EnglandEnglish is a West Germanic language in the Indo-European language family, whose speakers, called Anglophones, originated in early medieval England";
            string actual = c.Md5(exp);
            string expected = _Md5(exp);

            Trace.WriteLine(actual);
            Trace.WriteLine(expected);

            Assert.AreEqual(expected, actual);

        }

            public string _Md5(string content = "")
        {
            Encoding CryptoEncoding = Encoding.UTF8;
            MD5 md5 = MD5.Create();
            byte[] source = CryptoEncoding.GetBytes(content);
            byte[] crypto = md5.ComputeHash(source);
            return Convert.ToBase64String(crypto);
        }
    }
    
}
