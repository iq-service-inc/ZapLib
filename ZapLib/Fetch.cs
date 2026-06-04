using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using ZapLib.Security;
using ZapLib.Utility;

namespace ZapLib
{
    /// <summary>
    /// HTTP 網路請求與接收回應工具
    /// </summary>
    public class Fetch : IDisposable
    {
        /// <summary>
        /// HTTP 連線主要物件
        /// </summary>
        public HttpClient Client { get; set; }

        /// <summary>
        /// HTTP 連線資訊控制器
        /// </summary>
        public HttpClientHandler ClientHandler { get; set; }

        /// <summary>
        /// HTTP 請求主要物件
        /// </summary>
        public HttpRequestMessage Request { get; set; }

        /// <summary>
        /// HTTP 回應主要物件
        /// </summary>
        public HttpResponseMessage Response { private set; get; }


        /// <summary> 
        /// 網址 URL
        /// </summary>
        public string Url
        {
            get => Client.BaseAddress?.ToString();
            set
            {
                try
                {
                    Client.BaseAddress = new Uri(value);
                }
                catch (Exception e)
                {
                    log.Write("URL is invalid : " + (value ?? "null"));
                    log.Write(e.ToString());
                }
            }
        }

        /// <summary>
        /// 查詢字串 HTTP Query String 
        /// </summary>
        public object Qs
        {
            get => Client.BaseAddress.Query;
            set
            {
                if (value != null)
                {
                    Url = string.Format("{0}{1}{2}{3}?{4}",
                   Client.BaseAddress?.Scheme,
                   Uri.SchemeDelimiter,
                   Client.BaseAddress?.Authority,
                   Client.BaseAddress?.AbsolutePath,
                   QueryString.Parse(value));
                }
            }
        }

        /// <summary>
        /// HTTP 標頭 Header
        /// </summary>
        public object Header
        {
            set
            {
                Mirror.EachMembers(value, (string key, string val) =>
                {
                    if (string.IsNullOrWhiteSpace(key) || string.IsNullOrWhiteSpace(val)) return;
                    switch (key.ToLower())
                    {
                        case "content-type": ContentType = val; break;
                        default: Client.DefaultRequestHeaders.Add(key, val); break;
                    }
                });
            }
            get => Client.DefaultRequestHeaders;
        }

        /// <summary>
        /// HTTP Cookie
        /// </summary>
        public object Cookie
        {
            set
            {
                Mirror.EachMembers(value, (string key, string val) =>
                {
                    if (string.IsNullOrWhiteSpace(key) || string.IsNullOrWhiteSpace(val)) return;
                    CookieContainer.Add(new Cookie(key, val) { Domain = Client.BaseAddress.Host });
                });
            }
            get => Client.BaseAddress == null ? null : CookieContainer.GetCookies(Client.BaseAddress);
        }

        /// <summary>
        /// 能夠接受的回應內容類型
        /// </summary>
        public string Accept
        {
            get => Client.DefaultRequestHeaders.Accept?.ToString();
            set => Client.DefaultRequestHeaders.Accept.TryParseAdd(value);
        }

        /// <summary>
        /// 請求體的多媒體類型，在非 GET 請求時，它將影響資料邊碼格式
        /// </summary>
        public string ContentType { get; set; }

        /// <summary>
        /// 是否加入平台驗證 Header 資料
        /// </summary>
        public bool ValidPlatform { get; set; }

        /// <summary>
        /// HTTP 回應狀態碼，在送出請求之前一律為 0
        /// </summary>
        public int StatusCode
        {
            get => Response?.StatusCode == null ? 0 : (int)Response.StatusCode;
        }


        /// <summary>
        /// 網路代理，預設為 NULL
        /// </summary>
        public string Proxy
        {
            set
            {
                WebProxy.Address = value == null ? null : new Uri(value);
                WebProxy.BypassProxyOnLocal = value != null;
            }
            get => WebProxy?.Address?.ToString();
        }

        /// <summary>
        /// 發送請求的資料的編碼格式，預設為 UTF8
        /// </summary>
        public Encoding RequestEncoding { get; set; } = Encoding.UTF8;


