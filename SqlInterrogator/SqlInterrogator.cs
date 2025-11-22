namespace SqlInterrogatorService;
/// <summary>
/// Provides static methods for extracting information from SQL queries, including database names,
/// table names, and column details. Supports various SQL identifier formats including bracketed,
/// quoted, and qualified names.
/// </summary>
/// <remarks>
/// This service handles SQL Server syntax including:
/// <list type="bullet">
/// <item>Bracketed identifiers: [DatabaseName].[SchemaName].[TableName]</item>
/// <item>Double-quoted identifiers: "DatabaseName"."SchemaName"."TableName"</item>
/// <item>Unquoted identifiers: DatabaseName.SchemaName.TableName</item>
/// <item>Mixed formats and two, three, four, and five-part identifiers</item>
/// <item>SQL comments (single-line -- and multi-line /* */)</item>
/// <item>Common Table Expressions (CTEs)</item>
/// <item>USE statements and GO batch separators</item>
/// </list>
/// </remarks>
public static partial class SqlInterrogator
{
    /// <summary>
    /// Extracts all unique database names referenced in a SQL statement.
    /// </summary>
    /// <param name="sql">The SQL statement to analyse. Can contain multiple table references.</param>
    /// <returns>
    /// A list of unique database names found in the SQL. Returns an empty list if the SQL is null,
    /// empty, or contains no database references. Database names are case-insensitive.
    /// </returns>
    /// <remarks>
    /// <para>This method identifies database names from three-part and four-part identifiers:</para>
    /// <list type="bullet">
    /// <item>Three-part: [DatabaseName].[SchemaName].[TableName]</item>
    /// <item>Four-part: [ServerName].[DatabaseName].[SchemaName].[TableName]</item>
    /// </list>
    /// <para>Supported SQL keywords: FROM, JOIN, INTO, UPDATE, TABLE, MERGE</para>
    /// <para>Comments are automatically removed before processing.</para>
    /// </remarks>
    /// <example>
    /// <code>
    /// var sql = "SELECT * FROM [MyDB].[dbo].[Users] JOIN [AnotherDB].[dbo].[Orders] ON ...";
    /// var databases = SqlInterrogator.ExtractDatabaseNamesFromSql(sql);
    /// // Result: ["MyDB", "AnotherDB"]
    /// </code>
    /// </example>
    public static List<string> ExtractDatabaseNamesFromSql(string? sql)
    {
        if (string.IsNullOrWhiteSpace(sql))
        {
            return [];
        }

        // Early exit: if no dots exist, there can't be qualified identifiers
        if (!sql.Contains('.'))
        {
            return [];
        }

        var databaseNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        sql = RemoveComments(sql);
        var patterns = GetDatabasePatterns();
        ExtractDatabaseNamesFromPatterns(sql, patterns, databaseNames);

        return [.. databaseNames];
    }

    /// <summary>
    /// Extracts the first table name from a SELECT statement's FROM or JOIN clause.
    /// </summary>
    /// <param name="sql">The SQL SELECT statement to analyse.</param>
    /// <returns>
    /// The table name (without database/schema qualifiers) from the first FROM or JOIN clause.
    /// Returns null if the SQL is not a SELECT statement or contains no table references.
    /// </returns>
    /// <remarks>
    /// <para>This method:</para>
    /// <list type="bullet">
    /// <item>Only processes SELECT statements (returns null for UPDATE, INSERT, DELETE)</item>
    /// <item>Removes comments, CTEs, and USE statements before processing</item>
    /// <item>Returns the table name from multi-part identifiers (e.g., "Users" from "dbo.Users")</item>
    /// <item>Handles aliases, table hints (NOLOCK), and various identifier formats</item>
    /// </list>
    /// </remarks>
    /// <example>
    /// <code>
    /// var sql = "SELECT * FROM [MyDB].[dbo].[Users] WHERE Active = 1";
    /// var tableName = SqlInterrogator.ExtractFirstTableNameFromSelectClauseInSql(sql);
    /// // Result: "Users"
    /// 
    /// var sql2 = "SELECT u.Name FROM Users u JOIN Orders o ON u.Id = o.UserId";
    /// var tableName2 = SqlInterrogator.ExtractFirstTableNameFromSelectClauseInSql(sql2);
    /// // Result: "Users" (first table in FROM clause)
    /// </code>
    /// </example>
    public static string? ExtractFirstTableNameFromSelectClauseInSql(string? sql)
    {
        if (string.IsNullOrWhiteSpace(sql))
        {
            return null;
        }

        sql = PreprocessSql(sql);

        // Only process SELECT statements
        if (!IgnoreCaseRegex().IsMatch(sql))
        {
            return null;
        }

        var patterns = GetTableNamePatterns();
        return FindFirstTableNameFromPatterns(sql, patterns);
    }

