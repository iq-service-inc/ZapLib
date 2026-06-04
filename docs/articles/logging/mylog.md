# MyLog

`MyLog` 是 ZapLib 內建的檔案日誌工具。多數 ZapLib 類別（`Fetch`、`SQL`、`Mailer`…）失敗時會用它寫 log，你也可以拿來記錄自己的程式行為。

## Namespace

```csharp
using ZapLib;
```

## Basic Write

```csharp
MyLog log = new MyLog();
log.Path = @"D:\logs";
log.Write("API 呼叫開始");
// ... 業務邏輯
log.Write("API 呼叫完成");
```

**輸出檔案**：`D:\logs\20260520.txt`

**內容**：

```
[14:30:01] API 呼叫開始
[14:30:03] API 呼叫完成
```

> 每天會自動產生一個新檔（命名為 `yyyyMMdd.txt`）。同一天的訊息會 append 到同個檔案。

## Configurable Path

寫入位置的優先順序：

1. **物件屬性** `log.Path`
2. **Config key** `MyLog`
3. **Config key** `Storage`

```xml
<appSettings>
  <add key="MyLog" value="D:\logs\zaplib" />
  <add key="Storage" value="D:\zaplib-storage" />
</appSettings>
```

```csharp
MyLog log = new MyLog();
// 沒設 log.Path → 走 Config["MyLog"] → 沒設則走 Config["Storage"]
log.Write("hello");
```

> ⚠️ **目錄不存在會直接 return**，不會自動建立、不會 throw。記得自己 `Directory.CreateDirectory()`。

## Custom Log Name

```csharp
MyLog log = new MyLog("api-trace.log");
log.Path = @"D:\logs";
log.Write("custom name");
```

**輸出檔**：`D:\logs\api-trace.log`

## Silent Mode

完全停止寫入（不影響檔案，純粹跳過 I/O）：

```csharp
MyLog log = new MyLog();
log.SilentMode = "true";
log.Write("這行不會被寫入");
```

或全域設定 — 詳見 [Global Config — SilentMode](../global-config.md#silentmode)。

## Force Log

當寫入失敗（多執行緒搶檔、檔案被鎖），預設**靜默忽略**。啟用 `ForceLog` 後，ZapLib 會把 fallback 內容集中寫到同目錄的 `.force` 檔案：

```xml
<add key="ForceLog" value="true" />
```

失敗時實際寫入：`D:\logs\20260520.force.txt`

ForceLog 內容會包含原始 log path、寫入失敗的 exception detail，以及原本要寫入的內容。多次 fallback 會 append 到同一個 `.force` 檔案，不再產生 GUID suffix 檔案。

詳見 [Global Config — ForceLog](../global-config.md#forcelog)。

## Read Log

逆向（從**最新**往**最舊**）翻頁讀回 log 內容：

```csharp
MyLog log = new MyLog();
log.Path = @"D:\logs";
log.PageSize = 4096;   // 每頁 4 KB

ModelLog page1 = log.Read(page: 1);

Console.WriteLine($"Total Page: {page1.MaxPage}");
Console.WriteLine(page1.Data);

if (!string.IsNullOrEmpty(page1.ErrMsg))
{
    Console.WriteLine("讀取失敗：" + page1.ErrMsg);
}
```

`ModelLog` 內容：

| 屬性 | 說明 |
|---|---|
| `Data` | 該頁的純文字內容 |
| `Page` | 目前頁碼 |
| `MaxPage` | 總頁數 |
| `PageSize` | 每頁大小（bytes） |
| `Path` | 實際讀取的檔案路徑 |
| `Result` | 是否成功 |
| `ErrMsg` | 錯誤訊息 |

## Windows Event Log

寫到 **Windows 事件檢視器**（不是檔案）：

```csharp
MyLog log = new MyLog();

try
{
    // ... 可能 throw 的程式碼
}
catch (Exception ex)
{
    // 預設用 Error 等級
    log.Event(ex);

    // 或自訂等級
    log.Event(ex, EventLogEntryType.Warning);
}

// 也可寫純訊息
log.Event("應用程式啟動", EventLogEntryType.Information);
```

呼叫者的 method name、檔案路徑、行號會**自動帶入**（透過 `CallerMemberName` / `CallerFilePath` / `CallerLineNumber`），方便除錯。

> ⚠️ 需要寫入 EventLog 的權限。一般 IIS Application Pool 帳號預設沒有 — 需手動授予或改用 Administrator 帳號跑。

## See Also

* [LogExecTime](log-exec-time.md) — 紀錄執行時間
* [Global Config](../global-config.md) — `MyLog` / `Storage` / `SilentMode` / `ForceLog`
