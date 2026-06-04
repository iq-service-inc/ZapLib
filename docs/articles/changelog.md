# Changelog

完整的版本紀錄請見專案根目錄的 [CHANGELOG.md](https://github.com/LinZap/ZapLib/blob/master/CHANGELOG.md)。

本頁列出近期版本的重點：

## `2.6.0`

### 🔒 Crypto 演算法更新

* 移除 `Crypto.DESEncryption()` / `Crypto.DESDecryption()`，改用 `Crypto.AESEncryption()` / `Crypto.AESDecryption()`
* 新增 AES-CBC + HMAC-SHA256 可逆加解密；密文內含 HMAC 驗證碼，解密前會先驗證完整性
* `[ValidPlatform]` 與 `Fetch.ValidPlatform` 的 `Channel-Authorization` 已同步改用 AES-CBC + HMAC-SHA256

### ✨ SQL NoWait

* 新增 `SQL.QuickExecuteNoWait()`：背景執行一般 SQL Text，呼叫端立即返回
* 新增 `SQL.QuickExecNoWait()`：背景執行 Stored Procedure，呼叫端立即返回
* NoWait 背景任務會使用獨立 `SqlConnection` / `SqlCommand` / `SqlTransaction`，不共用目前 instance 的 `Conn` / `Cmd` / `Tran`

### 🪵 MyLog ForceLog

* ForceLog fallback 改為集中寫入 `{fileName}.force{ext}`
* 不再遞迴呼叫 `_write()`，也不再產生 GUID suffix 檔案
* ForceLog fallback 寫入加上 lock 保護，並記錄原始 path、exception 與原本內容

---

## `2.5.0` 🎉

**v2.5.x 系列正式版**。同時也是新文件站 [linzap.github.io/ZapLib](https://linzap.github.io/ZapLib/) 的首個正式版。

### ✨ 新功能

* **`OracleSQL` 類別** — 純託管 Oracle 連線，免裝 Oracle Client。詳見 [SQL / Oracle](sql/oracle.md)
* 自動產生的 [API Reference](../api/) 由 DocFX 從 source code 的 XML doc comments 抽出

### 🔒 安全性更新

升級多個有 CVE 公告的相依套件，**無公開 API 變動**：

| 套件 | 原版本 | 新版本 | CVE |
|---|---|---|---|
| `Newtonsoft.Json` | 12.0.1 | 13.0.3 | [GHSA-5crp-9r3c-p9vr](https://github.com/advisories/GHSA-5crp-9r3c-p9vr) |
| `Microsoft.Owin` 家族 | 4.2.0 | 4.2.3 | [GHSA-3rq8-h3gj-r5c6](https://github.com/advisories/GHSA-3rq8-h3gj-r5c6) |
| `MailKit` + `MimeKit` | 3.3.0 | 4.16.0 | 3 個 advisories |

> MailKit 跳了 major 版本，但 ZapLib 的 `Mailer` 公開 API 維持不變，使用方式完全相同。

### 🔤 方法命名修正（含 Obsolete 別名）

修正三個 public method 的英文拼字錯誤。**舊名稱仍可使用**（編譯時有 warning），下一個 major 版本才會移除：

| 舊名稱 | 新名稱 |
|---|---|
| `SQL.Connet()` | `SQL.Connect()` |
| `SQL.BuildconnString()` | `SQL.BuildConnectionString()` |
| `OracleSQL.Connet()` | `OracleSQL.Connect()` |
| `Fetch.SetRequestContnet()` | `Fetch.SetRequestContent()` |

升級 2.4.x → 2.5.0 的程式碼**不需任何修改**，只會看到 deprecation warning。

---

## `2.5.0-beta1`

OracleSQL 類別首次出現。詳見 [SQL / Oracle](sql/oracle.md)。

---

## `2.4.13`

### `2.4.13-beta3`

* `SQL` 類別新增 Timeout 機制 — 除非程式明確指定，否則會嘗試從 Connection String 的 `Connect Timeout` 抓取，都抓不到才使用預設 30 秒

### `2.4.13-beta2`

* 實驗機制：當 `EnableDBAlwaysOn` 啟用且 `SQLReadOnly=true` 時，若 `QuickDynamicQuery` / `QuickQuery` 回傳 0 筆，會自動切回 `SQLReadOnly=false` 重新查詢。**避免讀寫分離架構下 secondary 尚未同步的問題**

詳見 [SQL / Query — DB Always-On Read-Only Routing](sql/query.md#db-always-on-read-only-routing)。

---

## `2.4.12`（含 `2.4.9` ~ `2.4.12`）

### SQL 讀寫分離支援

* 新增屬性 `SQL.EnableDBAlwaysOn` — 啟用 DB Always On 才有的 `ApplicationIntent=ReadOnly` 設定
* 新增屬性 `SQL.SQLReadOnly` — 動態切換 ReadOnly / ReadWrite
* 也可在 [Global Config](global-config.md#enabledbalwayson) 中指定 `EnableDBAlwaysOn=True`

---

## `2.4.8`

* `Config.Delete(key)` — 刪除 `appSettings` 中的參數
* `Config.DeleteConnectionString(key)` — 刪除 `connectionStrings` 中的連線字串

詳見 [Utility / Config](utility/config.md)。

---

## `2.4.7`

`Config` 系列新增 3 個功能（可在 Config 檔案被加密的狀況下運作）：

* `Config.SetOrAdd(key, val)` — 設定 / 新增 `appSettings` 參數
* `Config.SetOrAddConnectionString(key, val, providerName)` — 設定 / 新增 connectionStrings
* `Config.GetConnectionStrings()` — 取得所有連線字串設定

---

## `2.4.6`

* `Crypto.Md5()` 改為使用 ZapLib **自行實作**的 MD5（`ZapLib.Utility.MD5`），不再依賴 `System.Security.Cryptography.MD5`

詳見 [Security / MD5](security/md5.md)。

---

## `2.4.5`

* `SQL` 新增 `SQLDBReplace()` — 無須變更 SQL 語法即可自動替換 DB Name

詳見 [Global Config — SQLDBReplace](global-config.md#sqldbreplace)。

---

## `2.4.4`

* `SQL` 新增 `QuickBulkCopy()` — 大量寫入

詳見 [SQL / Bulk Copy](sql/bulk-copy.md)。

---

## `2.4.3`

* `MyLog` 支援自訂 Log 檔案副檔名
* `RegExp` 支援自訂 `RegexOptions`
* `SQL` 根據 ConnectionString 動態填補缺少的設定
* `Crypto` 支援自訂編碼

---

## `2.4.2`

* `Mailer` / `ImplicitMailer` 支援 Password 為 `null` 跳過驗證

---

## `2.4.1`

* `Mailer` / `ImplicitMailer` 新增 `AddAttachments()` — 取得附件 `cid` 可在 HTML body 中嵌入圖片
* 新增 `AttachmentsList` 屬性

詳見 [Mailer / Inline Image with Content-ID](mailer.md#inline-image-with-content-id)。

---

## `2.4.0`

* `Mailer` / `ImplicitMailer.Send()` 新增 `attchments` 參數
* 新增 Config `TLS12` — 強制使用 TLS 1.2

詳見 [Global Config — TLS12](global-config.md#tls12)。

---

## `2.3.0`

* 新增 `ImplicitMailer` 類別 — 底層採用 [Aegis Implicit Mail](https://github.com/nilnull/AIM)，支援 port 465 的隱式 SSL 加密

---

## `2.2.0`

* `Mailer` 改用 [MailKit](https://github.com/jstedfast/MailKit)，支援 TLS 1.2

> 原先 `SmtpClient` 不支援 TLS 且被微軟棄用。

---

## `2.1.0`

* `ExtApiHelper.Response` / `Request` 改為 public
* 新增 `ZipHelper` 類別 — 快速 Zip 壓縮
* 新增 `MyLog.Read(page)` — 翻頁讀取 log
* 新增 `Fetch.Patch` 方法
* 大量套件更新（jQuery、SignalR、WebApi、Owin、Newtonsoft.Json）

---

## `2.0.x` 重點

* `2.0.12` — 新增 `Mirror.GetClasses`、`ClassMirror`、`DllLoader`、`JXPath`、`Config.Refresh`
* `2.0.9` — 新增 [`DynamicObject`](dynamic-object.md)、修正 query string 中文亂碼
* `2.0.7` — `SQL` 新增 transaction 選項
* `2.0.6` — `ExtApiHelper.GetHeader<T>`、`Cast.ToEnum<T>`
* `2.0.5` — 新增 [`JsonReader`](json-reader.md)
* `2.0.4` — 新增 `[SQLType]` Attribute 與 `ISQLParam` 介面
* `2.0.3` — 新增 `ExpParam<T>` SQL 參數擴展

---

## `v1.19.1`

* 新增 [`[ValidPlatform]`](webapi/valid-platform.md) 平台驗證
* `Fetch.ValidPlatform` 屬性 — 自動附加驗證資訊
* `ZapLib.Security.Const.Key` / `Const.GodKey` — 系統金鑰與上帝鑰匙

---

## 完整紀錄

`v1.x` 及更早期的版本紀錄請見 **[完整 CHANGELOG.md](https://github.com/LinZap/ZapLib/blob/master/CHANGELOG.md)**。