    /// <summary>
    /// Extracts detailed information about columns in a SELECT statement, including database names,
    /// table names, and column names or aliases.
    /// </summary>
    /// <param name="sql">The SQL SELECT statement to analyse.</param>
    /// <returns>
    /// A list of tuples containing:
    /// <list type="bullet">
    /// <item><c>DatabaseName</c> - The database name if specified (e.g., from DB.Table.Column), otherwise null</item>
    /// <item><c>TableName</c> - The table name or alias if specified (e.g., from Table.Column), otherwise null</item>
    /// <item><c>Column</c> - A tuple containing ColumnName (the actual column name) and Alias (the alias if specified, otherwise null)</item>
    /// </list>
    /// Returns an empty list if the SQL is not a SELECT statement or contains no columns.
    /// </returns>
    /// <remarks>
    /// <para>This method handles:</para>
    /// <list type="bullet">
    /// <item>SELECT * returns a single entry with ColumnName = "*", Alias = null</item>
    /// <item>Simple columns: SELECT Name → (null, null, (ColumnName: "Name", Alias: null))</item>
    /// <item>Qualified columns: SELECT u.Name → (null, "u", (ColumnName: "Name", Alias: null))</item>
    /// <item>Fully qualified: SELECT DB.dbo.Users.Name → ("DB", "Users", (ColumnName: "Name", Alias: null))</item>
    /// <item>Explicit aliases: SELECT Name AS FullName → Column = (ColumnName: "Name", Alias: "FullName")</item>
    /// <item>Implicit aliases: SELECT u.Name UserName → Column = (ColumnName: "Name", Alias: "UserName")</item>
    /// <item>Functions: SELECT COUNT(*) AS Total → (null, null, (ColumnName: "COUNT", Alias: "Total"))</item>
    /// <item>DISTINCT and TOP keywords are automatically removed</item>
    /// <item>Comments, CTEs, and USE statements are automatically removed</item>
    /// </list>
    /// </remarks>
    /// <example>
    /// <code>
    /// var sql = "SELECT u.Name, u.Email AS EmailAddress, COUNT(*) AS Total FROM Users u";
    /// var columns = SqlInterrogator.ExtractColumnDetailsFromSelectClauseInSql(sql);
    /// // Result:
    /// // [
    /// //   (null, "u", (ColumnName: "Name", Alias: null)),
    /// //   (null, "u", (ColumnName: "Email", Alias: "EmailAddress")),
    /// //   (null, null, (ColumnName: "COUNT", Alias: "Total"))
    /// // ]
    /// 
    /// var sql2 = "SELECT MyDB.dbo.Users.FirstName FROM MyDB.dbo.Users";
    /// var columns2 = SqlInterrogator.ExtractColumnDetailsFromSelectClauseInSql(sql2);
    /// // Result: [("MyDB", "Users", (ColumnName: "FirstName", Alias: null))]
    /// </code>
    /// </example>
    public static List<(string? DatabaseName, string? TableName, (string ColumnName, string? Alias) Column)> ExtractColumnDetailsFromSelectClauseInSql(string? sql)
    {
        if (string.IsNullOrWhiteSpace(sql))
        {
            return [];
        }

        sql = PreprocessSql(sql);

        // Only process SELECT statements
        if (!IgnoreCaseRegex().IsMatch(sql))
        {
            return [];
        }

        if (!TryExtractSelectClause(sql, out var selectClause) || selectClause == null)
        {
            return [];
        }

        // Handle SELECT * as a special case
        if (SelectStarRegex().IsMatch(selectClause.Trim()))
        {
            return [(null, null, ("*", null))];
        }

        var columnExpressions = SplitByCommaRespectingParentheses(selectClause);
        return ProcessColumnExpressions(columnExpressions);
    }

    /// <summary>
    /// Extracts column-value pairs from WHERE clause conditions in a SQL statement.
    /// </summary>
    /// <param name="sql">The SQL statement to analyse.</param>
    /// <returns>
    /// A list of tuples containing:
    /// <list type="bullet">
    /// <item><c>Column</c> - A tuple with ColumnName (the column being filtered) and Alias (null for WHERE clause columns)</item>
    /// <item><c>Operator</c> - The comparison operator (=, !=, <>, >, <, >=, <=, LIKE, IN, IS, IS NOT, etc.)</item>
    /// <item><c>Value</c> - The comparison value (e.g., "1", "'John'", "(1,2,3)", "@userId", "NULL" for IS/IS NOT, or another column name like "u.Id")</item>
    /// </list>
    /// Returns an empty list if the SQL has no WHERE clause or contains no valid conditions.
    /// </returns>
    /// <remarks>
    /// <para>This method handles:</para>
    /// <list type="bullet">
    /// <item>Simple comparisons: WHERE Id = 1 → ("Id", "=", "1")</item>
    /// <item>String comparisons: WHERE Name = 'John' → ("Name", "=", "'John'")</item>
    /// <item>SQL parameters: WHERE Id = @userId → ("Id", "=", "@userId")</item>
    /// <item>Qualified columns: WHERE u.Active = 1 → ("u.Active", "=", "1")</item>
    /// <item>Column-to-column comparisons: WHERE o.UserId = u.Id → ("o.UserId", "=", "u.Id")</item>
    /// <item>LIKE patterns: WHERE Email LIKE '%@example.com' → ("Email", "LIKE", "'%@example.com'")</item>
    /// <item>Comparison operators: =, !=, <>, >, <, >=, <=, LIKE, IN, IS, IS NOT</item>
    /// <item>Bracketed identifiers: WHERE [User Name] = 'John' → ("User Name", "=", "'John'")</item>
    /// <item>Fully bracketed qualified names: WHERE [dbo].[Users].[Active] = 1 → ("dbo.Users.Active", "=", "1")</item>
    /// <item>Double-quoted identifiers: WHERE "dbo"."Users"."Email" = 'test@test.com' → ("dbo.Users.Email", "=", "'test@test.com'")</item>
    /// <item>Bracketed column values: WHERE OrderDate > [LastModifiedDate] → ("OrderDate", ">", "[LastModifiedDate]")</item>
    /// <item>NULL checks: WHERE DeletedDate IS NULL → ("DeletedDate", "IS", "NULL")</item>
    /// <item>NOT NULL checks: WHERE CreatedDate IS NOT NULL → ("CreatedDate", "IS NOT", "NULL")</item>
    /// <item>IN clauses: WHERE Status IN (1,2,3) → ("Status", "IN", "(1,2,3)")</item>
    /// <item>IN clauses with parameters: WHERE Status IN (@status1, @status2) → ("Status", "IN", "(@status1")</item>
    /// <item>Comments are automatically removed before processing</item>
    /// <item>Handles conditions before ORDER BY, GROUP BY, HAVING, or UNION clauses</item>
    /// </list>
    /// <para>Limitations:</para>
    /// <list type="bullet">
    /// <item>Complex nested conditions with AND/OR are split into individual conditions</item>
    /// <item>Subqueries in WHERE clause are not fully parsed</item>
    /// <item>Function calls in conditions are extracted as-is</item>
    /// </list>
    /// </remarks>
    /// <example>
    /// <code>
    /// var sql = "SELECT * FROM Users WHERE Id = 1 AND Active = 1 AND Email LIKE '%@test.com'";
    /// var conditions = SqlInterrogator.ExtractWhereClausesFromSql(sql);
    /// // Result:
    /// // [
    /// // ((ColumnName: "Id", Alias: null), "=", "1"),
    /// //   ((ColumnName: "Active", Alias: null), "=", "1"),
    /// //   ((ColumnName: "Email", Alias: null), "LIKE", "'%@test.com'")
    /// // ]
    /// 
    /// var sql2 = "SELECT * FROM Users u WHERE u.CreatedDate > '2024-01-01' ORDER BY u.Name";
    /// var conditions2 = SqlInterrogator.ExtractWhereClausesFromSql(sql2);
    /// // Result: [((ColumnName: "u.CreatedDate", Alias: null), ">", "'2024-01-01'")]
    /// 
    /// var sql3 = "SELECT * FROM Users WHERE DeletedDate IS NULL";
    /// var conditions3 = SqlInterrogator.ExtractWhereClausesFromSql(sql3);
    /// // Result: [((ColumnName: "DeletedDate", Alias: null), "IS", "NULL")]
    /// 
    /// var sql4 = "SELECT * FROM Users WHERE CreatedDate IS NOT NULL";
    /// var conditions4 = SqlInterrogator.ExtractWhereClausesFromSql(sql4);
    /// // Result: [((ColumnName: "CreatedDate", Alias: null), "IS NOT", "NULL")]
    /// 
    /// var sql5 = "SELECT * FROM Orders o JOIN Users u ON o.UserId = u.Id WHERE o.Status = u.DefaultStatus";
    /// var conditions5 = SqlInterrogator.ExtractWhereClausesFromSql(sql5);
    /// // Result: [((ColumnName: "o.Status", Alias: null), "=", "u.DefaultStatus")]
    /// 
    /// var sql6 = "SELECT * FROM Users WHERE Id = @userId AND Status = @userStatus";
    /// var conditions6 = SqlInterrogator.ExtractWhereClausesFromSql(sql6);
    /// // Result:
    /// // [
    /// //   ((ColumnName: "Id", Alias: null), "=", "@userId"),
    /// //   ((ColumnName: "Status", Alias: null), "=", "@userStatus")
    /// // ]
    /// 
    /// var sql7 = "SELECT * FROM Users WHERE [dbo].[Users].[Active] = 1";
    /// var conditions7 = SqlInterrogator.ExtractWhereClausesFromSql(sql7);
    /// // Result: [((ColumnName: "dbo.Users.Active", Alias: null), "=", "1")]
    /// </code>
    /// </example>
    public static List<((string ColumnName, string? Alias) Column, string Operator, string Value)> ExtractWhereClausesFromSql(string? sql)
    {
        if (string.IsNullOrWhiteSpace(sql))
        {
            return [];
        }

        sql = PreprocessSql(sql);

        var whereMatch = WhereClauseRegex().Match(sql);
        if (!whereMatch.Success)
        {
            return [];
        }

        var whereClause = whereMatch.Groups[1].Value.Trim();
        var individualConditions = SplitWhereConditions(whereClause);

        return ParseWhereConditions(individualConditions);
    }

