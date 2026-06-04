# ChangeLog

改版紀錄

## `2.6.0`

1. **移除 DES 可逆加解密 API**：`Crypto.DESEncryption()` / `Crypto.DESDecryption()` 已移除，改由 `Crypto.AESEncryption()` / `Crypto.AESDecryption()` 取代。
2. **新增 AES-CBC + HMAC-SHA256 可逆加解密**：`AESEncryption()` 會自動產生 Base64 16-byte IV 並寫入 `Crypto.IV`，回傳的密文為 Base64，且內含 HMAC 驗證碼；`AESDecryption()` 會先驗證密文完整性，驗證失敗會丟出 `CryptographicException`。
3. **更新 `[ValidPlatform]` 與 `Fetch.ValidPlatform`**：平台驗證的 `Channel-Authorization` 由 DES 改為 AES-CBC + HMAC-SHA256 產生。
4. **新增 `SQL` NoWait 執行方法**：新增 `QuickExecuteNoWait()` 與 `QuickExecNoWait()`，可在背景以獨立連線執行 SQL Text / Stored Procedure，呼叫端立即返回且不等待結果。
5. **調整 `MyLog.ForceLog` fallback 策略**：原始 log 寫入失敗時，改為集中 append 到 `{fileName}.force{ext}`，不再遞迴產生 GUID suffix 檔案；fallback 寫入加上 lock 保護。

## `2.5.0`

🎉 **v2.5.x 系列正式版**。彙整 `2.5.0-beta1` 與 `2.5.0-beta2` 全部內容，並新增以下：

1. **方法命名修正**：修正三個 public method 的英文拼字錯誤。原拼字錯誤的版本仍以 `[Obsolete]` 別名保留，**現有程式碼不需修改即可升級**，編譯時會看到 warning 提示遷移。

    | 舊名稱 | 新名稱（推薦） |
    | --- | --- |
    | `SQL.Connet()` | `SQL.Connect()` |
    | `SQL.BuildconnString()` | `SQL.BuildConnectionString()` |
    | `OracleSQL.Connet()` | `OracleSQL.Connect()` |
    | `Fetch.SetRequestContnet()` | `Fetch.SetRequestContent()` |

    > ⚠️ 下一個 major 版本（v3.0）將完全移除舊名稱。請盡快遷移。

