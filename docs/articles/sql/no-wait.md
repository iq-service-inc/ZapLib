# SQL - NoWait Execution

`QuickExecuteNoWait` / `QuickExecNoWait` 提供 fire-and-forget SQL 執行方式。呼叫端送出後會立即返回，不等待 SQL 或 Stored Procedure 執行完成，也不取得回傳值。

## Namespace

```csharp
using ZapLib;
```

## Execute SQL Text

```csharp
SQL db = new SQL("DefaultConn");
db.Timeout = 1800;

db.QuickExecuteNoWait(
    "UPDATE SomeTable SET Status = @Status WHERE Id = @Id",
    new { Status = 1, Id = 123 }
);

// 呼叫後立即繼續往下執行
```

## Execute Stored Procedure

```csharp
SQL db = new SQL("DefaultConn");
db.Timeout = 1800;

db.QuickExecNoWait(
    "sp_LongRunningJob",
    new { JobId = 123 }
);

// 呼叫後立即繼續往下執行
```

## Behavior

NoWait 方法會在背景任務中建立獨立的 `SqlConnection`、`SqlCommand` 與 `SqlTransaction`。它不會共用目前 `SQL` instance 的 `Conn` / `Cmd` / `Tran`，因此呼叫後仍可用同一個 `SQL` object 做其他同步查詢。

```csharp
SQL db = new SQL("DefaultConn");

db.QuickExecNoWait("sp_LongRunningJob", new { JobId = 123 });

Book[] books = db.QuickQuery<Book>("SELECT * FROM Book");
```

## Error Handling

NoWait 是 best-effort fire-and-forget：

* 不回傳結果
* 不支援 output parameter
* 不讓呼叫端 await
* 背景任務例外不會拋回呼叫端
* 執行失敗會寫入 `MyLog`
* 程式或 AppDomain 結束時，不保證背景任務一定完成

## Transaction

如果建立 `SQL` 時啟用 transaction，NoWait 背景任務會建立自己的 transaction：

```csharp
SQL db = new SQL("DefaultConn", transaction: true);
db.QuickExecuteNoWait("UPDATE Job SET Status = @Status", new { Status = 1 });
```

成功時會 commit；失敗時會 rollback。這個 transaction 不會共用目前 instance 的 `Tran`。

## See Also

* [Modify](modify.md)
* [Stored Procedure](stored-procedure.md)
* [Transaction](transaction.md)