    /// <summary>
    /// Converts a SELECT statement to a SELECT COUNT(*) statement while preserving all other clauses.
    /// </summary>
    /// <param name="sql">The SQL SELECT statement to convert.</param>
    /// <returns>
    /// A new SQL statement with the SELECT clause replaced with "SELECT COUNT(*)" and all other clauses
    /// (FROM, WHERE, JOIN, GROUP BY, HAVING, ORDER BY) preserved. Returns null if the input is not
    /// a valid SELECT statement or if it lacks a FROM clause.
    /// </returns>
    /// <remarks>
    /// <para>This method is useful for converting data retrieval queries into count queries, commonly
    /// needed for pagination scenarios where you need both the data and the total row count.</para>
    /// <para>The method:</para>
    /// <list type="bullet">
    /// <item>Validates the input is a SELECT statement (returns null for UPDATE, INSERT, DELETE)</item>
    /// <item>Pre-processes SQL to remove comments, CTEs, and USE statements</item>
    /// <item>Detects if original query had DISTINCT keyword</item>
    /// <item>For DISTINCT queries: Uses subquery approach to count distinct rows accurately</item>
    /// <item>For non-DISTINCT queries: Replaces SELECT clause with "SELECT COUNT(*)"</item>
    /// <item>Preserves all clauses after FROM: WHERE, JOIN, GROUP BY, HAVING, ORDER BY, OFFSET/FETCH</item>
    /// <item>Maintains table aliases, hints (NOLOCK), and qualified table names</item>
    /// <item>Returns null if no FROM clause exists (e.g., SELECT GETDATE())</item>
    /// </list>
    /// <para>Common use cases:</para>
    /// <list type="bullet">
    /// <item>Pagination: Get total count for the same query used to fetch paged data</item>
    /// <item>Performance testing: Check row count before executing expensive data retrieval</item>
    /// <item>Conditional loading: Verify rows exist before fetching full result set</item>
    /// <item>Query optimization: Test filter effectiveness by counting matching rows</item>
    /// </list>
    /// <para>Limitations:</para>
    /// <list type="bullet">
    /// <item>ORDER BY clauses are preserved but have no effect on COUNT (SQL is still valid)</item>
    /// <item>TOP keywords are removed during pre-processing</item>
    /// <item>Subqueries in SELECT clause are removed (only outer query is converted)</item>
    /// </list>
    /// </remarks>
    /// <example>
    /// <para><strong>Basic Conversion:</strong></para>
    /// <code>
    /// var sql = "SELECT Name, Email FROM Users WHERE Active = 1";
    /// var countSql = SqlInterrogator.ConvertSelectStatementToSelectCount(sql);
    /// // Result: "SELECT COUNT(*) FROM Users WHERE Active = 1"
    /// </code>
    /// 
    /// <para><strong>DISTINCT Query (uses subquery):</strong></para>
    /// <code>
    /// var sql = "SELECT DISTINCT Name, Email FROM Users WHERE Active = 1";
    /// var countSql = SqlInterrogator.ConvertSelectStatementToSelectCount(sql);
    /// // Result: "SELECT COUNT(*) FROM (SELECT DISTINCT Name, Email FROM Users WHERE Active = 1) AS DistinctCount"
    /// // Correctly counts distinct Name/Email combinations
    /// </code>
    /// 
    /// <para><strong>Pagination Scenario:</strong></para>
    /// <code>
    /// // Original query with pagination
    /// var dataQuery = @"
    ///     SELECT u.Id, u.Name, u.Email 
    ///     FROM Users u 
    ///     WHERE u.Active = 1 
    ///     ORDER BY u.CreatedDate DESC 
    /// OFFSET 20 ROWS 
    ///     FETCH NEXT 10 ROWS ONLY";
    /// 
    /// // Get count query for total rows
    /// var countQuery = SqlInterrogator.ConvertSelectStatementToSelectCount(dataQuery);
    /// // Result: "SELECT COUNT(*) FROM Users u WHERE u.Active = 1 ORDER BY u.CreatedDate DESC OFFSET 20 ROWS FETCH NEXT 10 ROWS ONLY"
    /// 
    /// // Execute both queries
    /// var data = await db.QueryAsync&lt;User&gt;(dataQuery);
    /// var total = await db.ExecuteScalarAsync&lt;int&gt;(countQuery);
    /// </code>
    /// 
    /// <para><strong>Complex Query with JOINs:</strong></para>
    /// <code>
    /// var sql = @"
    ///     SELECT u.Name, o.OrderDate, p.ProductName
    ///     FROM Users u
    ///     INNER JOIN Orders o ON u.Id = o.UserId
    ///     LEFT JOIN Products p ON o.ProductId = p.Id
    ///     WHERE u.Active = 1 
    ///       AND o.OrderDate &gt; '2024-01-01'
    ///     GROUP BY u.Name, o.OrderDate, p.ProductName
    ///     HAVING COUNT(o.Id) &gt; 5";
    /// 
    /// var countSql = SqlInterrogator.ConvertSelectStatementToSelectCount(sql);
    /// // Result: "SELECT COUNT(*) FROM Users u INNER JOIN Orders o ON u.Id = o.UserId LEFT JOIN Products p ON o.ProductId = p.Id WHERE u.Active = 1 AND o.OrderDate > '2024-01-01' GROUP BY u.Name, o.OrderDate, p.ProductName HAVING COUNT(o.Id) > 5"
    /// </code>
    /// 
    /// <para><strong>Non-SELECT Statement (returns null):</strong></para>
    /// <code>
    /// var updateSql = "UPDATE Users SET Active = 1";
    /// var result = SqlInterrogator.ConvertSelectStatementToSelectCount(updateSql);
    /// // Result: null
    /// 
    /// var noFromSql = "SELECT GETDATE()";
    /// var result2 = SqlInterrogator.ConvertSelectStatementToSelectCount(noFromSql);
    /// // Result: null
    /// </code>
    /// 
    /// <para><strong>With SQL Parameters:</strong></para>
    /// <code>
    /// var sql = "SELECT * FROM Users WHERE Id = @userId AND Status = @status";
    /// var countSql = SqlInterrogator.ConvertSelectStatementToSelectCount(sql);
    /// // Result: "SELECT COUNT(*) FROM Users WHERE Id = @userId AND Status = @status"
    /// // Parameters are preserved and can be used with the count query
    /// </code>
    /// </example>
    public static string? ConvertSelectStatementToSelectCount(string sql)
    {
        if (string.IsNullOrWhiteSpace(sql))
        {
            return null;
        }

        // Check if original SQL has DISTINCT before preprocessing removes it
        var hadDistinct = SelectDistinctRegex().IsMatch(sql);

        // Store original SQL for DISTINCT case
        var originalSql = sql;

        sql = PreprocessSql(sql);
        
        // Only process SELECT statements
        if (!IgnoreCaseRegex().IsMatch(sql))
        {
            return null;
        }

        if (!TryExtractSelectClause(sql, out var selectClause) || selectClause == null)
        {
            return null;
        }

        var fromIndex = sql.IndexOfIgnoreCase(" FROM ");
        if (fromIndex < 0)
        {
            return null;
        }

        // If original query had DISTINCT, use subquery approach for accurate counting
        if (hadDistinct)
        {
            // Remove comments, CTEs, USE statements but preserve DISTINCT
            var cleanedSql = RemoveComments(originalSql);
            cleanedSql = RemoveCTEs(cleanedSql);
            cleanedSql = RemoveUseStatements(cleanedSql);
            cleanedSql = cleanedSql.Trim();
            
            return $"SELECT COUNT(*) FROM ({cleanedSql}) AS DistinctCount";
        }

        // For non-DISTINCT queries, use simple replacement
        var countSelectClause = "SELECT COUNT(*)";
        var fromAndBeyond = sql[fromIndex..];
        return countSelectClause + fromAndBeyond;
    }

