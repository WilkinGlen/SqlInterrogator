namespace SqlInterrogatorService;

using System.Text.RegularExpressions;

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
    public static List<string> ExtractDatabaseNamesFromSql(string sql)
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

        // Pattern array ordered from most specific to least specific
        // Each pattern captures database names from different identifier formats
        var patterns = GetDatabasePatterns();

        // Iterate through patterns and extract database names from matches
        foreach (var pattern in patterns)
        {
            var matches = Regex.Matches(sql, pattern, RegexOptions.IgnoreCase | RegexOptions.Multiline);
            foreach (Match match in matches)
            {
                // Skip if this looks like part of a four-part identifier
                // Check if there's another identifier before our match
                var matchStart = match.Index;
                if (matchStart > 0)
                {
                    // Look backwards for a potential fourth part
                    var beforeMatch = sql.Substring(Math.Max(0, matchStart - 50), Math.Min(50, matchStart));
                    // If we find a pattern like "word." or "[word]." right before FROM/JOIN/etc, skip
                    if (FourPartIdentifierPrefixRegex().IsMatch(beforeMatch))
                    {
                        // This might be part of a four-part identifier, but only skip if the current match is three-part
                        // Count dots in the matched pattern to determine if it's three-part
                        var dotCount = match.Value.Count(c => c == '.');
                        if (dotCount == 2) // Three-part identifier (db.schema.table has 2 dots)
                        {
                            continue; // Skip this match as it's likely part of a four-part
                        }
                    }
                }

                var databaseName = ExtractDatabaseNameFromMatch(match);
                if (!string.IsNullOrWhiteSpace(databaseName))
                {
                    _ = databaseNames.Add(databaseName);
                }
            }
        }

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
    public static string? ExtractFirstTableNameFromSelectClauseInSql(string sql)
    {
        if (string.IsNullOrWhiteSpace(sql))
        {
            return null;
        }

        // Pre-process SQL: remove comments, CTEs, and USE statements
        sql = RemoveComments(sql);
        sql = RemoveCTEs(sql);
        sql = RemoveUseStatements(sql);

        // Only process SELECT statements
        if (!IgnoreCaseRegex().IsMatch(sql))
        {
            return null;
        }

        // Patterns ordered from most specific to least specific
        // This ensures four-part names are matched before three-part, etc.
        var patterns = GetTableNamePatterns();

      // Try each pattern in order until a match is found
        foreach (var pattern in patterns)
        {
       var match = Regex.Match(sql, pattern, RegexOptions.IgnoreCase | RegexOptions.Multiline);
  if (match.Success)
        {
         // Extract the last non-empty group (the table name)
       // Groups are ordered: [server].[database].[schema].[table], so we want the last one
      for (var i = match.Groups.Count - 1; i >= 1; i--)
       {
          if (match.Groups[i].Success && !string.IsNullOrWhiteSpace(match.Groups[i].Value))
       {
    var tableName = match.Groups[i].Value;
            // Filter out table-valued functions (names containing parentheses)
    // Check the original matched text for parentheses after the table name
    var matchedText = match.Value;
       var tableNameIndex = matchedText.LastIndexOf(tableName, StringComparison.OrdinalIgnoreCase);
   if (tableNameIndex >= 0)
          {
        var afterTableName = matchedText[(tableNameIndex + tableName.Length)..].TrimStart();
          if (afterTableName.StartsWith('('))
         {
        // This is a table-valued function, skip it
              continue;
        }
               }

      return tableName;
     }
            }
            }
        }

        return null;
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
    public static List<(string? DatabaseName, string? TableName, (string ColumnName, string? Alias) Column)> ExtractColumnDetailsFromSelectClauseInSql(string sql)
    {
        var columns = new List<(string? DatabaseName, string? TableName, (string ColumnName, string? Alias) Column)>();
        if (string.IsNullOrWhiteSpace(sql))
        {
            return columns;
        }

        // Pre-process SQL: remove comments, CTEs, and USE statements
        sql = RemoveComments(sql);
        sql = RemoveCTEs(sql);
        sql = RemoveUseStatements(sql);

        // Only process SELECT statements
        if (!IgnoreCaseRegex().IsMatch(sql))
        {
            return columns;
        }

        // Extract the SELECT clause (between SELECT and FROM keywords)
        var selectClauseMatch = SelectClauseRegex().Match(sql);
        if (!selectClauseMatch.Success)
        {
            return columns;
        }

        var selectClause = selectClauseMatch.Groups[1].Value;

        // Remove DISTINCT, ALL, TOP keywords as they don't affect column extraction
        selectClause = DistinctTopRegex().Replace(selectClause, "");

        // Handle SELECT * as a special case
        if (SelectStarRegex().IsMatch(selectClause.Trim()))
        {
            columns.Add((null, null, ("*", null)));
            return columns;
        }

        // Split by comma, but not within parentheses (for functions like CONCAT, SUBSTRING, etc.)
        var columnExpressions = SplitByCommaRespectingParentheses(selectClause);

        // Process each column expression
        foreach (var expr in columnExpressions)
        {
            var trimmedExpr = expr.Trim();
            if (string.IsNullOrWhiteSpace(trimmedExpr))
            {
                continue;
            }

            // Extract column details from the expression
            var columnDetail = ExtractColumnDetailFromExpression(trimmedExpr);
            if (columnDetail.HasValue)
            {
                columns.Add(columnDetail.Value);
            }
        }

        return columns;
    }

    /// <summary>
    /// Extracts column-value pairs from WHERE clause conditions in a SQL statement.
    /// </summary>
    /// <param name="sql">The SQL statement to analyze.</param>
    /// <returns>
    /// A list of tuples containing:
    /// <list type="bullet">
    /// <item><c>Column</c> - A tuple with ColumnName (the column being filtered) and Alias (null for WHERE clause columns)</item>
    /// <item><c>Operator</c> - The comparison operator (=, !=, <>, >, <, >=, <=, LIKE, IN, IS, IS NOT, etc.)</item>
    /// <item><c>Value</c> - The comparison value (e.g., "1", "'John'", "(1,2,3)", "NULL" for IS/IS NOT, or another column name like "u.Id")</item>
    /// </list>
    /// Returns an empty list if the SQL has no WHERE clause or contains no valid conditions.
    /// </returns>
    /// <remarks>
    /// <para>This method handles:</para>
    /// <list type="bullet">
    /// <item>Simple comparisons: WHERE Id = 1 → ("Id", "=", "1")</item>
    /// <item>String comparisons: WHERE Name = 'John' → ("Name", "=", "'John'")</item>
    /// <item>Qualified columns: WHERE u.Active = 1 → ("u.Active", "=", "1")</item>
    /// <item>Column-to-column comparisons: WHERE o.UserId = u.Id → ("o.UserId", "=", "u.Id")</item>
    /// <item>LIKE patterns: WHERE Email LIKE '%@example.com' → ("Email", "LIKE", "'%@example.com'")</item>
    /// <item>Comparison operators: =, !=, <>, >, <, >=, <=, LIKE, IN, IS, IS NOT</item>
    /// <item>Bracketed identifiers: WHERE [User Name] = 'John' → ("User Name", "=", "'John'")</item>
    /// <item>Bracketed column values: WHERE OrderDate > [LastModifiedDate] → ("OrderDate", ">", "[LastModifiedDate]")</item>
    /// <item>NULL checks: WHERE DeletedDate IS NULL → ("DeletedDate", "IS", "NULL")</item>
    /// <item>NOT NULL checks: WHERE CreatedDate IS NOT NULL → ("CreatedDate", "IS NOT", "NULL")</item>
    /// <item>IN clauses: WHERE Status IN (1,2,3) → ("Status", "IN", "(1,2,3)")</item>
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
    /// //   ((ColumnName: "Id", Alias: null), "=", "1"),
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
    /// </code>
    /// </example>
    public static List<((string ColumnName, string? Alias) Column, string Operator, string Value)> ExtractWhereClausesFromSql(string sql)
    {
        var conditions = new List<((string ColumnName, string? Alias) Column, string Operator, string Value)>();

        if (string.IsNullOrWhiteSpace(sql))
        {
            return conditions;
        }

        // Pre-process SQL: remove comments, CTEs, and USE statements
        sql = RemoveComments(sql);
        sql = RemoveCTEs(sql);
        sql = RemoveUseStatements(sql);

        // Extract WHERE clause
        var whereMatch = WhereClauseRegex().Match(sql);
        if (!whereMatch.Success)
        {
            return conditions;
        }

        var whereClause = whereMatch.Groups[1].Value.Trim();

        // Split by AND/OR while respecting parentheses
        var individualConditions = SplitWhereConditions(whereClause);

        // Extract column-operator-value triplets from each condition
        foreach (var condition in individualConditions)
        {
            var trimmedCondition = condition.Trim();
            if (string.IsNullOrWhiteSpace(trimmedCondition))
            {
                continue;
            }

            var conditionMatch = WhereConditionRegex().Match(trimmedCondition);
            if (conditionMatch.Success)
            {
                var columnName = conditionMatch.Groups[1].Value.Trim().Trim('[', ']', '"');
                var operatorPart = conditionMatch.Groups[2].Value.Trim();
                var valuePart = conditionMatch.Groups.Count > 3 && conditionMatch.Groups[3].Success
                ? conditionMatch.Groups[3].Value.Trim()
                 : string.Empty;

                conditions.Add(((columnName, null), operatorPart, valuePart));
            }
        }

        return conditions;
    }
}