2. **全新文件站** [https://linzap.github.io/ZapLib/](https://linzap.github.io/ZapLib/) 上線，由 DocFX 自動產生，包含完整教學文章 + API Reference。

3. 本版本綜合 `2.5.0-beta1` 新增的 `OracleSQL` 類別，與 `2.5.0-beta2` 的多個 CVE 安全更新。詳見下方各 beta 版說明。

## `2.5.0-beta2`

安全性更新 (Security fixes) — 升級多個有 CVE 公告的相依套件，無公開 API 變動，使用者可直接更新。

升級內容：

| 套件 | 原版本 | 新版本 | CVE / Advisory |
| --- | --- | --- | --- |
| `Newtonsoft.Json` | 12.0.1 | 13.0.3 | [GHSA-5crp-9r3c-p9vr](https://github.com/advisories/GHSA-5crp-9r3c-p9vr) (high) |
| `Microsoft.Owin` (+ `.Security`, `.Host.SystemWeb`) | 4.2.0 | 4.2.3 | [GHSA-3rq8-h3gj-r5c6](https://github.com/advisories/GHSA-3rq8-h3gj-r5c6) (high) |
| `MailKit` | 3.3.0 | 4.16.0 | [GHSA-9j88-vvj5-vhgr](https://github.com/advisories/GHSA-9j88-vvj5-vhgr) (moderate) |
| `MimeKit` | 3.3.0 | 4.16.0 | [GHSA-gmc6-fwg3-75m5](https://github.com/advisories/GHSA-gmc6-fwg3-75m5) (high), [GHSA-g7hc-96xr-gvvx](https://github.com/advisories/GHSA-g7hc-96xr-gvvx) (moderate) |

備註：

* MailKit/MimeKit 由 3.x 升級到 4.x，原本相依的 `Portable.BouncyCastle 1.9.0` 改為 `BouncyCastle.Cryptography 2.6.2`，並一併拉新 `System.Buffers` / `System.Memory` / `System.Numerics.Vectors` / `System.Runtime.CompilerServices.Unsafe` / `System.Threading.Tasks.Extensions`，並新增 `System.Formats.Asn1`。
* `Mailer` class 公開介面 (`Send`, `AddAttachments`, `SecureSocketOption` 等) 維持不變，使用方式完全相同。
* 仍維持 .NET Framework 4.7.2 目標，未變更 TFM。

## `2.5.0-beta1`

1. 新增全新類別 `OracleSQL`，用於連線 Oracle 資料庫，使用方式比照既有的 `SQL` class 設計，主要差異如下：
    * 底層採用 [Oracle.ManagedDataAccess](https://www.nuget.org/packages/Oracle.ManagedDataAccess) 純託管套件，部署機器**不需要安裝 Oracle Client**。
    * 建構子僅提供一種 `public OracleSQL(string connectionString)`，傳入的參數可以是 `.config` 中 `connectionStrings` 的名稱，也可以直接傳入連線字串。
    * SQL 語法中的參數寫法維持與 `SQL` class 相同，使用 `@name`，內部會自動轉為 Oracle 原生的 `:name` 並啟用 `BindByName` 按名稱綁定。
    * 本版本為精簡版，僅先行支援 `Query` / `QuickQuery<T>` / `QuickDynamicQuery` 三個常用查詢方法，其餘功能 (BulkCopy、Stored Procedure、Transaction 等) 後續版本再行擴充。


**`App.config` 或 `Web.config` 設定範例**

可於 `connectionStrings` 中加入 Oracle 連線字串，注意 `providerName` 需指定為 `Oracle.ManagedDataAccess.Client`：

```xml
<connectionStrings>
  <add name="OracleConn"
       connectionString="User Id=zaplib;Password=zaplib123;Data Source=10.190.173.129:1521/FREEPDB1"
       providerName="Oracle.ManagedDataAccess.Client" />
</connectionStrings>
```

**使用範例**

直接比照 `SQL` class 的寫法，透過 config 的連線名稱建立物件並查詢：

```csharp
// 以 connectionStrings 中的名稱連線
OracleSQL db = new OracleSQL("OracleConn");

// 也可以直接傳入完整連線字串
// OracleSQL db = new OracleSQL("User Id=zaplib;Password=zaplib123;Data Source=10.190.173.129:1521/FREEPDB1");

// 動態查詢，返回 dynamic[]
dynamic[] rows = db.QuickDynamicQuery("SELECT * FROM HR_VIEW");

// 泛型查詢，自動對應到 POCO 的屬性 (欄位比對不分大小寫)
HR_Row[] users = db.QuickQuery<HR_Row>(
    "SELECT * FROM HR_VIEW WHERE EMPN = @empn",
    new { empn = "625871" }
);

// 查詢失敗時可取得錯誤訊息
string err = db.GetErrorMessage();
```


## `2.4.13-beta3`

* 新增了 SQL 類別中 Timout 的機制，除非在程式中明確指定了 Timeout 數值，否則將會嘗試從 SQL Connection String 中抓取，如果都抓不到則會預設 30 秒


## `2.4.13-beta2`

* 新增了一個實驗機制，當 `SQL` class 啟用 `EnableDBAlwaysOn` 設定時， `QuickDynamicQuery` 與 `QuickQuery` 兩個方法會偵測查詢結果的資料筆數，如果為 0 筆時，此時若為 `SQLReadOnly=true` 模式，則會自動重新建立一次 SQL 連線，並且改用 `SQLReadOnly=false` 重新執行一次查詢。此功能是為了避免讀寫分離架構啟用時，寫入的資料在 primary db 尚未同步到 secondary db 上的錯誤。由於此方式過於粗糙因此目前僅為歸納為實驗性質，後續仍待評估更好的做法。  

## `2.4.12`

> 包含了 `2.4.9` ~ `2.4.12`

1. 新增了 `SQL` class 支援讀寫分離的 DB 連線參數設定
  * 新增成員 **`EnableDBAlwaysOn`** : 是否啟用 DB Always On 才有的 `ApplicationIntent=ReadOnly` 設定，預設為 `false`。也可於 Config 檔案中指定 `EnableDBAlwaysOn` 為 `True` 。
  * 新增成員 **`SQLReadOnly`** : 必須設置 `EnableDBAlwaysOn` 設置為 `true` 時，此設定才會生效 (否則為空操作)。 如果此值為 `true`，且 `EnableDBAlwaysOn` 也為 `true` 時，會自動將資料庫連線字串中的 `ApplicationIntent` 設置為 `ReadOny`。此數值預設為 `false`。
  * 請注意 Config 檔案中 `EnableDBAlwaysOn` 指定為 `False` 或不指定時， 連線字串將強制設置 `ApplicationIntent=ReadWrite`。
  * 詳細使用方式請參閱：[使用範例](https://gist.github.com/LinZap/a0a491ed24aa8e5c862376136d457c07)

## `2.4.8`

1. 新增了 `Config` class 中的 2 個新功能
  * **`Config.Delete(key)`**：刪除指定的 `appSettings` 中的參數
  * **`Config.DeleteConnectionString(key)`** 刪除指定的 `connectionStrings` 資料庫連線字串
  * 詳細使用方式請參閱：[使用範例](https://gist.github.com/LinZap/ece1a8c6ac04a49b6348dc071a946c3f)

## `2.4.7`

1. 新增了 `Config` class 中的 3 個新功能 (以下功能皆可在 Config 檔案被加密的狀況下正常運作)
  * **`Config.SetOrAdd(key, val)`**：設定既有的或新增全新的 `appSettings` 中的參數 (已存在則會修改，不存在則會新增)
  * **`Config.SetOrAddConnectionString(key, val, providerName)`**：設定既有的或新增全新的 `connectionStrings` 資料庫連線字串 (已存在則會修改，不存在則會新增)
  * **`Config.GetConnectionStrings()`**：取得所有資料庫連線字串設定，如果無法取得則回傳 `null`
  * 詳細使用方式請參閱：[使用範例](https://gist.github.com/LinZap/ece1a8c6ac04a49b6348dc071a946c3f)

## `2.4.6`

1. 將 `Crypto.Md5()` 方法中原先依賴使用 [`System.Security.Cryptography.MD5`](https://learn.microsoft.com/zh-tw/dotnet/api/system.security.cryptography.md5?view=net-8.0) 的部分移除，並改為自行實作 MD5 雜湊的方式提供


## `2.4.5`

1. 新增了 `SQL` class 中的一個新功能 `SQLDBReplace()`，提供了無須變更任何 SQL 語法即可自動變更 SQL 語法中 DB Name 部分格式的功能  [詳情說明](https://gist.github.com/LinZap/158b604d89acf62ba104d03c4c53029c)


## `2.4.4`

1. 新增了 `SQL` class 中的一個新方法 `QuickBulkCopy()`，允許一次性大量寫入資料，避免逐筆寫入的效能問題 [使用範例](https://gist.github.com/LinZap/db56534b0ac9893306e5cb99b3cb66ae)

## `2.4.3`

1. `MyLog` 支援自訂義 Log 檔案副檔名 (原本只能強制 `.txt`) [使用範例](https://gist.github.com/LinZap/9fd8579854283b10c51ddf3c18b6acf9)
2. `RegExp` 支援自訂正規表達式選項 (原本強制只能 `RegexOptions.IgnoreCase`) [使用範例](https://gist.github.com/LinZap/1e203e9b719580011c8c3ab95dceb394)
3. `SQL` 優化成會根據 ConnectionString 動態填補缺少的設定，已經指定於 ConnectionString 中的設定將不會被覆蓋 (例如 Connect Timeout) [使用範例](https://gist.github.com/LinZap/63ec2bbabca5a9972bb68d0a0583b665)
4. `Crypto` 支援可指定自訂義編碼 (原本只能 `Encoding.ASCII`) [使用範例](https://gist.github.com/LinZap/c7551251a0085a6b0e9c9f69e7826bca)
 

## `2.4.2`

1. 新增支援 `Mailer` 中支援 Password 參數如果給 `null` 則可跳過驗證
2. `ImplicitMailer` 已經支援

## `2.4.1`

1. 在 `ImplicitMailer` 與 `Mailer` 中新增 `AddAttachments()` 方法，可以新增郵件附件，並取得 `cid` 
2. 新增成員 `AttachmentsList` 可以取得使用 `AddAttachments()` 新增的附件列表

以下是附加檔案，並取得 Content ID 的範例：  
  
```csharp
Mailer m = new Mailer(SMTP_HOST, MAIL_ACT, MAIL_PWD, MAIL_PORT, MAIL_SSL);

string cid = m.AddAttachments(@"D:\Downloads\user.png");
string BODY = $"<p>Image here: <img src=\"cid:{cid}\" ></p>";

// 附件列表
var atts =  m.AttachmentsList;
```

## `2.4.0`

1. 新增 `ImplicitMailer` 與 `Mailer` 的 `Send()` 方法中新增 `attchments` 參數，允許傳入實體檔案路徑作為郵件附加檔案，一起寄出
2. 新增 Config `TLS12` 如果設為 `true` 則全系統使用 `Fetch` 功能時，會強制呼叫 TLS1.2 協議，呼叫方式如下：

```csharp
ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
```


## `2.3.1`

1. 修正因為版本 `2.2.5` 支援  `qs` 為 `null` 時導致 URL 中已經指定的 query string 被覆蓋成  `null` 的問題所產生的副作用錯誤

## `2.3.0`

1. 新增全新功能 `ImplicitMailer` 類別，底層採用 [Aegis Implicit Mail (AIM)](https://github.com/nilnull/AIM) 套件進行寄信功能，該套件支援隱式 (Implicit) SSL 加密連線，支援 port `465` 的 SMTP 連線加密協議。使用方式如下：


```csharp
string SMTP_HOST = "smtp.gmail.com";
string MAIL_ACT = "maryzap123@gmail.com";
string MAIL_PWD = "...";
            
int MAIL_PORT = 465;
bool MAIL_SSL = true; // 指定 false 則會採用 auto 自動適應 SSL or TLS

ImplicitMailer m = new ImplicitMailer(SMTP_HOST, MAIL_ACT, MAIL_PWD, MAIL_PORT, MAIL_SSL);

string TO = "zaplin@iqs-t.com,sollin@iqs-t.com";
string SUBJECT = $"This is a test mail (ZapLib.ImplicitMailer) - {DateTime.Now}";
string BODY = $"<h1>Test Mail by ZapLib ImplicitMailer</h1>";

bool result = m.Send(TO, SUBJECT, BODY); // 發送郵件

Trace.WriteLine(m.ErrMsg); // 印出寄信的細節結果
```


## `2.2.5`

1. 修改 `Fetch` 允許呼叫 `Get` `Post` 等方法時， 如果 `qs` query string 參數帶入 `null`，會將 URL 中已經指定的 query string 覆蓋的問題。現在指定 `null` 將可保留 URL 中設置的 query string


## `2.2.4`

1. 修改 Mailer 成員允許修改 SecureSocketOptions


## `2.2.3`

1. 修改 `SQL` Class 的 `Timeout` 參數，現在也會同步設定到 `CommandTimeout`


## `2.2.2`

1. 寄信功能 Mailer 的 Send 方法，支援一次傳送多個信箱(逗號隔開)

## `2.2.1`


1. 寄信功能 Mailer 的 Send 方法，新增 CC (副本) 與 BCC (密件副本)

## `2.2.0`

1. 寄信功能 Mailer 改為使用 [MailKit](https://github.com/jstedfast/MailKit) 支援 TLS1.2 更安全的加密協議 (介面與之前完全相同，可向下相容)

> 備註：[原先 SmtpClient 不支援 TLS 且被微軟棄用](https://developpaper.com/system-net-mail-smtpclient-failed-to-send-email-through-ssl-tls-protocol/)

## `2.1.2`


1. 可以設定 Config 參數 `MyLog`，來指定 Log 寫入的位置 (如果沒有設置則會採用 `Storage`)
2. 新增全新 Config 參數 `LogExecTime`，當設為 `true`時，Log 將會自動紀錄 SQL 的執行時間
3. 新增全新類別 `LogExecTime`，可以記錄程式的執行時間 (Config 參數 `LogExecTime` 設為 `true` 時才會生效)

```csharp
LogExecTime lxt = new LogExecTime("Unit Test");
Thread.Sleep(1500);
lxt.Log();
```

**Log**

```
[11:40:19] [Log Exec Time] Unit Test
Takes 1.514 second
```


## `2.1.1`

1. 修復 ExtApiHelper 中用到 Stream 物件的程式，資源不正常釋放的問題

## `2.1.0`

1. 將 ExtApiHelper 的 Response 與 Request 改為 public 存取
2. 新增 ZipHelper 快速 Zip 壓縮的類別函式庫 
3. 新增 MyLog.Read(page) 方法，能以翻頁的方式讀取 Log 檔案
4. 新增 Fetch.Patch 方法
5. 修改 MyLog 建構子，以利向下相容
6. 允許設置 ExtApiHelper 的 Encoding 修改後，所有 Response 都可以套用
7. 修改所有 ExtApiHelper 中用到 Stream 物件的程式，全部加上 using 釋放資源

#### 更新套件

* jQuery  1.6.4 → 3.6.0
* Microsoft.AspNet.SignalR 2.2.3 → 2.4.3
* Microsoft.AspNet.SignalR.Core 2.2.3 → 2.4.3
* Microsoft.AspNet.SignalR.JS 2.2.3 → 2.4.3
* Microsoft.AspNet.SignalR.SystemWeb 2.2.3 → 2.4.3
* Microsoft.AspNet.WebApi.Client 5.2.4 → 5.2.7
* Microsoft.AspNet.WebApi.Core 5.2.4 → 5.2.7
* Microsoft.AspNet.WebApi.WebHost 5.2.4 → 5.2.7
* Microsoft.Owin 2.1.0 → 4.2.0
* Microsoft.Owin.Host.SystemWeb 2.1.0 → 4.2.0
* Microsoft.Owin.Security 2.1.0 → 4.2.0
* Newtonsoft.Json 11.0.2 → 12.0.1

## `2.0.12`

1. 新增以 Stream 作為輸入參數的 API Response 函數 `GetStreamResponse`
2. SQL 支援類別的成員是 Nullable Enum 資料類型的 property  (資料會嘗試轉換到 Enum，如果失敗則會容錯成 Enum 的預設值，不會變成 null)
3. `Cast.To` 如果嘗試轉換成 Enum 類別，則會自動容錯成呼叫 Enum.To 函數
4. Config 新增 `Config.Refresh` 方法，如果應用程式運行過程中可能會改變 `.config` 檔案，可以自行決定何時刷新 Config 中的快取 (cache)
5. 新增 `JXPath.GetValue` Utility 類別方法，允許指定 XPath 來取出特定 `dynamic` 或 JSON 字串中指定路徑位置的數值
6. 新增  `Mirror.GetClasses` 可以掃描全系統 class 取得特定 class 或取得有繼承 class 的 classes
7. 新增全新功能 `ClassMirror` 可以輸入指定的 Type 並建立實體
8. 新增全新 `DllLoader` Utility 類別方法，允許指定 dll 檔案路徑並建立 assembly 組件

## `2.0.11`

1. 增加 標註棄用說明 `SQL.Exec` 已標註棄用，將在下一個版本刪除
2. 修正 `ExtApiHelper` 呼叫 `GetJsonBody<T>` 無法正常取得資料的問題
3. 修正 執行 `MyLog.write` 時，如果 `Storage` 目錄不存在會造成 stackoverflow 崩潰的問題，目錄不存在時，將完全不執行
4. 修正 使用 `Fetch` 傳入不合法的 URL 時會直接崩潰的問題，並且會自動 Log 提示
5. 優化 執行 `Mirror.Assign` 或 `Cast.To` 時，如果目標 Type 為 `Nullable<T>` 將會嘗試使用 `T` 底層類別 (under type) 重新嘗試轉換

## `2.0.10`

* 修正上傳檔案參數撰寫錯誤(#32)的 bug

## `2.0.9`


1. 修正 query string 中文變亂碼的問題
2. 增加 sql order by 白名單過濾
3. 修正 ValidPlatform 少做一次內容驗證問題
4. 增加 fetch 取得 header 自動轉型
5. 修正 Mirror.Member 可能會噴錯的問題
6. 新增 DynamicObject 動態實體物件
7. 修正 ExpParam 在遇到變數名稱相似的時候 ，會取代錯誤的問題
8. 修正 SQL getErrorMessage 有時候無法取得完整的 Exception 資訊的問題

## `2.0.8`

* Fixed Fetch Oject Disposed Exception
* Fixed Proxy setting.

## `2.0.7`

* Add transaction options in SQL
* Add get headers in ExtApiHelper

## `2.0.6`

1. `ZapLib/ExtApiHelper.cs` Add `GetHeader` and `GetHeader<T>` function
2. `ZapLib/Utility/Cast.cs` Add `ToEnum` and `ToEnum<T>` function

## `2.0.5`

* Add New Feature JsonReader

## `2.0.4`

1. implement interface  `ISQLParam` can custom function to run way of your process
2. add `SQLTypeAttribute ` defined SQL output model for mapping data type
3. implement interface  `ISQLTypeAttribute` can custom function to run way of your process
4. `ZapLib/Utility/Mirror.cs` Add `GetCustomAttributes<T>`


## `2.0.3`

* New Feature  SQL 參數擴展 ，允許使用  `ExpParam<T>` 以陣列當作資料，並自動生成 Prepare statement args


## `2.0.0 ~ 2.0.2`

* beta

## `v1.22.0`


### MyLog 添加新的機制

MyLog 從這個版本開始，**將會忽略因為 Log 檔案無法寫入的錯誤** (e.g. 多執行續共寫一個 Log 檔案)，並且允許全局關閉 ZapLib 內建的 Log 訊息。

**Force Log**

如果你仍需要在無法寫入 Log 檔案時，完整保留 Log 資訊，可以在 `App.config` 或是 `Web.config` 中添加 `ForceLog`，ZapLib 將自動把資訊寫在另一個檔案中，它看起來像是這樣 **`原本 Log 檔名-0f8fad5b-d9cb-469f-a165-70867728950e.txt`**，它會有助於你進行 debug 作業

```xml
<add key="ForceLog" value="True"/>
```
  
**Silent Mode**

如果你需要全局關閉 ZapLib 內建的 Log 訊息，可以在 `App.config` 或是 `Web.config` 中添加 `SilentMode`，一旦設置了這個選項，ZapLib 將關閉所有內建的 Log 訊息，但您還是可以呼叫 `MyLog` 功能進行紀錄，不會有所影響。

```xml
<add key="SilentMode" value="True"/>
```

**更新指南**

1. 如果你需要在 Log 檔無法寫入強破紀錄訊息，可以開啟 Force Log 的功能
2. 如果你需要關閉所有 ZapLib 的內建 log，可以開啟 Silent Mode 的功能
3. 如果你沒有以上需求，可以無需做任何修改，直接更新

## `v1.21.0`

### Fetch API 新增功能

**Form Data**

Fetch 預設會以 json 編碼傳送資料，如果需要傳統的 From 編碼格式，可以在 `contentType` 中指定

```csharp
Fetch f = new Fetch("https://httpbin.org/post");

// 預設
f.contentType = "application/json";

// 使用 URL 編碼傳送資料 x-www-form-urlencoded
f.contentType = "application/x-www-form-urlencoded";

// 使用傳統 Form 傳送資料
f.contentType = "multipart/form-data"
```
  
**Fetch 現在可以傳送 `Dictionary` 物件**


**示範：**

```csharp
Dictionary<string, string> data = new Dictionary<string, string>();
data.Add("test", "123");

Fetch f = new Fetch("https://httpbin.org/post");
f.contentType = "application/x-www-form-urlencoded";
f.post(data);
```
> 只能是 `Dictionary<string, string>` 不可為其他型態


### SQL API 新增功能

## `v1.20.0`

### SQL API 新增功能


**使用連線字串連線**

SQL API 可以使用自訂的 Connection String 進行連線  

```csharp
SQL db = new SQL("Data Source=(LocalDb)\MSSQLLocalDB;Initial ..."); // 使用自訂 Connection String 連線
```

**`getConnection()`** 

並且新增了 `getConnection()` 方法取代原先只能用 `connet()` 方法取得目前連線物件的方式  

```csharp
SQL db = new SQL(); 
db.connect();
SqlConnection myConn = db.getConnection();
```


**`quickDynamicQuery()` 與 `dynamicFetch()`**

新增了動態快速查詢 API，將返回 `dynamic[]` 型態的資料，查詢失敗一樣回傳 `null`  

```csharp
SQL db = new SQL();
dynamic[] data = db.quickDynamicQuery("select * from entity");

for(int i = 0; i < data.Length; i++)
{
    Trace.WriteLine((string)data[i].cname);
}
```

如果需要手動連線，進行細節操作

```csharp
SQL db = new SQL(); 
db.connet();
SqlDataReader stmt = db.query(sql);
dynamic[] data = dynamicFetch(stmt);
stmt.Close();
db.close();
```

**`quickDynamicQuery()` 與 `dynamicExec()`**

新增了快速執行 stored procedure，將返回 `dynamic` 型態的資料，查詢失敗一樣回傳 `null`  

```csharp
SQL db = new SQL();

var input_para = new
{
    act = "admin",
    passportcode = "1234567890"
};

var output_para = new
{
    res = SqlDbType.Int
};

dynamic data = db.quickDynamicExec("xp_checklogin", input_para, output_para);
Trace.WriteLine((int)data.res);
```

如果需要手動連線，進行細節操作

```csharp
SQL db = new SQL();
db.connet();
dynamic obj = dynamicExec(sql, input_para, output_para);
db.close();
```



## `v1.19.1`

新增了全新的**平台驗證**功能於 `using ZapLib.Security`  

**Controller 自動驗證**

提供 WebApi Controller 新的標籤 `[ValidPlatform]` 用於驗證是否為信任的請求，使用方式如下：

```csharp
// 在某一個 Controller 的 Action 中
[ValidPlatform]
[Route("test")]
public HttpResponseMessage test_file()
{
    // 如果驗證通過才會進入這個 Action    
}
```  
    
**`Fetch` 附加驗證**  

如果使用 `Fetch` 呼叫具有 `[ValidPlatform]` 驗證的 API，可以啟用 `validPlatform` 改為 `true` 便會自動附加驗證的資訊  

```csharp
Fetch2 f = new Fetch2("https://httpbin.org/post");
// 附加平台驗證資訊
f.validPlatform = true;
object res = f.post<object>(new { data = "123", result = true, number = 123 });
``` 

**`系統金鑰` 與 `上帝鑰匙`**

要通過平台驗證，兩個平台必須要有相同的 **`系統金鑰`**，如果要改變預設的 **`系統金鑰`**，可以在系統進入點添增設定

`WebApiConfig.cs`  
```csharp
ZapLib.Security.Const.Key = "新的金鑰";
``` 

為了開發人員方便，驗證留有一個後門 **`上帝鑰匙`** 可以直接通過驗證，預設為 `nvOcQMfERrASHCIuE797`，也可以在系統進入點修改它

`WebApiConfig.cs`  
```csharp
ZapLib.Security.Const.GodKey = "上帝鑰匙";
``` 


**驗證方式與規格**

平台驗證實作了 [DES](https://zh.wikipedia.org/wiki/%E8%B3%87%E6%96%99%E5%8A%A0%E5%AF%86%E6%A8%99%E6%BA%96) 方法，但是稍作改良，請求端會再 HTTP Header 附加 3 個欄位資料  

| key  |  description |
| --------  | -------- | 
| `Channel-Signature` | 將傳送的資料以 MD5 加密後的字串，作為請求的簽章 |  
| `Channel-Iv` | 一組每次都會亂數產生的加密種子 |  
| `Channel-Authorization` | 以 `Channel-Signature` + `Channel-Iv` + `系統金鑰` 使用 DES 雜湊後的結果 |

如果在 Controller 附加了 `[ValidPlatform]` 則會在進入 Action 之前，先將 `Channel-Signature` + `Channel-Iv` + `系統金鑰` 在進行一次 DES 雜湊，比對 `Channel-Authorization` 數值是否相同，如果相同就會驗證通過。   
驗證不通過時，會直接回傳 `401 Unauthorized` 未授權回應。




## `v1.18.2`

新增了 `ExtApiHelper.getStreamResponse` 用從伺服器中抓取檔案並以串流方式回應，使用範例如下：
   
**`getStreamResponse`**  
  
| arg  |  type | required | description |
| --------  | -------- | -------- |  -------- |
| file         |   string 或 byte[]   | Y | 檔案實體路徑 或 存放於記憶體中的檔案 |
| name         |   string     | N | 檔案名稱，如果沒有給予，則會以亂數命名 |
| type         |   string     | N | 檔案 MIME TYPE，如果沒有給予，則會以預設 `application/octet-stream` |
| disposition  |   string     | N | 串流方式，預設為 `attachment` 會直接下載檔案，如果要在瀏覽器中顯示，例如圖片，可以設定成 `inline` |
  
> 注意：`disposition` 如果設置為 `inline`，`type` 也必須設定成可以支援直接顯示的類型 e.g. `image/jpeg`, `image/png` ...
  
```csharp
// 在某一個 Controller 的 Action 中
ExtApiHelper api = new ExtApiHelper(this);
// 將會直接下載，並以亂數命名檔案名稱
return api.getStreamResponse(@"D:\a.txt");
```  
  
## `v1.17.0`
新增了 `ExtApiHelper` 擴充功能 `addIdentityPaging(ref string sql, string orderby = "since desc", string idcolumn = null, string nextId = null)`  
可以使用 ID 來做為分頁基準，細節可以參閱 [Identity Paging
](http://192.168.1.136/SideProject/ZapLib/issues/7) 中的描述，使用範例如下：

* 第一頁(第一次抓取)，`nextId` 為 `null`：

```csharp
// 在某一個 Controller 的 Action 中
ExtApiHelper api = new ExtApiHelper(this);
string sql = "select * from entity where eid>10";
string nextId = null;
api.addIdentityPaging(ref sql, "eid desc", "eid", nextId);

// 此時 sql 是：select top(51) * from entity where eid>2 order by eid desc
```

* 此時請注意，API 預設抓取 n 筆資料(在 `Web.config` 中設定 `APIDataLimit` )，但 `addIdentityPaging` 會抓取 n+1 筆資料，目的是為了判斷是否還能翻下一頁，請自行刪除最後一筆資料並取得最後一筆資料的 ID，作為下一次翻頁的基準

```csharp
SQL db = new SQL();
ModelEntity[] data = db.quickQuery<ModelEntity>(sql);

if(data.Length > int.Parse(Config.get("APIDataLimit")))
{
	// 取出最後一筆的 ID
	string nextId = data[data.Length - 1].eid.ToString();
	// 刪除最後一筆資料
	Array.Resize(ref data, data.Length - 1);
}
```
  
* 第二頁(第二次抓取)，`nextId` 為 `100`：
  
```csharp
string sql = "select * from entity where eid>10";
api.addIdentityPaging(ref sql, "eid desc", "eid", nextId);

// 此時 sql 是：with tb as(select ROW_NUMBER() over(order by eid desc) as _seq,* from entity where eid>2) select top(51) * from tb where _seq>=(select _seq from tb where eid='100') order by eid desc
```


## `v1.16.1`
新增了 `ApiController` 擴充了 SignalR 功能的 `ApiControllerSignalR` 類別，使用範例如下：  
  
首先在 Web API 專案根目錄下新增 `Startup.cs` 並撰寫以下程式碼 (這裡的設定可以依照需求調整)
```csharp
public class Startup
{
    public void Configuration(IAppBuilder app)
    {
        var hubConfiguration = new HubConfiguration();
        hubConfiguration.EnableDetailedErrors = true;
        app.MapSignalR(hubConfiguration);
    }
}
``` 
  
接下來在根目錄下的 `Global.asax` 中加入以下程式碼 (這裡的 `TimeSpan.FromSeconds(10)` 可依照需求調整)

```csharp
GlobalHost.Configuration.DisconnectTimeout = TimeSpan.FromSeconds(10);
```
  
接下來新增一個 `Hubs` 目錄，並在其中新增一個 `MsgHub.cs` 類別

```csharp
[HubName("messaging")]
public class MsgHub : Hub
{
	// write something...
}
```
  
最後，開啟欲使用 SignalR 的 Controller，將原先的 `ApiController` 改為 ZapLib 提供的 `ApiControllerSignalR`，如此一來便能使用下方展示的擴充功能
  
`___Controller.cs`  

```csharp
[RoutePrefix("api")]
public class MsgController : ApiControllerSignalR<MsgHub>
{
    [HttpPost]
    [Route("msg/send")]
    public HttpResponseMessage sendMsg([FromBody]ModelMsg msg)
    {
		// 廣播給某個對象
		Hub.Clients.Client( id ).pushMsg("...");
		// 檢查特定 ID 連線狀態
		bool isAlive = IsConnectionIdAlive("Connection ID");

		string[] connectionIds = new string[]{   };

		// 廣播給某群人
		Hub.Clients.Clients(connectionIds).receiveMsg("...");
		
		// 解析一群 ID 的連線狀態
		IList<string> Alive, Dead;
        ResolveConnectionIds(connectionIds, out Alive, out Dead);

		foreach(var id in Dead)
		{
			// 無效的連線 ID
		}
	}
}
```

> 其中 `Hub` 是 `GlobalHost.ConnectionManager.GetHubContext` 取出的 `HubContext` 可以直接使用原生的功能 
> 另外 `ResolveConnectionIds(IList<string> connectionIds, out IList<string> Alive, out IList<string> Dead)` 與 `IsConnectionIdAlive(string connectionId)` 提供的功能實作原理，可以參閱[技術說明](https://www.facebook.com/groups/1634561570101701/permalink/2516506991907150/)


## `v1.15.0`

* 可以取得 SQL 最後錯誤資訊，不一定要寫入 Log 觀察，使用範例如下：  
```csharp
SQL db = new SQL("localhost", "dbname", "account", "password");
object[] o = db.quickQuery<object>("select * from class");
string error = db.getErrorMessage(); // 輸出錯誤資訊
```
  
* 修復 `ExtApiHelper.getAttachemntResponse` 無法指定副檔名的問題，，使用範例如下：  
```csharp
ExtApiHelper api = new ExtApiHelper(this);
api.getAttachemntResponse("內容","file_name.txt"); // 可以直接指定副檔名，如果沒有指定則無附檔名
```  
  
* 新增 MyLog 可以指定儲存位置與 Log 的檔案名稱
```csharp
MyLog log = new MyLog();
log.path = "D:\\Log"; // 指定儲存路徑，預設為抓取 config 中的 Storage 設定，如果都沒有指定則不會進行 Log 寫入
log.name = "mylog"; // 指定 log 檔案名稱，預設為 yyyyMMdd
log.write("wewfewfewfewfewf"); 
log.write("safawfqafw");
```
  
* 新增 Fetch 可以取得 Response 的 Header 資訊 (如果無法取得 Header 則會回傳 NULL)
```csharp
Fetch fetch = new Fetch("http://localhost");
fetch.get(null);
string header_value = fetch.getResponseHeader("key"); // 取得指定 Header 
WebHeaderCollection all_headers = fetch.getResponseHeaders();  // 取得全部 Headers
```
  

## `v1.14.0`
  * 新增了 SQL BCP 的功能，可以快速寫入大量資料，使用範例如下：  

```csharp
// 建立 DataTable 物件
var dt = new DataTable();
dt.Columns.Add("words", typeof(string));

// 將資料塞進 DataTable 中
foreach (string word in words)
{
    var row = dt.NewRow();
    row["words"] = new_word;
    dt.Rows.Add(row);
}
// 全部一次性寫入
SQL db = new SQL();
result = db.quickBulkCopy(dt, "dbo.UD");
```