        /// <summary>
        /// 方法 GetBinary() 的資源大小上限，預設 25MB
        /// </summary>
        public int MaxDownloadSize { get; set; } = 25 * 1024;

        private CookieContainer CookieContainer;
        private WebProxy WebProxy;
        private MyLog log;

        /// <summary>
        /// 建構子，初始化必要物件
        /// </summary>
        /// <param name="uri">連線的 URL</param>
        public Fetch(string uri = null)
        {
            CookieContainer = new CookieContainer();
            WebProxy = new WebProxy();
            ClientHandler = new HttpClientHandler();
            ClientHandler.CookieContainer = CookieContainer;
            Client = new HttpClient(ClientHandler);
            Request = new HttpRequestMessage();
            log = new MyLog();
            log.SilentMode = Config.Get("SilentMode");
            Url = uri;
        }

        /// <summary>
        /// 發送 HTTP GET 請求，取得回傳資料後以字串回傳，請求失敗時 (200~299之外) 將回傳 NULL
        /// </summary>
        /// <param name="qs">查詢字串 Query String</param>
        /// <returns>HTTP 回應的內容</returns>
        public string Get(object qs = null)
        {
            Qs = qs;
            Request.Method = HttpMethod.Get;
            return Send() ? Response.Content.ReadAsStringAsync().Result : null;
        }

        /// <summary>
        /// 發送 HTTP GET 請求，Accept 將設為 application/json 預期取得 JSON 資料，並將資料反序列化綁定到資料模型 T 中，請求失敗時 (200~299之外) 將回傳 NULL
        /// </summary>
        /// <typeparam name="T">指定綁定的資料型態 T</typeparam>
        /// <param name="qs">查詢字串</param>
        /// <returns>綁定資料的資料模型</returns>
        public T Get<T>(object qs = null)
        {
            Qs = qs;
            Request.Method = HttpMethod.Get;
            Accept = "application/json";
            return Send() ? JsonConvert.DeserializeObject<T>(Response.Content.ReadAsStringAsync().Result) : default;
        }

        /// <summary>
        /// 發送 HTTP GET，直接以 byte 方式將資料讀入後回傳，請求失敗時 (200~299之外) 將回傳 NULL
        /// </summary>
        /// <param name="qs">查詢字串</param>
        /// <returns>HTTP 回應的內容</returns>
        public byte[] GetBinary(object qs = null)
        {
            Qs = qs;
            Request.Method = HttpMethod.Get;
            if (Send())
                try
                {
                    using (MemoryStream ms = new MemoryStream())
                    {
                        using (var stream = Response.Content.ReadAsStreamAsync().Result)
                        {
                            byte[] buffer = new byte[MaxDownloadSize];
                            int read;
                            while ((read = stream.Read(buffer, 0, buffer.Length)) > 0) ms.Write(buffer, 0, read);
                            return ms.ToArray();
                        }
                    }
                }
                catch { }
            return null;
        }

        /// <summary>
        /// 發送 HTTP POST 請求，取得回傳資料後以字串回傳，請求失敗時 (200~299之外) 將回傳 NULL
        ///  (如果有 file 傳送檔案，ContentType 將設為 multipart/form-data)
        /// </summary>
        /// <param name="data">資料</param>
        /// <param name="qs">查詢字串</param>
        /// <param name="files">檔案</param>
        /// <returns>HTTP 回應的內容</returns>
        public string Post(object data = null, object qs = null, object files = null)
        {
            Qs = qs;
            if (files != null) ContentType = "multipart/form-data";
            Request.Method = HttpMethod.Post;
            SetRequestContent(data, files);
            return Send() ? Response.Content.ReadAsStringAsync().Result : default;
        }

        /// <summary>
        /// 發送 HTTP POST 請求，Accept 將設為 application/json 預期取得 JSON 資料，並將資料反序列化綁定到資料模型 T 中，請求失敗時 (200~299之外) 將回傳 NULL 
        ///  (如果有 file 傳送檔案，ContentType 將設為 multipart/form-data)
        /// </summary>
        /// <typeparam name="T">指定綁定的資料型態</typeparam>
        /// <param name="data">資料</param>
        /// <param name="qs">查詢字串</param>
        /// <param name="files">檔案</param>
        /// <returns>綁定資料的資料模型</returns>
        public T Post<T>(object data = null, object qs = null, object files = null)
        {
            Qs = qs;
            Accept = "application/json";
            Request.Method = HttpMethod.Post;
            SetRequestContent(data, files);
            return Send() ? JsonConvert.DeserializeObject<T>(Response.Content.ReadAsStringAsync().Result) : default;
        }