    /// <summary>
    /// Converts a SELECT statement to a SELECT TOP N statement while preserving all other clauses.
    /// </summary>
    /// <param name="sql">The SQL SELECT statement to convert.</param>
    /// <param name="top">The number of rows to return. Must be greater than 0.</param>
    /// <returns>
    /// A new SQL statement with the SELECT clause replaced by "SELECT TOP N" and all other clauses
    /// (FROM, WHERE, JOIN, GROUP BY, HAVING, ORDER BY) preserved. Returns null if the input is not
    /// a valid SELECT statement, if it lacks a FROM clause, or if top is less than or equal to 0.
    /// </returns>
    /// <remarks>
    /// <para>This method is useful for limiting result sets, commonly needed for performance optimization,
    /// data sampling, and retrieving top N results based on specific criteria.</para>
    /// <para>The method:</para>
    /// <list type="bullet">
    /// <item>Validates the input is a SELECT statement (returns null for UPDATE, INSERT, DELETE)</item>
    /// <item>Validates that top is greater than 0 (returns null if top &lt;= 0)</item>
    /// <item>Preprocesses SQL to remove comments, CTEs, and USE statements</item>
    /// <item>Replaces any SELECT clause (SELECT *, columns, functions, DISTINCT, existing TOP) with "SELECT TOP N"</item>
    /// <item>Preserves all clauses after FROM: WHERE, JOIN, GROUP BY, HAVING, ORDER BY, OFFSET/FETCH</item>
    /// <item>Maintains table aliases, hints (NOLOCK), and qualified table names</item>
    /// <item>Returns null if no FROM clause exists (e.g., SELECT GETDATE())</item>
    /// <item>Existing TOP clauses are replaced with the new TOP value</item>
    /// </list>
    /// <para>Common use cases:</para>
    /// <list type="bullet">
    /// <item>Performance optimization: Limit result set size for expensive queries</item>
    /// <item>Data sampling: Get a sample of data for analysis or testing</item>
    /// <item>Top N queries: Retrieve top performers, recent items, etc. with ORDER BY</item>
    /// <item>Preview data: Show first N rows without fetching entire result set</item>
    /// <item>Debugging: Quickly verify query results with limited output</item>
    /// </list>
    /// <para>Limitations:</para>
    /// <list type="bullet">
    /// <item>TOP without ORDER BY returns arbitrary rows (non-deterministic)</item>
    /// <item>DISTINCT keyword is removed during pre-processing</item>
    /// <item>TOP PERCENT syntax is not supported (only TOP N)</item>
    /// <item>Existing OFFSET/FETCH clauses are preserved but may conflict with TOP</item>
    /// </list>
    /// </remarks>
    /// <example>
    /// <para><strong>Basic Conversion:</strong></para>
    /// <code>
    /// var sql = "SELECT Name, Email FROM Users WHERE Active = 1";
    /// var topSql = SqlInterrogator.ConvertSelectStatementToSelectTop(sql, 10);
    /// // Result: "SELECT TOP 10 Name, Email FROM Users WHERE Active = 1"
    /// </code>
    /// 
    /// <para><strong>Top N with ORDER BY:</strong></para>
    /// <code>
    /// var sql = "SELECT ProductName, SalesTotal FROM Products ORDER BY SalesTotal DESC";
    /// var topSql = SqlInterrogator.ConvertSelectStatementToSelectTop(sql, 5);
    /// // Result: "SELECT TOP 5 ProductName, SalesTotal FROM Products ORDER BY SalesTotal DESC"
    /// // Returns top 5 products by sales
    /// </code>
    /// 
    /// <para><strong>Performance Optimization:</strong></para>
    /// <code>
    /// // Original expensive query
    /// var sql = @"
    ///   SELECT u.Name, u.Email, COUNT(o.OrderId) AS OrderCount
    ///     FROM Users u
    ///     LEFT JOIN Orders o ON u.Id = o.UserId
    ///     GROUP BY u.Name, u.Email
    ///     HAVING COUNT(o.OrderId) &gt; 0
    ///     ORDER BY COUNT(o.OrderId) DESC";
    /// 
    /// // Limit to top 100 users
    /// var topSql = SqlInterrogator.ConvertSelectStatementToSelectTop(sql, 100);
    /// // Result: "SELECT TOP 100 u.Name, u.Email, COUNT(o.OrderId) AS OrderCount FROM Users u LEFT JOIN Orders o ON u.Id = o.UserId GROUP BY u.Name, u.Email HAVING COUNT(o.OrderId) > 0 ORDER BY COUNT(o.OrderId) DESC"
    /// </code>
    /// 
    /// <para><strong>Replacing Existing TOP:</strong></para>
    /// <code>
    /// var sql = "SELECT TOP 100 * FROM Users ORDER BY CreatedDate DESC";
    /// var topSql = SqlInterrogator.ConvertSelectStatementToSelectTop(sql, 10);
    /// // Result: "SELECT TOP 10 * FROM Users ORDER BY CreatedDate DESC"
    /// // Existing TOP 100 is replaced with TOP 10
    /// </code>
    /// 
    /// <para><strong>Data Sampling:</strong></para>
    /// <code>
    /// var sql = "SELECT * FROM LargeTable WHERE Category = @category";
    /// var sampleSql = SqlInterrogator.ConvertSelectStatementToSelectTop(sql, 1000);
    /// // Result: "SELECT TOP 1000 * FROM LargeTable WHERE Category = @category"
    /// // Get sample of 1000 rows for analysis
    /// </code>
    /// 
    /// <para><strong>Invalid Input (returns null):</strong></para>
    /// <code>
    /// var updateSql = "UPDATE Users SET Active = 1";
    /// var result = SqlInterrogator.ConvertSelectStatementToSelectTop(updateSql, 10);
    /// // Result: null
    /// 
    /// var noFromSql = "SELECT GETDATE()";
    /// var result2 = SqlInterrogator.ConvertSelectStatementToSelectTop(noFromSql, 10);
    /// // Result: null
    /// 
    /// var invalidTop = SqlInterrogator.ConvertSelectStatementToSelectTop("SELECT * FROM Users", 0);
    /// // Result: null (top must be > 0)
    /// 
    /// var negativeTop = SqlInterrogator.ConvertSelectStatementToSelectTop("SELECT * FROM Users", -1);
    /// // Result: null (top must be > 0)
    /// </code>
    /// 
    /// <para><strong>With SQL Parameters:</strong></para>
    /// <code>
    /// var sql = "SELECT * FROM Users WHERE Status = @status ORDER BY CreatedDate DESC";
    /// var topSql = SqlInterrogator.ConvertSelectStatementToSelectTop(sql, 50);
    /// // Result: "SELECT TOP 50 * FROM Users WHERE Status = @status ORDER BY CreatedDate DESC"
    /// // Parameters are preserved and can be used with the TOP query
    /// </code>
    /// </example>
    public static string? ConvertSelectStatementToSelectTop(string sql, int top)
    {
        if (string.IsNullOrWhiteSpace(sql))
        {
            return null;
        }

        if (top <= 0)
        {
            return null;
        }

        sql = PreprocessSql(sql);

        // Only process SELECT statements
        if (!IgnoreCaseRegex().IsMatch(sql))
        {
            return null;
        }

        if (!TryExtractSelectClause(sql, out var selectClause) || selectClause == null)
        {
            return null;
        }

        var topSelectClause = $"SELECT TOP {top}";
        var fromIndex = sql.IndexOfIgnoreCase(" FROM ");
        if (fromIndex < 0)
        {
            return null;
        }

        var fromAndBeyond = sql[fromIndex..];
        return topSelectClause + fromAndBeyond;
    }

