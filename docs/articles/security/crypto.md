# Security - Crypto

`Crypto` 提供 MD5、AES 可逆加解密、亂數字串等基本工具。底層採用 .NET 內建 `System.Security.Cryptography`，MD5 部分使用 ZapLib 自行實作的版本（`ZapLib.Utility.MD5`）。

> ⚠️ 可逆加密適合保護短文字或內部驗證資料，不適合儲存密碼。密碼請使用 BCrypt / PBKDF2 / Argon2 這類 password hashing。

## Namespace

```csharp
using ZapLib.Security;
```

## MD5 Hashing

```csharp
Crypto crypto = new Crypto();
string hash = crypto.Md5("Hello, ZapLib");
Console.WriteLine(hash);
```

**輸出：**

```
N1KFvCxBoBgKxvw4j7w2bg==
```

> **回傳格式**：Base64 string（不是常見的 hex string）。如果你需要 hex 格式，請用 `Convert.ToHexString()`（.NET 5+）或自行轉換。

### Encoding 選擇

預設用 ASCII 編碼輸入字串。需要 UTF-8（含中文等多位元字元）：

```csharp
Crypto crypto = new Crypto(Encoding.UTF8);
string hash = crypto.Md5("你好，ZapLib");
```

## AES Encryption / Decryption

`AESEncryption` / `AESDecryption` 使用 AES-CBC 加密，並以 HMAC-SHA256 驗證密文完整性。回傳的密文是 Base64 字串，內含 HMAC 驗證碼；IV 會另外存放在 `crypto.IV`。

### Encrypt

```csharp
Crypto crypto = new Crypto(Encoding.UTF8);
string encrypted = crypto.AESEncryption("secret message");
string iv = crypto.IV;   // 自動產生的 Base64 16-byte IV，解密時需要

Console.WriteLine($"Encrypted: {encrypted}");
Console.WriteLine($"IV: {iv}");
```

### Decrypt

```csharp
Crypto crypto = new Crypto(Encoding.UTF8);
string original = crypto.AESDecryption(encrypted, iv);
Console.WriteLine(original);   // "secret message"
```

### 指定 IV

```csharp
Crypto crypto = new Crypto(Encoding.UTF8);
string iv = Convert.ToBase64String(Guid.NewGuid().ToByteArray()); // 16 bytes

string encrypted = crypto.AESEncryption("text", iv);
string original = crypto.AESDecryption(encrypted, iv);
```

> 每次加密都應使用新的 IV。重用 IV 會降低加密強度。

### 完整性驗證

`AESDecryption` 會先驗證 HMAC。密文遭竄改、IV 錯誤或 `Const.Key` 不一致時，會丟出 `CryptographicException`，而不是回傳被破壞的明文。

## Random String

產生 `[A-Za-z0-9]` 範圍內的隨機字串：

```csharp
Crypto crypto = new Crypto();
string token = crypto.RandomString(32);
// 輸出範例：aB3xK9pMz7tQwR2sLv8nY4hG1jF6dC0e
```

> ⚠️ **不是密碼學等級的亂數**。內部用 `new Random()`，**可預測、可被攻擊**。
>
> **不適合**：API token、session id、密碼重設碼。
> **適合**：測試資料、UI placeholder、display-only ID。
>
> 需要密碼學亂數請用：
>
> ```csharp
> using System.Security.Cryptography;
> byte[] bytes = new byte[32];
> using (RandomNumberGenerator rng = RandomNumberGenerator.Create())
> {
>     rng.GetBytes(bytes);
> }
> string secureToken = Convert.ToBase64String(bytes);
> ```

## Security Notes

### Const.Key — 內建金鑰

`AESEncryption` 使用 `ZapLib.Security.Const.Key` 作為對稱金鑰來源，並派生出 AES 與 HMAC 各自的 key：

```csharp
namespace ZapLib.Security
{
    public class Const
    {
        public static string Key = "...";       // 對稱金鑰來源
        public static string GodKey = "...";    // ValidPlatform bypass key
    }
}
```

⚠️ **這仍然是需要管理的金鑰**：

* 反編譯 ZapLib.dll 可能取得預設金鑰
* 所有沿用預設值的專案會共用同一支金鑰
* 生產環境應覆寫 `Const.Key`，或改造成從 `Config` / KMS 讀取

## When to Use What

| 需求 | ZapLib | 替代方案 |
|---|---|---|
| 短文字可逆加密 | `Crypto.AESEncryption` / `AESDecryption` | 平台 KMS / DPAPI |
| 簡單訊息簽章（內部信任） | `Crypto.Md5` | HMAC-SHA256 |
| 內部 S2S API 驗證 | `Crypto.AESEncryption` + `[ValidPlatform]` | JWT / OAuth |
| 密碼儲存 | ❌ 不要用 MD5 / AES | `BCrypt.Net-Next` / `Microsoft.AspNetCore.Identity` |
| 對外 API 加密通訊 | ❌ 不要用 | HTTPS + 應用層標準協定 |
| 密碼學等級亂數 | ❌ 不要用 `RandomString` | `RandomNumberGenerator` |

## See Also

* [MD5 (Custom Implementation)](md5.md) — `Crypto.Md5` 背後使用的版本
* [`[ValidPlatform]`](../webapi/valid-platform.md) — `Crypto` 的主要使用者
