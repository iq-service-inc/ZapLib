using Microsoft.VisualStudio.TestTools.UnitTesting;
using ZapLib.Security;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace ZapLib.Security.Tests
{
    [TestClass()]
    public class ValidPlatformAttributeTests
    {
        [TestMethod()]
        public void IsVaildTest()
        {
            string content = JsonConvert.SerializeObject(new { name="Zap"});
            Crypto crypto = new Crypto();
            string OuterSignature = crypto.Md5(content);
            string Authorization = crypto.AESEncryption(OuterSignature);
            string IV = crypto.IV;


            ValidPlatformAttribute attr = new ValidPlatformAttribute();

            // 請求內容完全合法且沒遭竄改
            Assert.IsTrue(attr.IsVaild(content, IV, Authorization, OuterSignature));

            // 請求內容遭竄改
            string fake_content = JsonConvert.SerializeObject(new { name = "Jack" });
            Assert.IsFalse(attr.IsVaild(fake_content, IV, Authorization, OuterSignature));

            // 請求內容遭到竄改但是使用上帝金鑰
            string original_god_key = Const.GodKey;
            string god_key = "GoD!";
            Const.GodKey = god_key;
            Assert.IsTrue(attr.IsVaild(fake_content, IV, god_key, OuterSignature));
            Const.GodKey = original_god_key;
        }
    }
}