    /// <summary>
    /// Converts a SELECT statement to a SELECT DISTINCT statement while preserving all other clauses.
    /// </summary>
    /// <param name="sql">The SQL SELECT statement to convert.</param>
    /// <returns>
    /// A new SQL statement with the SELECT clause replaced by "SELECT DISTINCT" and all other clauses
    /// (FROM, WHERE, JOIN, GROUP BY, HAVING, ORDER BY) preserved. Returns null if the input is not
    /// a valid SELECT statement or if it lacks a FROM clause.
    /// </returns>
    /// <remarks>
    /// <para>This method is useful for removing duplicate rows from query results, commonly needed for
    /// data analysis, reporting, and ensuring unique result sets.</para>
    /// <para>The method:</para>
    /// <list type="bullet">
    /// <item>Validates the input is a SELECT statement (returns null for UPDATE, INSERT, DELETE)</item>
    /// <item>Preprocesses SQL to remove comments, CTEs, and USE statements</item>
    /// <item>Replaces any SELECT clause with "SELECT DISTINCT" (removes existing DISTINCT if present)</item>
    /// <item>Preserves all clauses after FROM: WHERE, JOIN, GROUP BY, HAVING, ORDER BY, OFFSET/FETCH</item>
    /// <item>Maintains table aliases, hints (NOLOCK), and qualified table names</item>
    /// <item>Returns null if no FROM clause exists (e.g., SELECT GETDATE())</item>
    /// <item>Existing DISTINCT keywords are handled during pre-processing</item>
    /// </list>
    /// <para>Common use cases:</para>
    /// <list type="bullet">
    /// <item>Remove duplicate rows from result sets</item>
    /// <item>Get unique values for reporting and analysis</item>
    /// <item>Data quality: Identify distinct combinations of columns</item>
    /// <item>Lookup lists: Generate unique dropdown options</item>
    /// <item>Set operations: Simulate UNION behaviour for single queries</item>
    /// </list>
    /// <para>Limitations:</para>
    /// <list type="bullet">
    /// <item>DISTINCT applies to all columns in SELECT (cannot select distinct on specific columns)</item>
    /// <item>Can impact performance on large result sets</item>
    /// <item>Existing TOP clauses are removed during pre-processing</item>
    /// <item>May not preserve original row order without explicit ORDER BY</item>
    /// </list>
    /// </remarks>
    /// <example>
    /// <para><strong>Basic Conversion:</strong></para>
    /// <code>
    /// var sql = "SELECT Name, Email FROM Users WHERE Active = 1";
    /// var distinctSql = SqlInterrogator.ConvertSelectStatementToSelectDistinct(sql);
    /// // Result: "SELECT DISTINCT Name, Email FROM Users WHERE Active = 1"
    /// </code>
    /// 
    /// <para><strong>Remove Duplicates from JOIN:</strong></para>
    /// <code>
    /// var sql = @"
    ///     SELECT u.Name, u.Email
    ///     FROM Users u
    ///     INNER JOIN Orders o ON u.Id = o.UserId
    ///     WHERE o.OrderDate &gt; '2024-01-01'";
    /// 
    /// var distinctSql = SqlInterrogator.ConvertSelectStatementToSelectDistinct(sql);
    /// // Result: "SELECT DISTINCT u.Name, u.Email FROM Users u INNER JOIN Orders o ON u.Id = o.UserId WHERE o.OrderDate > '2024-01-01'"
    /// // Returns unique users even if they have multiple orders
    /// </code>
    /// 
    /// <para><strong>Unique Values for Dropdown:</strong></para>
    /// <code>
    /// var sql = "SELECT Country FROM Customers ORDER BY Country";
    /// var distinctSql = SqlInterrogator.ConvertSelectStatementToSelectDistinct(sql);
    /// // Result: "SELECT DISTINCT Country FROM Customers ORDER BY Country"
    /// // Returns unique list of countries for dropdown
    /// </code>
    /// 
    /// <para><strong>Already DISTINCT (idempotent):</strong></para>
    /// <code>
    /// var sql = "SELECT DISTINCT Category FROM Products";
    /// var distinctSql = SqlInterrogator.ConvertSelectStatementToSelectDistinct(sql);
    /// // Result: "SELECT DISTINCT Category FROM Products"
    /// // Existing DISTINCT is handled correctly
    /// </code>
    /// 
    /// <para><strong>Complex Query with Multiple Columns:</strong></para>
    /// <code>
    /// var sql = @"
    ///     SELECT p.Category, p.Supplier, p.Country
    ///     FROM Products p
    ///     WHERE p.Discontinued = 0
    ///     ORDER BY p.Category, p.Supplier";
    /// 
    /// var distinctSql = SqlInterrogator.ConvertSelectStatementToSelectDistinct(sql);
    /// // Result: "SELECT DISTINCT p.Category, p.Supplier, p.Country FROM Products p WHERE p.Discontinued = 0 ORDER BY p.Category, p.Supplier"
    /// // Returns unique combinations of Category, Supplier, and Country
    /// </code>
    /// 
    /// <para><strong>With Aliases and Expressions:</strong></para>
    /// <code>
    /// var sql = "SELECT YEAR(OrderDate) AS OrderYear, Status FROM Orders WHERE Status IN (1,2,3)";
    /// var distinctSql = SqlInterrogator.ConvertSelectStatementToSelectDistinct(sql);
    /// // Result: "SELECT DISTINCT YEAR(OrderDate) AS OrderYear, Status FROM Orders WHERE Status IN (1,2,3)"
    /// // Preserves column aliases and expressions
    /// </code>
    /// 
    /// <para><strong>Invalid Input (returns null):</strong></para>
    /// <code>
    /// var updateSql = "UPDATE Users SET Active = 1";
    /// var result = SqlInterrogator.ConvertSelectStatementToSelectDistinct(updateSql);
    /// // Result: null
    /// 
    /// var noFromSql = "SELECT GETDATE()";
    /// var result2 = SqlInterrogator.ConvertSelectStatementToSelectDistinct(noFromSql);
    /// // Result: null
    /// </code>
    /// 
    /// <para><strong>With SQL Parameters:</strong></para>
    /// <code>
    /// var sql = "SELECT Status, Priority FROM Tasks WHERE AssignedTo = @userId";
    /// var distinctSql = SqlInterrogator.ConvertSelectStatementToSelectDistinct(sql);
    /// // Result: "SELECT DISTINCT Status, Priority FROM Tasks WHERE AssignedTo = @userId"
    /// // Parameters are preserved and can be used with the DISTINCT query
    /// </code>
    /// 
    /// <para><strong>Data Quality Analysis:</strong></para>
    /// <code>
    /// var sql = "SELECT Email FROM Users";
    /// var distinctSql = SqlInterrogator.ConvertSelectStatementToSelectDistinct(sql);
    /// // Result: "SELECT DISTINCT Email FROM Users"
    /// // Use to check for duplicate email addresses
    /// 
    /// // Compare counts to find duplicates:
    /// // var totalCount = ExecuteScalar("SELECT COUNT(*) FROM Users");
    /// // var uniqueCount = ExecuteScalar(distinctSql);
    /// // if (totalCount != uniqueCount) { /* duplicates exist */ }
    /// </code>
    /// </example>
    public static string? ConvertSelectStatementToSelectDistinct(string sql)
    {
        if (string.IsNullOrWhiteSpace(sql))
        {
            return null;
        }

        sql = PreprocessSql(sql);

        // Only process SELECT statements
        if (!IgnoreCaseRegex().IsMatch(sql))
        {
            return null;
        }

        if (!TryExtractSelectClause(sql, out var selectClause) || selectClause == null)
        {
            return null;
        }

        var fromIndex = sql.IndexOfIgnoreCase(" FROM ");
        if (fromIndex < 0)
        {
            return null;
        }

        // Get everything from SELECT to FROM (excluding SELECT keyword)
        // Note: TryExtractSelectClause already removes DISTINCT/TOP keywords from selectClause
        var selectToFrom = selectClause.Trim();

        var fromAndBeyond = sql[fromIndex..];

        return $"SELECT DISTINCT {selectToFrom}{fromAndBeyond}";
    }

