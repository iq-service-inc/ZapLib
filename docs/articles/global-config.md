# Global Config

ZapLib 各模組會自動讀取 `App.config` / `Web.config` 中的 `appSettings`，作為**全域行為控制**。本頁列出所有 ZapLib 會讀取的 config key，集中參考。

> 所有 config 都是**選填**。沒設定時各模組會走預設行為。

## Quick Reference

| Key | 使用者 | 預設值 | 說明 |
|---|---|---|---|
| [`SilentMode`](#silentmode) | `MyLog` | `false` | 全域關閉 ZapLib 內建 log |
| [`ForceLog`](#forcelog) | `MyLog` | `false` | 原始 log 寫入失敗時，集中寫入 `.force` fallback 檔 |
| [`MyLog`](#mylog) | `MyLog` | — | Log 儲存路徑（覆蓋 `Storage`） |
| [`Storage`](#storage) | `MyLog`, `ExtApiHelper.UploadFile` | — | 通用儲存路徑（Log、上傳檔案） |
| [`LogExecTime`](#logexectime) | `LogExecTime` | `false` | 紀錄 SQL 執行時間到 log |
| [`TLS12`](#tls12) | `Fetch` | `false` | 強制使用 TLS 1.2 |
| [`DBHost`](#sql-defaults) | `SQL()` 無參數建構子 | — | SQL Server IP |
| [`DBName`](#sql-defaults) | `SQL()` 無參數建構子 | — | 資料庫名稱 |
| [`DBAct`](#sql-defaults) | `SQL()` 無參數建構子 | — | 連線帳號 |
| [`DBPwd`](#sql-defaults) | `SQL()` 無參數建構子 | — | 連線密碼 |
| [`EnableDBAlwaysOn`](#enabledbalwayson) | `SQL` | `false` | 啟用 AlwaysOn 唯讀路由 |
| [`SQLDBReplace`](#sqldbreplace) | `SQL` | `false` | 啟用 DB Name 動態替換 |
| [`SQLDBReplace:xxx`](#sqldbreplace) | `SQL` | — | 動態替換規則 |
| [`MaxUploadFileSize`](#maxuploadfilesize) | `ExtApiHelper.UploadFile` | `5242880` (5 MB) | 上傳檔案大小上限 |
| [`APIDataLimit`](#apidatalimit) | `ExtApiHelper.AddIdentityPaging` | `50` | API 翻頁的 max page size |
| [`aspnet:DontUsePercentUUrlEncoding`](#aspnetdontusepercentuurlencoding) | `QueryString.Parse` | `false` | 切換 URL encoding 行為 |

---

## Log Related

### SilentMode

全域關閉 ZapLib 內建的 log 寫入（你自己用 `MyLog` 寫的不受影響）。

```xml
<add key="SilentMode" value="true" />
```

### ForceLog

當 log 檔案無法寫入時（例如多執行緒同時鎖檔），強制以同目錄的**單一 `.force` fallback 檔**寫入：

```xml
<add key="ForceLog" value="true" />
```

> 檔名範例：原始路徑 `D:\logs\20260520.txt` 失敗時，fallback 會寫入 `D:\logs\20260520.force.txt`。
>
> ForceLog 只在正常寫入失敗後啟用，fallback 寫入本身會加 lock；若 fallback 仍失敗，ZapLib 會靜默結束，不再遞迴重試或產生其他檔案。

### MyLog

指定 log 檔案儲存路徑。**優先於 `Storage`**：

```xml
<add key="MyLog" value="D:\logs\zaplib" />
```

### Storage

通用儲存路徑，由多個模組共用：

```xml
<add key="Storage" value="D:\zaplib-storage" />
```

* `MyLog` → 沒指定 `MyLog` 時的 fallback
* `ExtApiHelper.UploadFile()` → 上傳檔案的儲存位置

### LogExecTime

當設為 `true`，所有 SQL 查詢會自動寫入執行時間：

```xml
<add key="LogExecTime" value="true" />
```

Log 範例：

```
[11:40:19] [Log Exec Time] Exec sql: SELECT * FROM Users
Takes 0.142 second
```

---

## Network Related

### TLS12

當設為 `true`，`Fetch.Send()` 執行前會自動套用 `ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12`：

```xml
<add key="TLS12" value="true" />
```

詳見 [Fetch / Basic Usage — TLS 1.2 HTTPS](fetch/basic-usage.md#tls-12-https)。

### aspnet:DontUsePercentUUrlEncoding

切換 `QueryString.Parse` 的 URL encoding 行為：

```xml
<add key="aspnet:DontUsePercentUUrlEncoding" value="true" />
```

* `true` → 用 `HttpUtility.ParseQueryString().ToString()`（標準百分號編碼）
* `false`（預設） → 用 `Uri.EscapeUriString`（保留更多字元）

---

## SQL Related

### SQL Defaults

`SQL()` 無參數建構子會自動讀以下 4 個 key：

```xml
<add key="DBHost" value="10.0.0.1" />
<add key="DBName" value="MyDatabase" />
<add key="DBAct"  value="sa" />
<add key="DBPwd"  value="P@ssw0rd" />
```

```csharp
SQL db = new SQL();   // 自動帶入上面 4 個值
```

詳見 [SQL / Basic Usage](sql/basic-usage.md)。

### EnableDBAlwaysOn

啟用 SQL Server AlwaysOn 高可用性架構的 ReadOnly 路由支援：

```xml
<add key="EnableDBAlwaysOn" value="true" />
```

僅當此值為 `true` 時，`SQL.SQLReadOnly = true` 才會實際影響連線字串。詳見 [SQL / Query — DB Always-On Read-Only Routing](sql/query.md#db-always-on-read-only-routing)。

### SQLDBReplace

啟用 SQL 語法中的 **DB Name 動態替換**。常用於跨環境（DEV / UAT / PROD）共用 SQL：

```xml
<appSettings>
  <add key="SQLDBReplace" value="true" />
  <add key="SQLDBReplace:prod_db" value="dev_db" />
  <add key="SQLDBReplace:prod_log" value="dev_log" />
</appSettings>
```

設定後，任何 SQL 中的 `prod_db.dbo.*` 會在執行前**自動改成 `dev_db.dbo.*`**：

```csharp
// 原 SQL
string sql = "SELECT * FROM prod_db.dbo.Users";

// 實際送出
// SELECT * FROM dev_db.dbo.Users
```

支援 4 種大小寫不敏感的 pattern：`xxx.dbo`、`[xxx].[dbo]`、`[xxx].dbo`、`xxx.[dbo]`。

---

## Web API Related

### MaxUploadFileSize

`ExtApiHelper.UploadFile()` 接受的單檔最大 bytes：

```xml
<add key="MaxUploadFileSize" value="10485760" />   <!-- 10 MB -->
```

預設 `5242880`（5 MB）。超過此值的檔案會被靜默丟棄。

### APIDataLimit

`ExtApiHelper.AddIdentityPaging` / `AddPaging` 的最大每頁筆數上限：

```xml
<add key="APIDataLimit" value="100" />
```

預設 `50`。client 即使送 `?limit=999` 也會被夾在 `APIDataLimit` 內。

---

## .NET WebAPI Notes

若你的 Web API 專案**沒有**使用 SignalR，請務必加：

```xml
<appSettings>
  <add key="owin:AutomaticAppStartup" value="false" />
</appSettings>
```

> 從 ZapLib `v1.16.0` 起內建 SignalR + OWIN。沒關掉 OWIN 自動啟動會導致啟動時錯誤。

## See Also

* [Utility / Config](utility/config.md) — 程式中讀寫 config 的 API
* [Getting Started](getting-started.md)
