### Extract WHERE Clause Conditions

```csharp
var sql = "SELECT * FROM Users WHERE Id = 1 AND Active = 1 AND Email LIKE '%@test.com'";
var conditions = SqlInterrogator.ExtractWhereClausesFromSql(sql);

// Result:
// [
//   ((ColumnName: "Id", Alias: null), "=", "1"),
//   ((ColumnName: "Active", Alias: null), "=", "1"),
//   ((ColumnName: "Email", Alias: null), "LIKE", "'%@test.com'")
// ]
```

#### With SQL Parameters

```csharp
var sql = "SELECT * FROM Users WHERE Id = @userId AND Status = @userStatus";
var conditions = SqlInterrogator.ExtractWhereClausesFromSql(sql);

// Result:
// [
//   ((ColumnName: "Id", Alias: null), "=", "@userId"),
//   ((ColumnName: "Status", Alias: null), "=", "@userStatus")
// ]
```

### WHERE Clause Operators

- Comparison: `=`, `!=`, `<>`, `>`, `<`, `>=`, `<=`
- Pattern matching: `LIKE`, `NOT LIKE`
- Set operations: `IN`, `NOT IN`
- NULL checks: `IS NULL`, `IS NOT NULL`
- **SQL Parameters:** `@parameterName` (e.g., `WHERE Id = @userId`)
