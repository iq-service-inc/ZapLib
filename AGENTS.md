# AGENTS.md

給接手 ZapLib 的 AI/開發者看的工作筆記。這份文件記錄的是本 repo 目前實際狀態；開發前先讀，避免把舊專案當成新式 .NET 專案處理。

## 專案概覽

- ZapLib 是 .NET Framework C# library，目前主線版本為 `2.5.0`，目標框架是 `.NET Framework 4.7.2`。
- 主專案是傳統 non-SDK-style `csproj`，使用 `packages.config` 與 `packages/` hint path；不要未經要求改成 SDK-style、PackageReference 或跨 target。
- 核心定位：以簡短 API 包裝 HTTP request、SQL Server / Oracle query、ASP.NET Web API helper、SMTP mail、regex、JSON/path、crypto、logging、zip、reflection/dynamic utility。
- public API 已上 NuGet，改動時優先維持 backward compatibility。既有拼字錯誤方法已用 `[Obsolete]` 相容保留，預計 v3.0 才移除：
  - `SQL.Connet()` -> `SQL.Connect()`
  - `SQL.BuildconnString()` -> `SQL.BuildConnectionString()`
  - `OracleSQL.Connet()` -> `OracleSQL.Connect()`
  - `Fetch.SetRequestContnet()` -> `Fetch.SetRequestContent()`

## 目錄地圖

- `ZapLib/`：主 library。常見 namespace 為 `ZapLib`、`ZapLib.Utility`、`ZapLib.Security`、`ZapLib.Json`。
- `ZapLibUnitTest/`：目前 solution 內的 MSTest 專案，target `net472`。混有真正單元測試、外部整合測試、固定路徑測試與未完成 stub。
- `ZapLibTests2/`：舊的 `net45` 測試專案，不在 `ZapLib.sln` 中，主要作為 legacy 參考。
- `docs/`：DocFX 文件站來源。`docs/api/*.yml`、`docs/_site/`、`docs/obj/` 是產物，不要手改。
- `.github/workflows/docs.yml`：目前 CI 只建主專案並產生 DocFX，不跑完整測試。
- `tmp/`：開發/測試輔助 SQL 或資料產生腳本。

## 建置與工具

本機 `msbuild` / `vstest.console` 不一定在 PATH。這台機器可用的路徑：

```powershell
& 'C:\Program Files\Microsoft Visual Studio\18\Professional\MSBuild\Current\Bin\MSBuild.exe' ZapLib.sln /p:Configuration=Debug /m /nologo /verbosity:minimal
```

已確認上述指令可成功建置 `ZapLib` 與 `ZapLibUnitTest`。

乾淨環境需要先還原 NuGet packages：

```powershell
nuget restore ZapLib.sln
```

CI 文件流程若只需要主專案 metadata，可比照 `.github/workflows/docs.yml`：

```powershell
nuget restore ZapLib\packages.config -PackagesDirectory packages
msbuild ZapLib\ZapLib.csproj /p:Configuration=Release /m /nologo /verbosity:minimal
docfx docs/docfx.json --logLevel Info
```

## 測試現況

不要直接把全量 `ZapLibUnitTest` 失敗視為 regression。此測試專案目前包含：

- 需要內部 SQL Server / Oracle DB 的測試。
- 需要外部 HTTP、SMTP、固定 cookie 或 proxy 的測試。
- 依賴 `D:\Storage`、`D:\Downloads` 等本機路徑的測試。
- 多個 `Assert.Fail()` 未完成 stub。
- 少數目前預期與實作不一致的測試，例如 ValidPlatform IV 長度、部分 `BuildConnectionString` 字串大小寫/AlwaysOn 預期。

目前可作為快速 smoke test 的穩定切片：

```powershell
& 'C:\Program Files\Microsoft Visual Studio\18\Professional\Common7\IDE\CommonExtensions\Microsoft\TestWindow\vstest.console.exe' ZapLibUnitTest\bin\Debug\ZapLibUnitTest.dll /TestCaseFilter:"FullyQualifiedName~ZapLib.Utility.Tests.CastTests|FullyQualifiedName~ZapLib.Security.Tests.CryptoTests|FullyQualifiedName~ZapLib.Security.Tests.ValidPlatformAttributeTests|FullyQualifiedName=ZapLib.Utility.SQLString.Tests.OrderByTests.WhiteListTest|FullyQualifiedName=ZapLib.Tests.DynamicObjectTests.DynamicObjectTest|FullyQualifiedName=ZapLib.Tests.ExpParamTests.ReplaceSqlTest|FullyQualifiedName~ZapLib.Tests.RegExpTests|FullyQualifiedName=ZapLib.Tests.ExtApiHelperTests.addIdentityPaging|FullyQualifiedName=ZapLib.Tests.SQLTests.BuildConnectionStringTest3|FullyQualifiedName=ZapLib.Tests.SQLTests.SQLDBReplaceTest|FullyQualifiedName=ZapLib.Tests.SQLTests.QuickExecuteNoWaitDoesNotOpenCurrentInstanceConnection|FullyQualifiedName=ZapLib.Tests.SQLTests.QuickExecNoWaitDoesNotOpenCurrentInstanceConnection|FullyQualifiedName=ZapLib.Tests.MyLogTests.ForceLogWritesSingleFallbackFileWhenOriginalFileLocked|FullyQualifiedName=ZapLib.Tests.MyLogTests.ForceLogDisabledDoesNotCreateFallbackFile" /InIsolation
```

