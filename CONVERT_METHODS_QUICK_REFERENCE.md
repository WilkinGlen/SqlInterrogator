# SqlInterrogator Convert Methods - Combination Quick Reference

## Method Signatures

```csharp
public static string? ConvertSelectStatementToSelectCount(string sql)
public static string? ConvertSelectStatementToSelectTop(string sql, int top)
public static string? ConvertSelectStatementToSelectDistinct(string sql)
public static string? ConvertSelectStatementToSelectOrderBy(string sql, string orderByClause)
```

## Common Combinations

### 1. TOP → DISTINCT
```csharp
var sql = "SELECT Name FROM Users";
var result = SqlInterrogator.ConvertSelectStatementToSelectTop(sql, 10);
// Result: "SELECT TOP 10 FROM Users"

result = SqlInterrogator.ConvertSelectStatementToSelectDistinct(result);
// Result: "SELECT DISTINCT TOP 10 FROM Users"
```

### 2. DISTINCT → TOP
```csharp
var sql = "SELECT Name FROM Users";
var result = SqlInterrogator.ConvertSelectStatementToSelectDistinct(sql);
// Result: "SELECT DISTINCT Name FROM Users"

result = SqlInterrogator.ConvertSelectStatementToSelectTop(result, 10);
// Result: "SELECT DISTINCT TOP 10 FROM Users"
```
**Note:** Order doesn't matter - both produce the same result!

### 3. DISTINCT → COUNT (Uses Subquery)
```csharp
var sql = "SELECT DISTINCT Name FROM Users";
var result = SqlInterrogator.ConvertSelectStatementToSelectCount(sql);
// Result: "SELECT COUNT(*) FROM (SELECT DISTINCT Name FROM Users) AS DistinctCount"
```

### 4. ORDER BY → TOP
```csharp
var sql = "SELECT Name FROM Users WHERE Active = 1";
var result = SqlInterrogator.ConvertSelectStatementToSelectOrderBy(sql, "Name ASC");
// Result: "SELECT Name FROM Users WHERE Active = 1 ORDER BY Name ASC"

result = SqlInterrogator.ConvertSelectStatementToSelectTop(result, 10);
// Result: "SELECT TOP 10 FROM Users WHERE Active = 1 ORDER BY Name ASC"
```

### 5. TOP → ORDER BY (Replaces Existing)
```csharp
var sql = "SELECT Name FROM Users ORDER BY Email DESC";
var result = SqlInterrogator.ConvertSelectStatementToSelectTop(sql, 10);
// Result: "SELECT TOP 10 FROM Users ORDER BY Email DESC"

result = SqlInterrogator.ConvertSelectStatementToSelectOrderBy(result, "Name ASC");
// Result: "SELECT TOP 10 FROM Users ORDER BY Name ASC"
```

### 6. Complete Transformation Chain
```csharp
var sql = "SELECT Name, Email FROM Users WHERE Active = 1";

// Step 1: Add DISTINCT
sql = SqlInterrogator.ConvertSelectStatementToSelectDistinct(sql);
// "SELECT DISTINCT Name, Email FROM Users WHERE Active = 1"

// Step 2: Add TOP
sql = SqlInterrogator.ConvertSelectStatementToSelectTop(sql, 25);
// "SELECT DISTINCT TOP 25 FROM Users WHERE Active = 1"

// Step 3: Add ORDER BY
sql = SqlInterrogator.ConvertSelectStatementToSelectOrderBy(sql, "Name ASC");
// "SELECT DISTINCT TOP 25 FROM Users WHERE Active = 1 ORDER BY Name ASC"

// Step 4: Convert to COUNT
sql = SqlInterrogator.ConvertSelectStatementToSelectCount(sql);
// "SELECT COUNT(*) FROM (SELECT DISTINCT TOP 25 FROM Users WHERE Active = 1 ORDER BY Name ASC) AS DistinctCount"
```

## Pagination Scenarios

### Dynamic Sorting with Pagination
```csharp
var sql = "SELECT * FROM Users WHERE Active = 1 ORDER BY Name ASC OFFSET 20 ROWS FETCH NEXT 10 ROWS ONLY";

// User changes sort (pagination preserved)
sql = SqlInterrogator.ConvertSelectStatementToSelectOrderBy(sql, "Email DESC");
// "SELECT * FROM Users WHERE Active = 1 ORDER BY Email DESC OFFSET 20 ROWS FETCH NEXT 10 ROWS ONLY"
```

### Get Count for Paginated Data
```csharp
var dataQuery = "SELECT * FROM Users WHERE Active = 1 ORDER BY Name ASC";

// Get count for same filters
var countQuery = SqlInterrogator.ConvertSelectStatementToSelectCount(dataQuery);
// "SELECT COUNT(*) FROM Users WHERE Active = 1 ORDER BY Name ASC"

// Execute both
var data = await db.QueryAsync<User>(dataQuery);
var total = await db.ExecuteScalarAsync<int>(countQuery);
```

## Real-World Use Cases