        /// <summary>
        /// 發送 HTTP DELETE 請求，取得回傳資料後以字串回傳，請求失敗時 (200~299之外) 將回傳 NULL
        /// </summary>
        /// <param name="data">資料</param>
        /// <param name="qs">查詢字串</param>
        /// <param name="files">檔案</param>
        /// <returns>HTTP 回應的內容</returns>
        public string Delete(object data = null, object qs = null, object files = null)
        {
            Qs = qs;
            Request.Method = HttpMethod.Delete;
            SetRequestContent(data, files);
            return Send() ? Response.Content.ReadAsStringAsync().Result : default;
        }

        /// <summary>
        /// 發送 HTTP DELETE 請求，Accept 將設為 application/json 預期取得 JSON 資料，並將資料反序列化綁定到資料模型 T 中，請求失敗時 (200~299之外) 將回傳 NULL 
        ///  (如果有 file 傳送檔案，ContentType 將設為 multipart/form-data)
        /// </summary>
        /// <typeparam name="T">指定綁定的資料型態</typeparam>
        /// <param name="data">資料</param>
        /// <param name="qs">查詢字串</param>
        /// <param name="files">檔案</param>
        /// <returns>綁定資料的資料模型</returns>
        public T Delete<T>(object data = null, object qs = null, object files = null)
        {
            Qs = qs;
            Accept = "application/json";
            Request.Method = HttpMethod.Delete;
            SetRequestContent(data, files);
            return Send() ? JsonConvert.DeserializeObject<T>(Response.Content.ReadAsStringAsync().Result) : default;
        }

        /// <summary>
        /// 發送 HTTP PUT 請求，取得回傳資料後以字串回傳，請求失敗時 (200~299之外) 將回傳 NULL
        /// </summary>
        /// <param name="data">資料</param>
        /// <param name="qs">查詢字串</param>
        /// <param name="files">檔案</param>
        /// <returns>HTTP 回應的內容</returns>
        public string Put(object data = null, object qs = null, object files = null)
        {
            Qs = qs;
            Request.Method = HttpMethod.Put;
            SetRequestContent(data, files);
            return Send() ? Response.Content.ReadAsStringAsync().Result : null;
        }

        /// <summary>
        /// 發送 HTTP PUT 請求，Accept 將設為 application/json 預期取得 JSON 資料，並將資料反序列化綁定到資料模型 T 中，請求失敗時 (200~299之外) 將回傳 NULL 
        ///  (如果有 file 傳送檔案，ContentType 將設為 multipart/form-data)
        /// </summary>
        /// <typeparam name="T">指定綁定的資料型態</typeparam>
        /// <param name="data">資料</param>
        /// <param name="qs">查詢字串</param>
        /// <param name="files">檔案</param>
        /// <returns>綁定資料的資料模型</returns>
        public T Put<T>(object data = null, object qs = null, object files = null)
        {
            Qs = qs;
            Accept = "application/json";
            Request.Method = HttpMethod.Put;
            SetRequestContent(data, files);
            return Send() ? JsonConvert.DeserializeObject<T>(Response.Content.ReadAsStringAsync().Result) : default;
        }

        /// <summary>
        /// 發送 HTTP Patch 請求，取得回傳資料後以字串回傳，請求失敗時 (200~299之外) 將回傳 NULL
        ///  (如果有 file 傳送檔案，ContentType 將設為 multipart/form-data)
        /// </summary>
        /// <param name="data">資料</param>
        /// <param name="qs">查詢字串</param>
        /// <param name="files">檔案</param>
        /// <returns>HTTP 回應的內容</returns>
        public string Patch(object data = null, object qs = null, object files = null)
        {
            Qs = qs;
            if (files != null) ContentType = "multipart/form-data";
            Request.Method = new HttpMethod("PATCH");
            SetRequestContent(data, files);
            return Send() ? Response.Content.ReadAsStringAsync().Result : default;
        }