已確認此切片目前 `23/23` 通過。若改動 SQL / Oracle / Fetch / Mailer 等外部整合功能，請新增或挑選更貼近改動面的測試，而不是只依賴 smoke test。

## 程式碼慣例

- 保持 .NET Framework 4.7.2 與同步式 API 風格；既有 `Fetch` / DB helper 多用 `.Result` 與 sync method。
- public type/method/property 盡量補 XML doc comment，文件站會從 XML comment 產生 API reference。
- 既有註解與文件多為繁體中文，可延續繁中；API 名稱維持英文。
- 新增 public API 時要考慮 NuGet 使用者相容性，避免改變既有方法回傳型別、例外語意、預設值或命名。
- 需要重新命名 public API 時，先新增正確名稱，再保留舊名稱 `[Obsolete]` alias。
- 既有 helper 的失敗模式多為記錄 `MyLog` / `errormessage` 後回傳 `null`、`false` 或 `default`；除非明確設計新行為，盡量不要突然改成 throw。
- 反射/型別轉換優先沿用 `ZapLib.Utility.Mirror`、`Cast.To`、`Cast.ToEnum`。

## 重要模組脈絡

- `SQL`：SQL Server helper，支援 connection string name 或完整 connection string、`QuickQuery<T>`、`QuickDynamicQuery`、`QuickExec<T>`、`QuickExecuteNoWait`、`QuickExecNoWait`、`QuickBulkCopy`、transaction、AlwaysOn read-only routing、`SQLDBReplace`。
- `SQL.BuildConnectionString()` 會補齊/覆寫 `Connect Timeout`、`Encrypt`、`TrustServerCertificate`、`MultiSubnetFailover`、`ApplicationIntent`。`EnableDBAlwaysOn=false` 時會強制 `ApplicationIntent=ReadWrite`。
- `OracleSQL`：v2.5 新增，使用 `Oracle.ManagedDataAccess`，目前只支援 query 系列；`@name` 參數會轉成 Oracle `:name`，並使用 `BindByName=true`。
- `Fetch`：包裝 `HttpClient`，每個 instance 只能 `Send()` 一次；支援 JSON、form-url-encoded、multipart、cookie/header/proxy、`ValidPlatform` header。
- `ValidPlatform` / `Crypto`：目前以 AES-CBC + HMAC-SHA256 / MD5 支撐內部 S2S 機制。不要隨意更改 `Const.Key` / `GodKey` 語意。
- `Config`：會讀寫 App.config / Web.config，部分測試會修改執行輸出目錄中的 config。
- `MyLog` / `LogExecTime`：多個模組用它們做錯誤與耗時紀錄，受 `SilentMode`、`ForceLog`、`Storage` 等 config 影響。`ForceLog` fallback 現在集中 append 到 `{fileName}.force{ext}`，不再產生 GUID suffix 檔案。

## 文件與版本

- 使用者文件在 `docs/articles/**`，主入口是 `docs/index.md` 與 `docs/articles/toc.yml`。
- README 是 NuGet/readme 與 GitHub 入口，新增重要功能時同步更新 `README.md`、`CHANGELOG.md`、相關 `docs/articles/**`。
- NuGet metadata 在 `ZapLib/ZapLib.nuspec`。版本號來源以 nuspec 為準；post-build event 會把 `AssemblyInformationalVersion` 同步到 `ZapLib/Properties/AssemblyInfo.cs`。
- `AssemblyVersion` / `AssemblyFileVersion` 目前仍維持 `1.0.0.0`，不要順手改，除非釋出策略明確要求。

## 工作樹注意事項

- 目前有兩個未追蹤 blog 草稿：`BLOG-release-zaplib-v2.5.0-with-ai.md`、`BLOG-zaplib-docs-with-ai.md`。它們不是本次接手整理產物，除非使用者要求，不要移動、刪除或格式化。
- `.gitignore` 會忽略 `bin/`、`obj/`、`packages/`、`TestResults/`、DocFX 產物與 `.claude/`。
- 測試設定中有舊式內部連線資訊與帳號字串；回覆或文件中不要複製敏感值，新增測試時也不要新增真實密碼或內部 endpoint。
