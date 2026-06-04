using System;
using System.Linq;
using System.Text;
using System.Net.Http.Headers;
using System.Web.Http;
using System.Net.Http;
using System.Web.Http.Controllers;
using System.Net;
using Newtonsoft.Json;

namespace ZapLib.Security
{
    /// <summary>
    /// 平台驗證標籤，加上該標籤的 Action 都會進行平台驗證檢查，檢查失敗將回傳 401 Unauthorized，且不會進入 Action
    /// </summary>
    public class ValidPlatformAttribute : AuthorizeAttribute
    {
        /// <summary>
        /// Web API 進入 Action 會執行這個方法驗證
        /// </summary>
        /// <param name="actionContext">WebAPI HttpActionContext</param>
        public override void OnAuthorization(HttpActionContext actionContext)
        {
            HttpRequestMessage Request = actionContext.Request;
            var header = Request.Headers;
            string content = Request.Content == null ? "" : Request.Content.ReadAsStringAsync().Result;
            string OuterSignature = getHeader(header, "Channel-Signature");
            string IV = getHeader(header, "Channel-Iv");
            string Authorization = getHeader(header, "Channel-Authorization");

            if (IsVaild(content, IV, Authorization, OuterSignature)) return;

            HttpStatusCode httpcode = HttpStatusCode.Unauthorized;
            HttpResponseMessage Response = Request.CreateResponse(httpcode);
            Response.Content = new StringContent(
                JsonConvert.SerializeObject(new { error = "Request is not valid, please check [ValidPlatform]" }), Encoding.UTF8, "application/json");
            actionContext.Response = Response;
        }

        private string getHeader(HttpRequestHeaders header, string key)
        {
            try
            {
                return header.GetValues(key).FirstOrDefault();
            }
            catch (Exception)
            {
                return null;
            }
        }

        /// <summary>
        /// 內對內 API 驗證平台是否可被信任
        /// </summary>
        /// <param name="content">HTTP Request.Content 字串內容</param>
        /// <param name="IV">AES 初始化向量</param>
        /// <param name="Authorization">以 Signature + Iv + 系統金鑰 使用 AES 加密後的結果</param>
        /// <param name="OuterSignature">簽章，將傳送的資料以 MD5 加密後的字串</param>
        /// <returns>這個請求是否可被信任</returns>
        public bool IsVaild(string content, string IV, string Authorization, string OuterSignature)
        {
            Crypto crypto = new Crypto();
            string InnerSignature = crypto.Md5(content);

            if (!string.IsNullOrWhiteSpace(Authorization))
            {
                if (SecureEquals(Authorization, Const.GodKey)) return true;
                try
                {
                    string ExpectedAuthorization = crypto.AESEncryption(OuterSignature, IV);
                    if (SecureEquals(ExpectedAuthorization, Authorization) && SecureEquals(InnerSignature, OuterSignature)) return true;
                }
                catch (Exception)
                {
                    return false;
                }
            }
            return false;
        }

        private bool SecureEquals(string left, string right)
        {
            if (left == null || right == null) return false;
            byte[] leftBytes = Encoding.UTF8.GetBytes(left);
            byte[] rightBytes = Encoding.UTF8.GetBytes(right);
            if (leftBytes.Length != rightBytes.Length) return false;

            int diff = 0;
            for (int i = 0; i < leftBytes.Length; i++) diff |= leftBytes[i] ^ rightBytes[i];
            return diff == 0;
        }
    }
}
