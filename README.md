# ZapLib

ZapLib 受到 jQuery, Node.js 的靈感啟發，在 C# 也提供一套非常輕巧的函式庫，開發人員能夠快速完成複雜的功能。無論您需要 Http 請求、SQL Server 的查詢、.NET Web Api 的擴充函式、SMTP 寄信、Regular Expression... 等，都能用簡單的程式碼來完成它！

## Installation

**Package Manager**

```
PM> Install-Package ZapLib -Version 1.16.0
```

## System requirement

* `v1.10.0` 以前的版本支援 .NET Framework 4.0 以上
* `v1.12.0` 開始的版本僅支援 .NET Framework 4.5 以上

## API Reference

* Fetch：
* SQL：
* Extension Web Api Helper：
* Mailer：
* Regular Expression：
* Query String：
* Config：
* Log：
* 實務案例(一)：
* 實務案例(二)：


## ChangeLog
改版紀錄

### `v1.16.0`
新增了 SignalR 的 ApiController `ApiControllerSignalR`，使用範例如下：  

`MsgHub.cs`  

```csharp
[HubName("messaging")]
public class MsgHub : Hub
{
	// write something...
}
```
  
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

		string[] connectionIds = new string[]{ /*...*/ };
		// 廣播給某群人
		Hub.Clients.Clients(connectionIds).receiveMsg(m);
		
		// 解析一群 ID 的連線狀態
		List<string> Alive, Dead;
		ResolveConnectionIds(connectionIds, Alive out, Dead out);
		foreach(var id in Dead)
		{
			// 無效的連線 IDs
		}
	}
}
```


### `v1.15.0`

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
  

### `v1.14.0`
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

## License MIT

	Copyright (C) 2018 ZapLin
	Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation files (the "Software"), to deal in the Software without restriction, including without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so, subject to the following conditions:
	The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.
	THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