### 1. Data Grid with Dynamic Sorting
```csharp
public class DataGridService
{
    public async Task<PagedResult<User>> GetUsersAsync(
        string sortColumn, 
        string sortDirection, 
        int page, 
        int pageSize)
    {
        var sql = "SELECT * FROM Users WHERE Active = 1";
        
        // Add sorting
        sql = SqlInterrogator.ConvertSelectStatementToSelectOrderBy(
            sql, 
            $"{sortColumn} {sortDirection}");
        
        // Add pagination
        var offset = page * pageSize;
        sql = $"{sql} OFFSET {offset} ROWS FETCH NEXT {pageSize} ROWS ONLY";
        
        // Get data
        var data = await _db.QueryAsync<User>(sql);
        
        // Get count
        var countSql = SqlInterrogator.ConvertSelectStatementToSelectCount(sql);
        var total = await _db.ExecuteScalarAsync<int>(countSql);
        
        return new PagedResult<User>(data, total, page, pageSize);
    }
}
```

### 2. Top N Report
```csharp
public async Task<List<Product>> GetTopProductsAsync(int count, string sortBy)
{
    var sql = "SELECT ProductName, SalesTotal FROM Products WHERE Active = 1";
    
    // Add sorting
    sql = SqlInterrogator.ConvertSelectStatementToSelectOrderBy(sql, $"{sortBy} DESC");
    
    // Limit to top N
    sql = SqlInterrogator.ConvertSelectStatementToSelectTop(sql, count);
    
    return await _db.QueryAsync<Product>(sql);
}
```

### 3. Unique Values for Dropdown
```csharp
public async Task<List<string>> GetUniqueCountriesAsync()
{
    var sql = "SELECT Country FROM Customers WHERE Active = 1";
    
    // Get unique values
    sql = SqlInterrogator.ConvertSelectStatementToSelectDistinct(sql);
    
    // Sort alphabetically
    sql = SqlInterrogator.ConvertSelectStatementToSelectOrderBy(sql, "Country ASC");
    
    return await _db.QueryAsync<string>(sql);
}
```

### 4. Data Sampling
```csharp
public async Task<List<Product>> GetProductSampleAsync(int sampleSize)
{
    var sql = "SELECT * FROM Products WHERE Discontinued = 0";
    
    // Remove duplicates
    sql = SqlInterrogator.ConvertSelectStatementToSelectDistinct(sql);
    
    // Sort by category and price
    sql = SqlInterrogator.ConvertSelectStatementToSelectOrderBy(sql, "Category ASC, Price DESC");
    
    // Take sample
    sql = SqlInterrogator.ConvertSelectStatementToSelectTop(sql, sampleSize);
    
    return await _db.QueryAsync<Product>(sql);
}
```

## Important Behaviors

### Column Specifications Are Removed
All convert methods remove column specifications:
```csharp
var sql = "SELECT [Database].[dbo].[Users].[Name], [Database].[dbo].[Users].[Email] FROM Users";
var result = SqlInterrogator.ConvertSelectStatementToSelectTop(sql, 10);
// Result: "SELECT TOP 10 FROM Users"
```

### DISTINCT Detection
Methods detect DISTINCT even in preprocessed queries:
```csharp
var sql = "SELECT DISTINCT Name FROM Users";
var result = SqlInterrogator.ConvertSelectStatementToSelectTop(sql, 10);
// Result: "SELECT DISTINCT TOP 10 FROM Users" (DISTINCT preserved!)
```

### Null Propagation
Invalid inputs return null and propagate through chains:
```csharp
var sql = "UPDATE Users SET Active = 1"; // Not a SELECT
var result = SqlInterrogator.ConvertSelectStatementToSelectTop(sql, 10);
// Result: null

result = SqlInterrogator.ConvertSelectStatementToSelectDistinct(result);
// Result: null
```

### Idempotent Operations
Some operations are idempotent:
```csharp
// DISTINCT is idempotent
var sql = "SELECT DISTINCT Name FROM Users";
var result1 = SqlInterrogator.ConvertSelectStatementToSelectDistinct(sql);
var result2 = SqlInterrogator.ConvertSelectStatementToSelectDistinct(result1);
// result1 == result2 == "SELECT DISTINCT Name FROM Users"

// TOP with same value is idempotent
sql = "SELECT * FROM Users";
result1 = SqlInterrogator.ConvertSelectStatementToSelectTop(sql, 10);
result2 = SqlInterrogator.ConvertSelectStatementToSelectTop(result1, 10);
// result1 == result2 == "SELECT TOP 10 FROM Users"
```

### Order Independence
TOP and DISTINCT can be applied in any order:
```csharp
var sql = "SELECT Name FROM Users";

// Path A: TOP then DISTINCT
var pathA = SqlInterrogator.ConvertSelectStatementToSelectTop(sql, 10);
pathA = SqlInterrogator.ConvertSelectStatementToSelectDistinct(pathA);

// Path B: DISTINCT then TOP
var pathB = SqlInterrogator.ConvertSelectStatementToSelectDistinct(sql);
pathB = SqlInterrogator.ConvertSelectStatementToSelectTop(pathB, 10);

// pathA == pathB == "SELECT DISTINCT TOP 10 FROM Users"
```

## Best Practices

1. **Always check for null** before chaining methods
2. **Apply ORDER BY last** (except before pagination)
3. **Use COUNT for pagination** to get total row count
4. **DISTINCT + TOP order doesn't matter** - choose what makes sense
5. **Validate user input** when building dynamic ORDER BY clauses
6. **Consider performance** - DISTINCT on large datasets can be slow

## Testing

All combinations are thoroughly tested in:
- `SqlInterrogatorServiceTest/ConvertMethodCombinations_Should.cs` (33 tests)
- Individual method test files (681 tests)
- **Total: 714 passing tests**
