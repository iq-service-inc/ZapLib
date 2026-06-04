# Web API - ValidPlatform

`[ValidPlatform]` 是一個 `AuthorizeAttribute` — 加上它的 Web API action 會在執行前驗證請求是否來自「可信任的內部平台」。設計用途是**伺服器對伺服器（S2S）** 的內部 API 防護。

> ⚠️ 這不是替代 OAuth / JWT 等通用驗證機制。它使用對稱金鑰（`Const.Key`），適用於同公司內部、互相信任的服務之間。

## Namespace

```csharp
using ZapLib.Security;
```

## How It Works

驗證機制基於三個 HTTP Header：

| Header | 內容 |
|---|---|
| `Channel-Signature` | `MD5(request.Body)` |
| `Channel-Iv` | AES 加密的初始化向量（Base64 16-byte IV） |
| `Channel-Authorization` | `AES-CBC + HMAC-SHA256(Signature, IV, Const.Key)` |

伺服器收到請求時：

1. 重新計算 `MD5(request.Body)` → 得到 `InnerSignature`
2. 比對 `InnerSignature == Channel-Signature`（防止 body 被竄改）
3. 用 `Channel-Iv` + `Const.Key` 對 `Channel-Signature` 做 AES 加密並產生 HMAC
4. 比對加密結果是否等於 `Channel-Authorization`
5. 全部通過 → 放行；任一不通過 → 回 `401 Unauthorized`

## Server: Apply the Attribute

任何 `ApiController` 的 action 或整個 controller 加上 `[ValidPlatform]`：

```csharp
using ZapLib.Security;

public class InternalController : ApiController
{
    [HttpPost]
    [ValidPlatform]
    public HttpResponseMessage PurgeCache()
    {
        // 只有通過平台驗證的請求才會進到這裡
        var api = new ExtApiHelper(this);
        // ... 執行內部操作
        return api.GetResponse(new { ok = true });
    }
}
```

或保護整個 controller：

```csharp
[ValidPlatform]
public class InternalController : ApiController
{
    // 所有 action 都受保護
}
```

驗證失敗時，回應為：

```json
{ "error": "Request is not valid, please check [ValidPlatform]" }
```

Status code: `401 Unauthorized`。

## Client: Call a [ValidPlatform] API

最簡單的方式 — 用 ZapLib 的 [`Fetch`](../fetch/advanced.md#platform-validation-validplatform)，設定 `ValidPlatform = true`：

```csharp
using ZapLib;

Fetch f = new Fetch("https://internal-api.company.com/Internal/PurgeCache");
f.ValidPlatform = true;

dynamic result = f.Post<dynamic>(new { reason = "scheduled maintenance" });
```

`Fetch` 會自動：

1. 計算 body 的 MD5
2. 隨機產生 IV
3. 用 `Const.Key` 做 AES 加密並產生 HMAC
4. 把三個 header 塞進請求

## God Key Bypass

`Const.GodKey` 是一個**萬用通行證**。如果 `Channel-Authorization` 直接等於 `GodKey`，會跳過簽章比對。

> ⚠️ **僅限緊急 / 維運場景**。把 `GodKey` 暴露出去等於整個 ValidPlatform 機制失效。請確保它**永遠不被 commit 進公開 repo**，並定期輪替。

```csharp
public bool IsVaild(string content, string IV, string Authorization, string OuterSignature)
{
    if (Authorization == Const.GodKey) return true;
    // ... 一般驗證流程
}
```

## Manually Build Headers

不用 `Fetch`、想自己做 client？參考流程：

```csharp
using ZapLib.Security;

string body = JsonConvert.SerializeObject(new { reason = "test" });

Crypto crypto = new Crypto();
string signature = crypto.Md5(body);
string authorization = crypto.AESEncryption(signature);
string iv = crypto.IV;   // AESEncryption 內部產生並寫入 IV

// 用任意 HTTP client 送請求，帶上這三個 header
client.DefaultRequestHeaders.Add("Channel-Signature", signature);
client.DefaultRequestHeaders.Add("Channel-Iv", iv);
client.DefaultRequestHeaders.Add("Channel-Authorization", authorization);
```

## Security Notes

* **金鑰管理**：`Const.Key` 是寫死在 source code 的對稱金鑰。**這是這個機制的根本弱點**。建議改造為從 `Config` 或 KMS 讀取
* **重放攻擊**：本機制**沒有防重放**（沒有時間戳記、沒有 nonce）。攔截到請求的人可以無限次重放
* **演算法**：目前使用 AES-CBC + HMAC-SHA256 取代舊版 DES；若未來升級到支援 AEAD 的 target，可再評估 AES-GCM
* **用途定位**：適合「同內網互信、防誤觸」，不適合「對外開放、防駭客」

## See Also

* [Fetch — Platform Validation](../fetch/advanced.md#platform-validation-validplatform)
* [Crypto](../security/crypto.md)