        /// <summary>
        /// 發送 HTTP Patch 請求，Accept 將設為 application/json 預期取得 JSON 資料，並將資料反序列化綁定到資料模型 T 中，請求失敗時 (200~299之外) 將回傳 NULL 
        ///  (如果有 file 傳送檔案，ContentType 將設為 multipart/form-data)
        /// </summary>
        /// <typeparam name="T">指定綁定的資料型態</typeparam>
        /// <param name="data">資料</param>
        /// <param name="qs">查詢字串</param>
        /// <param name="files">檔案</param>
        /// <returns>綁定資料的資料模型</returns>
        public T Patch<T>(object data = null, object qs = null, object files = null)
        {
            Qs = qs;
            Accept = "application/json";
            Request.Method = new HttpMethod("PATCH");
            SetRequestContent(data, files);
            return Send() ? JsonConvert.DeserializeObject<T>(Response.Content.ReadAsStringAsync().Result) : default;
        }

        /// <summary>
        /// 以純文字方式取得發送請求後的回應結果，尚未發送請求前預設為 NULL
        /// </summary>
        /// <returns>HTTP 回應的資料</returns>
        public string GetResponse() => Response?.Content?.ReadAsStringAsync().Result;

        /// <summary>
        /// 以 byte[] 方式取得發送請求後的回應結果，尚未發送請求前預設為 NULL
        /// </summary>
        /// <returns>HTTP 回應的資料</returns>
        public byte[] GetBinaryResponse() => Response.Content?.ReadAsByteArrayAsync().Result;

        /// <summary>
        /// 取得發送請求後的所有 HTTP Header 資料
        /// </summary>
        /// <returns>HTTP 所有回應的標頭</returns>
        public HttpResponseHeaders GetResponseHeaders() => Response?.Headers;

        /// <summary>
        /// 取得發送請求後指定 key 名稱的 HTTP Header 資料，如果找不到該資料將回傳 NULL
        /// </summary>
        /// <param name="key">Header 名稱</param>
        /// <returns>HTTP 回應的特定標頭數值</returns>
        public string GetResponseHeader(string key)
        {
            if (Response != null)
                if (Response.Headers.TryGetValues(key, out IEnumerable<string> val))
                    return val.First();
            return null;
        }

        /// <summary>
        /// 取得發送請求後指定 key 名稱的 HTTP Header 資料，並自動轉型成指定型態，如果找不到該資料將回傳第二個參數 default
        /// </summary>
        /// <param name="key">Header 名稱</param>
        /// <param name="defaultVal">預設的數值</param>
        /// <returns>HTTP 回應的特定標頭數值</returns>
        public T GetResponseHeader<T>(string key, T defaultVal = default)
        {
            try
            {
                return (T)Convert.ChangeType(GetResponseHeader(key), typeof(T));
            }
            catch
            {
                return defaultVal;
            }
        }

        /// <summary>
        /// 將資料邊碼成 Multipart Form
        /// </summary>
        /// <param name="data">資料</param>
        /// <param name="files">檔案</param>
        /// <returns>請求的內容物件</returns>
        public MultipartFormDataContent BuildMultipartFormDataContent(object data = null, object files = null)
        {
            if (data == null && files == null) return null;
            var form = new MultipartFormDataContent();
            if (data != null)
                Mirror.EachMembers(data, (string key, string val) =>
                {
                    if (string.IsNullOrWhiteSpace(key) || string.IsNullOrWhiteSpace(val)) return;
                    form.Add(new StringContent(val), key);
                });
            if (files != null)
                Mirror.EachMembers(files, (string key, string val) =>
                {
                    if (string.IsNullOrWhiteSpace(key) || string.IsNullOrWhiteSpace(val)) return;
                    //val = val.Trim();
                    var exists = File.Exists(val);
                    if (!exists) return;
                    form.Add(new StreamContent(File.OpenRead(val)), key, Path.GetFileName(val));
                });
            return form;
        }