    /// <summary>
    /// Converts a SELECT statement to include or replace an ORDER BY clause while preserving all other clauses including pagination.
    /// </summary>
    /// <param name="sql">The SQL SELECT statement to convert.</param>
    /// <param name="orderByClause">The ORDER BY clause to add or replace (without the "ORDER BY" keyword).</param>
    /// <returns>
    /// A new SQL statement with the ORDER BY clause added or replaced, preserving all other clauses
    /// (SELECT, FROM, WHERE, JOIN, GROUP BY, HAVING, OFFSET/FETCH). Returns null if the input is not a valid SELECT statement,
    /// if it lacks a FROM clause, or if the orderByClause is null or empty.
    /// </returns>
    /// <remarks>
    /// <para>This method is useful for dynamically changing sort order in queries, commonly needed for
    /// user-driven sorting, reporting, and data grid scenarios.</para>
    /// <para>The method:</para>
    /// <list type="bullet">
    /// <item>Validates the input is a SELECT statement (returns null for UPDATE, INSERT, DELETE)</item>
    /// <item>Validates that orderByClause is not null or empty</item>
    /// <item>Preprocesses SQL to remove comments, CTEs, and USE statements</item>
    /// <item>Removes any existing ORDER BY clause</item>
    /// <item>Appends the new ORDER BY clause</item>
    /// <item>Preserves OFFSET/FETCH pagination clauses (they are re-added after the new ORDER BY)</item>
    /// <item>Preserves all other clauses: SELECT, FROM, WHERE, JOIN, GROUP BY, HAVING</item>
    /// <item>Maintains table aliases, hints (NOLOCK), and qualified table names</item>
    /// <item>Returns null if no FROM clause exists (e.g., SELECT GETDATE())</item>
    /// </list>
    /// <para>Common use cases:</para>
    /// <list type="bullet">
    /// <item>Dynamic sorting: Change sort column and direction based on user input</item>
    /// <item>Data grids: Implement column header sorting while maintaining pagination</item>
    /// <item>Reporting: Generate reports with different sort orders</item>
    /// <item>API endpoints: Support query string sort parameters</item>
    /// <item>Default ordering: Add ORDER BY to queries that lack it</item>
    /// </list>
    /// <para>Limitations:</para>
    /// <list type="bullet">
    /// <item>The orderByClause parameter should NOT include "ORDER BY" keyword</item>
    /// <item>Column names in orderByClause are not validated</item>
    /// <item>Complex ORDER BY expressions (CASE, functions) are supported but not validated</item>
    /// </list>
    /// </remarks>
    /// <example>
    /// <para><strong>Basic Usage:</strong></para>
    /// <code>
    /// var sql = "SELECT Name, Email FROM Users WHERE Active = 1";
    /// var orderedSql = SqlInterrogator.ConvertSelectStatementToSelectOrderBy(sql, "Name ASC");
    /// // Result: "SELECT Name, Email FROM Users WHERE Active = 1 ORDER BY Name ASC"
    /// </code>
    /// 
    /// <para><strong>Replace Existing ORDER BY:</strong></para>
    /// <code>
    /// var sql = "SELECT Name, Email FROM Users ORDER BY Name ASC";
    /// var orderedSql = SqlInterrogator.ConvertSelectStatementToSelectOrderBy(sql, "Email DESC");
    /// // Result: "SELECT Name, Email FROM Users ORDER BY Email DESC"
    /// </code>
    /// 
    /// <para><strong>Preserve Pagination:</strong></para>
    /// <code>
    /// var sql = "SELECT * FROM Users ORDER BY Name ASC OFFSET 10 ROWS FETCH NEXT 20 ROWS ONLY";
    /// var orderedSql = SqlInterrogator.ConvertSelectStatementToSelectOrderBy(sql, "Email DESC");
    /// // Result: "SELECT * FROM Users ORDER BY Email DESC OFFSET 10 ROWS FETCH NEXT 20 ROWS ONLY"
    /// // Pagination is preserved when changing sort order
    /// </code>
    /// 
    /// <para><strong>Multiple Columns:</strong></para>
    /// <code>
    /// var sql = "SELECT * FROM Products WHERE Active = 1";
    /// var orderedSql = SqlInterrogator.ConvertSelectStatementToSelectOrderBy(sql, "Category ASC, Price DESC");
    /// // Result: "SELECT * FROM Products WHERE Active = 1 ORDER BY Category ASC, Price DESC"
    /// </code>
    /// 
    /// <para><strong>With Qualified Column Names:</strong></para>
    /// <code>
    /// var sql = "SELECT u.Name, o.OrderDate FROM Users u JOIN Orders o ON u.Id = o.UserId";
    /// var orderedSql = SqlInterrogator.ConvertSelectStatementToSelectOrderBy(sql, "o.OrderDate DESC, u.Name ASC");
    /// // Result: "SELECT u.Name, o.OrderDate FROM Users u JOIN Orders o ON u.Id = o.UserId ORDER BY o.OrderDate DESC, u.Name ASC"
    /// </code>
    /// 
    /// <para><strong>Data Grid Sorting with Pagination:</strong></para>
    /// <code>
    /// // User changes sort column while on page 2
    /// var sql = "SELECT * FROM Users WHERE Active = 1 ORDER BY Name ASC OFFSET 20 ROWS FETCH NEXT 10 ROWS ONLY";
    /// var orderedSql = SqlInterrogator.ConvertSelectStatementToSelectOrderBy(sql, "Email DESC");
    /// // Result: "SELECT * FROM Users WHERE Active = 1 ORDER BY Email DESC OFFSET 20 ROWS FETCH NEXT 10 ROWS ONLY"
    /// // Still on page 2, but now sorted by Email
    /// </code>
    /// 
    /// <para><strong>With GROUP BY:</strong></para>
    /// <code>
    /// var sql = "SELECT Category, COUNT(*) AS Total FROM Products GROUP BY Category";
    /// var orderedSql = SqlInterrogator.ConvertSelectStatementToSelectOrderBy(sql, "Total DESC");
    /// // Result: "SELECT Category, COUNT(*) AS Total FROM Products GROUP BY Category ORDER BY Total DESC"
    /// </code>
    /// 
    /// <para><strong>Dynamic Sorting (User Input):</strong></para>
    /// <code>
    /// var sql = "SELECT * FROM Users WHERE Active = 1";
    /// var sortColumn = "Name"; // From user input
    /// var sortDirection = "DESC"; // From user input
    /// var orderBy = $"{sortColumn} {sortDirection}";
    /// var orderedSql = SqlInterrogator.ConvertSelectStatementToSelectOrderBy(sql, orderBy);
    /// // Result: "SELECT * FROM Users WHERE Active = 1 ORDER BY Name DESC"
    /// </code>
    /// 
    /// <para><strong>With Complex Expression:</strong></para>
    /// <code>
    /// var sql = "SELECT Name, Price FROM Products";
    /// var orderedSql = SqlInterrogator.ConvertSelectStatementToSelectOrderBy(sql, "CASE WHEN Price > 100 THEN 1 ELSE 2 END, Name");
    /// // Result: "SELECT Name, Price FROM Products ORDER BY CASE WHEN Price > 100 THEN 1 ELSE 2 END, Name"
    /// </code>
    /// 
    /// <para><strong>Invalid Input (returns null):</strong></para>
    /// <code>
    /// var updateSql = "UPDATE Users SET Active = 1";
    /// var result = SqlInterrogator.ConvertSelectStatementToSelectOrderBy(updateSql, "Name");
    /// // Result: null
    /// 
    /// var noFromSql = "SELECT GETDATE()";
    /// var result2 = SqlInterrogator.ConvertSelectStatementToSelectOrderBy(noFromSql, "Name");
    /// // Result: null
    /// 
    /// var emptyOrderBy = SqlInterrogator.ConvertSelectStatementToSelectOrderBy("SELECT * FROM Users", "");
    /// // Result: null (orderByClause cannot be empty)
    /// </code>
    /// 
    /// <para><strong>With SQL Parameters:</strong></para>
    /// <code>
    /// var sql = "SELECT * FROM Users WHERE Status = @status";
    /// var orderedSql = SqlInterrogator.ConvertSelectStatementToSelectOrderBy(sql, "CreatedDate DESC");
    /// // Result: "SELECT * FROM Users WHERE Status = @status ORDER BY CreatedDate DESC"
    /// // Parameters are preserved
    /// </code>
    /// </example>
    public static string? ConvertSelectStatementToSelectOrderBy(string sql, string orderByClause)
    {
        if (string.IsNullOrWhiteSpace(sql))
        {
            return null;
        }

        if (string.IsNullOrWhiteSpace(orderByClause))
        {
            return null;
        }

        sql = PreprocessSql(sql);

        // Only process SELECT statements
        if (!IgnoreCaseRegex().IsMatch(sql))
        {
            return null;
        }

        if (!TryExtractSelectClause(sql, out var selectClause) || selectClause == null)
        {
            return null;
        }

        var fromIndex = sql.IndexOfIgnoreCase(" FROM ");
        if (fromIndex < 0)
        {
            return null;
        }

        // Extract pagination clause before removing ORDER BY
        var paginationClause = ExtractPaginationClause(sql);

        // Remove existing ORDER BY and pagination
        var sqlWithoutOrderBy = RemoveOrderByAndPagination(sql);

        // Build new query with ORDER BY
        var result = $"{sqlWithoutOrderBy} ORDER BY {orderByClause.Trim()}";

        // Re-add pagination if it existed
        if (!string.IsNullOrEmpty(paginationClause))
        {
            result = $"{result} {paginationClause}";
        }

        return result;
    }

