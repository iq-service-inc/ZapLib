# [ZapLib](https://www.nuget.org/packages/ZapLib/)

[![NuGet](https://img.shields.io/nuget/v/ZapLib.svg)](https://www.nuget.org/packages/ZapLib/)
[![License](https://img.shields.io/badge/license-MIT-blue.svg)](https://github.com/LinZap/ZapLib/blob/master/LICENSE.md)

Inspired by jQuery and Node.js, ZapLib is a lightweight library for C# that empowers developers to swiftly accomplish intricate tasks. Whether it's performing HTTP requests, executing SQL Server / Oracle queries, extending .NET Web API functions, sending SMTP emails, working with regular expressions, and more, all can be achieved using straightforward code with ZapLib.

## 📚 Documentation

**👉 [https://linzap.github.io/ZapLib/](https://linzap.github.io/ZapLib/)**

完整使用手冊：教學文件 + 自動產生的 API Reference。

## Installation

**Package Manager (latest stable)**

```
PM> Install-Package ZapLib
```

**Specific Version**

```
PM> Install-Package ZapLib -Version 2.6.0       # v2 stable (latest)
PM> Install-Package ZapLib -Version 2.5.0       # previous v2 stable
PM> Install-Package ZapLib -Version 1.23.0      # v1 legacy
```

**.NET CLI**

```bash
dotnet add package ZapLib
```

## System Requirement

| ZapLib Version | .NET Framework |
| --- | --- |
| ≥ `v2.1.0` | 4.7.2 |
| ≥ `v1.23.0` | 4.7.2 |
| ≥ `v1.12.0` | 4.5 |
| ≤ `v1.10.0` | 4.0 |

## SignalR Issue

Starting from version `v1.16.0`, SignalR is included. If you are using ZapLib in a .NET WebAPI project and you are **not** utilizing SignalR-related features, please add the configuration to your `Web.config` file:

```xml
<appSettings>
    <add key="owin:AutomaticAppStartup" value="false" />
</appSettings>
```

## Quick Start

**HTTP Request**

```csharp
using ZapLib;

Fetch f = new Fetch("https://httpbin.org/get");
dynamic result = f.Get<dynamic>(new { name = "ZapLib" });
Console.WriteLine(result.args.name);   // ZapLib
```

**SQL Query**

```csharp
using ZapLib;

SQL db = new SQL("DefaultConn");
dynamic[] rows = db.QuickDynamicQuery("SELECT * FROM Book WHERE id = @id", new { id = 1 });
Console.WriteLine(rows[0].name);
```

**Oracle Query** (since `v2.5.0`)

```csharp
using ZapLib;

OracleSQL db = new OracleSQL("OracleConn");
HR_Row[] users = db.QuickQuery<HR_Row>(
    "SELECT * FROM HR_VIEW WHERE EMPN = @empn",
    new { empn = "625871" }
);
```

更多範例請見 [完整文件](https://linzap.github.io/ZapLib/)。

## Feature Highlights

| Module | Purpose | Docs |
| --- | --- | --- |
| `Fetch` | HTTP request 封裝（GET/POST/PUT/DELETE/PATCH、JSON、檔案上傳） | [📖](https://linzap.github.io/ZapLib/articles/fetch/basic-usage.html) |
| `SQL` | SQL Server 連線與查詢、Bulk Copy、Transaction、Stored Procedure、NoWait | [📖](https://linzap.github.io/ZapLib/articles/sql/basic-usage.html) |
| `OracleSQL` | Oracle 資料庫連線（純託管，免安裝 Oracle Client） | [📖](https://linzap.github.io/ZapLib/articles/sql/oracle.html) |
| `ExtApiHelper` | ASP.NET Web API 2 擴充輔助 | [📖](https://linzap.github.io/ZapLib/articles/webapi/extapihelper.html) |
| `ApiControllerSignalR<T>` | Web API + SignalR | [📖](https://linzap.github.io/ZapLib/articles/webapi/signalr.html) |
| `[ValidPlatform]` | S2S API 平台驗證 | [📖](https://linzap.github.io/ZapLib/articles/webapi/valid-platform.html) |
| `Mailer` / `ImplicitMailer` | SMTP 寄信（MailKit / 隱式 SSL） | [📖](https://linzap.github.io/ZapLib/articles/mailer.html) |
| `RegExp` | jQuery 風格的正規表達式 | [📖](https://linzap.github.io/ZapLib/articles/regexp.html) |
| `JsonReader` / `JXPath` | 安全的深層 JSON 存取 | [📖](https://linzap.github.io/ZapLib/articles/json-reader.html) |
| `Crypto` / `MD5` | MD5 / AES 加解密、簽章 | [📖](https://linzap.github.io/ZapLib/articles/security/crypto.html) |
| `Config` | App.config / Web.config 讀寫 | [📖](https://linzap.github.io/ZapLib/articles/utility/config.html) |
| `MyLog` / `LogExecTime` | 檔案日誌、執行時間追蹤 | [📖](https://linzap.github.io/ZapLib/articles/logging/mylog.html) |
| `ZipHelper` | Zip 壓縮 | [📖](https://linzap.github.io/ZapLib/articles/utility/zip-helper.html) |
| `DynamicObject` / `ClassMirror` / `DllLoader` | 執行期建立 / 載入類別 | [📖](https://linzap.github.io/ZapLib/articles/dynamic-object.html) |

## Changelog

請見 [CHANGELOG.md](https://github.com/LinZap/ZapLib/blob/master/CHANGELOG.md) 或文件站的 [Changelog 頁面](https://linzap.github.io/ZapLib/articles/changelog.html)。

## License

ZapLib is released under the **MIT License**.

```
Copyright (C) 2018-2026 ZapLin

Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation files (the "Software"), to deal in the Software without restriction, including without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
```