        /// <summary>
        /// 將資料邊碼成 Url Encoded Form
        /// </summary>
        /// <param name="data">資料</param>
        /// <returns>請求的內容物件</returns>
        public FormUrlEncodedContent BuildFormUrlEncodedContent(object data)
        {
            if (data == null) return null;
            List<KeyValuePair<string, string>> kvCollection = new List<KeyValuePair<string, string>>();
            Mirror.EachMembers(data, (string key, string val) =>
            {
                if (string.IsNullOrWhiteSpace(key) || string.IsNullOrWhiteSpace(val)) return;
                kvCollection.Add(new KeyValuePair<string, string>(key, val));
            });
            return new FormUrlEncodedContent(kvCollection);
        }

        /// <summary>
        /// 將資料邊碼成純文字格式
        /// </summary>
        /// <param name="data">資料</param>
        /// <returns>請求的內容物件</returns>
        public StringContent BuildStringContent(string data = null) =>
            data == null ?
            null :
            new StringContent(data, RequestEncoding, ContentType);

        /// <summary>
        /// [已棄用] 設定 Request.Content 的資料。請改用 <see cref="SetRequestContent"/>
        /// </summary>
        /// <param name="data">資料</param>
        /// <param name="files">檔案</param>
        [Obsolete("方法已重新命名為 SetRequestContent，原拼字錯誤的版本將於下一個 major 版本移除")]
        public void SetRequestContnet(object data = null, object files = null) => SetRequestContent(data, files);

        /// <summary>
        /// 設定 Request.Content 的資料
        /// </summary>
        /// <param name="data">資料</param>
        /// <param name="files">檔案</param>
        public void SetRequestContent(object data = null, object files = null)
        {
            if (string.IsNullOrWhiteSpace(ContentType)) ContentType = "application/json";
            if (files != null) ContentType = "multipart/form-data";

            switch (ContentType)
            {
                case "application/x-www-form-urlencoded":
                    Request.Content = BuildFormUrlEncodedContent(data);
                    break;
                case "multipart/form-data":
                    Request.Content = BuildMultipartFormDataContent(data, files);
                    break;
                case "text/json":
                case "application/json":
                    Request.Content = BuildStringContent(JsonConvert.SerializeObject(data));
                    break;
                default:
                    Request.Content = BuildStringContent(Cast.To<string>(data));
                    break;
            }

        }

        private void ProcValidPlatform()
        {
            string content = Request.Content == null ? "" : Request.Content.ReadAsStringAsync().Result;
            Crypto crypto = new Crypto();
            string Signature = crypto.Md5(content);
            string Authorization = crypto.AESEncryption(Signature);
            string IV = crypto.IV;
            Header = new Dictionary<string, string>()
            {
                { "Channel-Signature", Signature},
                { "Channel-Authorization", Authorization},
                { "Channel-Iv", IV},
            };
        }

        /// <summary>
        /// 手動送出請求 (注意：每個 Fetch 物件只能發送一次請求)
        /// </summary>
        /// <returns></returns>
        public bool Send()
        {
            if (StatusCode != 0) throw new Exception("Every Fetch instance only send request once!");
            if (ValidPlatform) ProcValidPlatform();
            if (!string.IsNullOrWhiteSpace(Proxy)) ClientHandler.Proxy = WebProxy;

            if(Config.Get("TLS12")?.ToLower()=="true")
                ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;

            try
            {
                Response = Client.SendAsync(Request).Result;
                if (!Response.IsSuccessStatusCode)
                {
                    log.Write("Fail WebRequest URL: " + Url);
                    log.Write($"Fail WebRequest Data: {GetResponse()}");
                }
                return Response.IsSuccessStatusCode;
            }
            catch (Exception e)
            {
                log.Write("Fail WebRequest URL: " + Url);
                log.Write($"Fail WebRequest Data: {e.ToString()}");
            }
            return false;
        }

        /// <summary>
        /// 釋放 Fetch 所有使用的物件
        /// </summary>
        public void Dispose()
        {
            if (Response != null) Response.Dispose();
            if (Request != null) Request.Dispose();
            if (Client != null) Client.Dispose();
        }
    }
}