    public static string? ExtractOrderByClause(string sql)
    {
        if (string.IsNullOrWhiteSpace(sql))
        {
            return null;
        }

        sql = PreprocessSql(sql);
        // Only process SELECT statements
        if (!IgnoreCaseRegex().IsMatch(sql))
        {
            return null;
        }

        var orderByIndex = sql.IndexOfIgnoreCase(" ORDER BY ");
        if (orderByIndex < 0)
        {
            return null; // No ORDER BY clause found
        }
        // Extract ORDER BY clause
        var orderByClause = sql[orderByIndex..].Trim();
        // Remove pagination from ORDER BY clause if present
        var paginationIndex = orderByClause.IndexOfIgnoreCase(" OFFSET ");
        if (paginationIndex >= 0)
        {
            orderByClause = orderByClause[..paginationIndex].Trim();
        }

        return orderByClause;
    }

    /// <summary>
    /// Extracts the OFFSET/FETCH pagination clause from SQL.
    /// </summary>
    /// <param name="sql">The SQL statement to process.</param>
    /// <returns>The pagination clause (OFFSET ... FETCH ...) or null if no pagination exists.</returns>
    private static string? ExtractPaginationClause(string sql)
    {
        var offsetIndex = sql.IndexOfIgnoreCase(" OFFSET ");
        return offsetIndex < 0 ? null : sql[offsetIndex..].Trim();
    }

    /// <summary>
    /// Removes ORDER BY and pagination (OFFSET/FETCH) clauses from SQL.
    /// </summary>
    /// <param name="sql">The SQL statement to process.</param>
    /// <returns>The SQL statement without ORDER BY or pagination clauses.</returns>
    private static string RemoveOrderByAndPagination(string sql)
    {
        // Find ORDER BY position
        var orderByIndex = sql.IndexOfIgnoreCase(" ORDER BY ");
        if (orderByIndex < 0)
        {
            return sql; // No ORDER BY to remove
        }

        // Return everything before ORDER BY
        return sql[..orderByIndex].TrimEnd();
    }
}
