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
    #region Constants

    // Regex group indices for clarity and maintainability
    private const int DatabaseGroupIndex = 1;
    private const int SecondaryDatabaseGroupIndex = 2;
    private const int SchemaGroupIndex = 2;
    private const int TableGroupIndex = 3;
    private const int ColumnGroupIndex = 4;
    private const int FifthPartGroupIndex = 5;

    // Estimated average columns in SELECT statement for StringBuilder capacity
    private const int EstimatedColumnsPerQuery = 4;

    #endregion

    #region Generated Regex Patterns

    // These regex patterns are generated at compile-time using the [GeneratedRegex] attribute
    // for optimal performance. The source generator creates the implementation automatically.

    /// <summary>Matches single-line SQL comments (-- comment).</summary>
    [GeneratedRegex(@"--[^\r\n]*", RegexOptions.Multiline)]
    private static partial Regex MultilineRegex();

    /// <summary>Matches multi-line SQL comments (/* comment */).</summary>
    [GeneratedRegex(@"/\*.*?\*/", RegexOptions.Singleline)]
    private static partial Regex SingleLineRegex();

    /// <summary>Matches SELECT keyword at the start of a statement.</summary>
    [GeneratedRegex(@"^\s*SELECT\s+", RegexOptions.IgnoreCase, "en-GB")]
    private static partial Regex IgnoreCaseRegex();

    /// <summary>Matches USE statement followed by GO on a new line.</summary>
    [GeneratedRegex(@"USE\s+\[?[\w\s]+\]?\s*;?\s*\r?\n\s*GO\s*", RegexOptions.IgnoreCase | RegexOptions.Multiline)]
    private static partial Regex UseWithGoRegex();

    /// <summary>Matches USE statement with optional semicolon.</summary>
    [GeneratedRegex(@"USE\s+\[?[\w\s]+\]?\s*;?\s*", RegexOptions.IgnoreCase)]
    private static partial Regex UseStatementRegex();

    /// <summary>Matches standalone GO batch separator.</summary>
    [GeneratedRegex(@"^\s*GO\s*$", RegexOptions.IgnoreCase | RegexOptions.Multiline)]
    private static partial Regex StandaloneGoRegex();

    /// <summary>Extracts the SELECT clause between SELECT and FROM keywords.</summary>
    [GeneratedRegex(@"SELECT\s+(.*?)\s+FROM", RegexOptions.IgnoreCase | RegexOptions.Singleline)]
    private static partial Regex SelectClauseRegex();

    /// <summary>Matches DISTINCT, ALL, or TOP keywords in SELECT clause.</summary>
    [GeneratedRegex(@"^\s*(?:DISTINCT|ALL|TOP\s+\d+)\s+", RegexOptions.IgnoreCase)]
    private static partial Regex DistinctTopRegex();

    /// <summary>Matches SELECT * pattern.</summary>
    [GeneratedRegex(@"^\*$")]
    private static partial Regex SelectStarRegex();

    /// <summary>Matches explicit column aliases using AS keyword.</summary>
    [GeneratedRegex(@"^(.*?)\s+AS\s+(.+)$", RegexOptions.IgnoreCase)]
    private static partial Regex ExplicitAliasRegex();

    /// <summary>Matches implicit aliases after qualified columns or functions.</summary>
    [GeneratedRegex(@"^(.*?[\.\)\]])\s+(\w+)$")]
    private static partial Regex ImplicitAliasRegex();

    /// <summary>Matches simple qualified column aliases (table.column alias).</summary>
    [GeneratedRegex(@"^(\w+\.\w+)\s+(\w+)$")]
    private static partial Regex SimpleQualifiedAliasRegex();

    /// <summary>Matches numeric or string literals.</summary>
    [GeneratedRegex(@"^\d+$|^'[^']*'$", RegexOptions.IgnoreCase)]
    private static partial Regex LiteralRegex();

    /// <summary>Matches SQL function calls including window functions and common SQL functions.</summary>
    [GeneratedRegex(@"(ROW_NUMBER|RANK|DENSE_RANK|NTILE|LEAD|LAG|FIRST_VALUE|LAST_VALUE|PERCENT_RANK|CUME_DIST|COUNT|SUM|AVG|MIN|MAX|CAST|CONVERT|COALESCE|ISNULL|CONCAT|SUBSTRING|UPPER|LOWER|LTRIM|RTRIM|LEFT|RIGHT|DATEADD|DATEDIFF|DATEPART|GETDATE|\w+)\s*\(", RegexOptions.IgnoreCase)]
    private static partial Regex FunctionRegex();

    /// <summary>Matches Common Table Expressions (WITH ... AS (...)).</summary>
    [GeneratedRegex(@"^\s*WITH\s+.*?\)\s*(?=SELECT)", RegexOptions.IgnoreCase | RegexOptions.Singleline)]
    private static partial Regex CtePatternRegex();

    /// <summary>Matches SELECT keyword embedded in column expressions (subqueries).</summary>
    [GeneratedRegex(@"SELECT\s+", RegexOptions.IgnoreCase)]
    private static partial Regex SubqueryDetectionRegex();

    #endregion

    #region Pattern Constants

    // Pattern 1: Bracketed three-part identifier [db].[schema].[table]
    // Example: [MyDB].[dbo].[Users], [DB1].[sys].[tables]
    // Negative lookahead (?!\.\[) ensures we don't match part of a four-part identifier
    private const string BracketedThreePartPattern =
        @"(?:FROM|JOIN|INTO|UPDATE|TABLE|MERGE|USING)\s+\[([^\]]+)\]\.\[([^\]]+)\]\.\[([^\]]+)\](?!\.\[)";

    // Pattern 2: Mixed bracketed/unbracketed [db].schema.table or db.[schema].[table]
    // Example: [MyDB].dbo.Users, MyDB.[dbo].[Users]
    private const string MixedThreePartPattern1 =
        @"(?:FROM|JOIN|INTO|UPDATE|TABLE|MERGE|USING)\s+\[([^\]]+)\]\.(?:\[?[^\]\.\s]+\]?)\.(?:\[?[^\]\.\s\(]+\]?)(?!\.)";
    private const string MixedThreePartPattern2 =
        @"(?:FROM|JOIN|INTO|UPDATE|TABLE|MERGE|USING)\s+(\w+)\.\[([^\]]+)\]\.\[([^\]]+)\](?!\.\[)";

    // Pattern 3: Unbracketed three-part identifier db.schema.table
    // Example: MyDB.dbo.Users, Database1.sys.objects
    // Negative lookahead (?!\.) ensures we don't match part of a four-part identifier
    private const string UnbracketedThreePartPattern =
        @"(?:FROM|JOIN|INTO|UPDATE|TABLE|MERGE|USING)\s+(\w+)\.(\w+)\.(\w+)(?!\.)";


    // Pattern 4: Double-quoted identifiers "db"."schema"."table"
    // Example: "MyDB"."dbo"."Users"
    private const string DoubleQuotedThreePartPattern =
        @"(?:FROM|JOIN|INTO|UPDATE|TABLE|MERGE|USING)\s+""([^""]+)""\.""([^""]+)""\.""([^""]+)""";

    // Pattern 5: Mixed quoted and bracketed "db".[schema].[table]
    // Example: "MyDB".[dbo].[Users], "MyDB".dbo.Users
    private const string MixedQuotedBracketedPattern =
        @"(?:FROM|JOIN|INTO|UPDATE|TABLE|MERGE|USING)\s+""([^""]+)""\.(?:\[?[^\]\.\s]+\]?)\.(?:\[?[^\]\.\s\(]+\]?)";

    // Pattern 6: Server.database.schema.table (four-part names) - extract database (2nd part)
    // Example: [Server1].[MyDB].[dbo].[Users], Server1.MyDB.dbo.Users  
    // Uses non-capturing group for server, captures database
    private const string BracketedFourPartPattern =
        @"(?:FROM|JOIN|INTO|UPDATE|TABLE|MERGE|USING)\s+\[(?:[^\]]+)\]\.\[([^\]]+)\]\.\[([^\]]+)\]\.\[([^\]]+)\]";
    private const string UnbracketedFourPartPattern =
        @"(?:FROM|JOIN|INTO|UPDATE|TABLE|MERGE|USING)\s+(?:\w+)\.(\w+)\.(\w+)\.(\w+)(?!\.)";

    // Pattern 7: Handle table hints WITH (NOLOCK) etc
    // Example: [MyDB].[dbo].[Users] WITH (NOLOCK), MyDB.dbo.Users AS u
    private const string BracketedWithHintPattern =
        @"(?:FROM|JOIN|INTO|UPDATE|TABLE|MERGE|USING)\s+\[([^\]]+)\]\.\[([^\]]+)\]\.\[([^\]]+)\]\s+(?:WITH|AS)";
    private const string UnbracketedWithHintPattern =
        @"(?:FROM|JOIN|INTO|UPDATE|TABLE|MERGE|USING)\s+(\w+)\.(\w+)\.(\w+)\s+(?:WITH|AS)";

    #endregion

    #region Public Methods

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
        var patterns = new[]
        {
            // Four-part patterns MUST come first to match before three-part patterns
            // Pattern 6: Server.database.schema.table (four-part names) - extract database (2nd part)
            BracketedFourPartPattern,
            UnbracketedFourPartPattern,
            
            // Three-part patterns
            // Pattern 1: Bracketed three-part identifier [db].[schema].[table]
            BracketedThreePartPattern,
            // Pattern 2: Mixed bracketed/unbracketed  
            MixedThreePartPattern1,
            MixedThreePartPattern2,
            // Pattern 3: Unbracketed three-part identifier db.schema.table
            UnbracketedThreePartPattern,
            // Pattern 4: Double-quoted identifiers "db"."schema"."table"
            DoubleQuotedThreePartPattern,
            // Pattern 5: Mixed quoted and bracketed "db".[schema].[table]
            MixedQuotedBracketedPattern,
            // Pattern 7: Handle table hints WITH (NOLOCK) etc
            BracketedWithHintPattern,
            UnbracketedWithHintPattern,
        };

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
                    if (Regex.IsMatch(beforeMatch, @"(\w+|\[[^\]]+\])\.\s*$"))
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
        var patterns = new[]
        {
            // Pattern 1: Four-part names - extract table (4th part) - MUST come before three-part patterns
            @"(?:FROM|JOIN)\s+\[([^\]]+)\]\.\[([^\]]+)\]\.\[([^\]]+)\]\.\[([^\]]+)\]",
            @"(?:FROM|JOIN)\s+(\w+)\.(\w+)\.(\w+)\.(\w+)(?!\.\w)",

            // Pattern 2: Bracketed three-part identifier [db].[schema].[table]
            @"(?:FROM|JOIN)\s+\[([^\]]+)\]\.\[([^\]]+)\]\.\[([^\]]+)\](?!\.\[)",

            // Pattern 3: Mixed bracketed/unbracketed three-part db.schema.table or [db].schema.table
            @"(?:FROM|JOIN)\s+\[([^\]]+)\]\.([^\]\.\s\[]+)\.([^\]\.\s\(\[]+)(?!\.\w)(?!\.\[)",
            @"(?:FROM|JOIN)\s+(\w+)\.\[([^\]]+)\]\.\[([^\]]+)\](?!\.\[)",

            // Pattern 4: Unbracketed three-part identifier db.schema.table
            @"(?:FROM|JOIN)\s+(\w+)\.(\w+)\.(\w+)(?!\.\w)",

            // Pattern 5: Bracketed two-part identifier [schema].[table]
            @"(?:FROM|JOIN)\s+\[([^\]]+)\]\.\[([^\]]+)\](?!\.\[)",

            // Pattern 6: Mixed two-part identifier [schema].table
            @"(?:FROM|JOIN)\s+\[([^\]]+)\]\.(\w+)(?!\.)(?!\s*\()",

            // Pattern 7: Unbracketed two-part identifier schema.table
            @"(?:FROM|JOIN)\s+(\w+)\.(\w+)(?!\.\w)",

            // Pattern 8: Double-quoted identifiers
            @"(?:FROM|JOIN)\s+""([^""]+)""\.""([^""]+)""\.""([^""]+)""\.""([^""]+)""(?!\."")",
            @"(?:FROM|JOIN)\s+""([^""]+)""\.""([^""]+)""\.""([^""]+)""(?!\."")",
            @"(?:FROM|JOIN)\s+""([^""]+)""\.""""([^""]+)""(?!\.)(?!\."")",
            @"(?:FROM|JOIN)\s+""([^""]+)""(?!\."")",

            // Pattern 9: Single bracketed table name [table]
            @"(?:FROM|JOIN)\s+\[([^\]]+)\](?!\.\[)(?!\.)",

            // Pattern 10: Single unbracketed table name
            @"(?:FROM|JOIN)\s+(\w+)(?!\.\w)(?!\s*\()",

            // Pattern 11: Handle table hints WITH (NOLOCK) etc
            @"(?:FROM|JOIN)\s+\[([^\]]+)\]\.\[([^\]]+)\]\.\[([^\]]+)\]\s+(?:WITH|AS)",
            @"(?:FROM|JOIN)\s+(\w+)\.(\w+)\.(\w+)\s+(?:WITH|AS)",
        };

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
    /// <item><c>ColumnName</c> - The column name or alias (always populated for valid columns)</item>
    /// </list>
    /// Returns an empty list if the SQL is not a SELECT statement or contains no columns.
    /// </returns>
    /// <remarks>
    /// <para>This method handles:</para>
    /// <list type="bullet">
    /// <item>SELECT * returns a single entry with ColumnName = "*"</item>
    /// <item>Simple columns: SELECT Name → (null, null, "Name")</item>
    /// <item>Qualified columns: SELECT u.Name → (null, "u", "Name")</item>
    /// <item>Fully qualified: SELECT DB.dbo.Users.Name → ("DB", "Users", "Name")</item>
    /// <item>Explicit aliases: SELECT Name AS FullName → ColumnName = "FullName"</item>
    /// <item>Implicit aliases: SELECT u.Name UserName → ColumnName = "UserName"</item>
    /// <item>Functions: SELECT COUNT(*) AS Total → (null, null, "Total")</item>
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
    /// //   (null, "u", "Name"),
    /// //   (null, "u", "EmailAddress"),
    /// //   (null, null, "Total")
    /// // ]
    /// 
    /// var sql2 = "SELECT MyDB.dbo.Users.FirstName FROM MyDB.dbo.Users";
    /// var columns2 = SqlInterrogator.ExtractColumnDetailsFromSelectClauseInSql(sql2);
    /// // Result: [(MyDB", "Users", "FirstName")]
    /// </code>
    /// </example>
    public static List<(string? DatabaseName, string? TableName, string ColumnName)> ExtractColumnDetailsFromSelectClauseInSql(string sql)
    {
        var columns = new List<(string? DatabaseName, string? TableName, string ColumnName)>();

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
            columns.Add((null, null, "*"));
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

    #endregion

    #region Private Helper Methods

    /// <summary>
    /// Splits a text string by commas whilst respecting parentheses nesting.
    /// This ensures function parameters like CONCAT(a, b) aren't incorrectly split.
    /// </summary>
    /// <param name="text">The text to split (typically a SELECT clause).</param>
    /// <returns>A list of text segments split by commas at depth 0.</returns>
    /// <remarks>
    /// This method tracks parentheses depth and only splits on commas that are not
    /// inside function calls or subqueries.
    /// </remarks>
    private static List<string> SplitByCommaRespectingParentheses(string text)
    {
        var result = new List<string>();
        // Pre-size StringBuilder based on estimated average column length
        var estimatedColumnLength = text.Length / EstimatedColumnsPerQuery;
        var currentPart = new System.Text.StringBuilder(estimatedColumnLength);
        var parenDepth = 0;

        foreach (var ch in text)
        {
            if (ch == '(')
            {
                parenDepth++;
                _ = currentPart.Append(ch);
            }
            else if (ch == ')')
            {
                parenDepth--;
                _ = currentPart.Append(ch);
            }
            else if (ch == ',' && parenDepth == 0)
            {
                // Only split on commas at depth 0 (not inside parentheses)
                result.Add(currentPart.ToString());
                _ = currentPart.Clear();
            }
            else
            {
                _ = currentPart.Append(ch);
            }
        }

        // Add the last segment
        if (currentPart.Length > 0)
        {
            result.Add(currentPart.ToString());
        }

        return result;
    }

    /// <summary>
    /// Extracts database name, table name, and column name from a single column expression.
    /// </summary>
    /// <param name="expression">A single column expression from a SELECT clause.</param>
    /// <returns>
    /// A tuple with (DatabaseName, TableName, ColumnName) if the expression is valid,
    /// otherwise null. Literals and invalid expressions return null.
    /// </returns>
    /// <remarks>
    /// <para>Handles various expression types:</para>
    /// <list type="bullet">
    /// <item>Simple: "Name" → (null, null, "Name")</item>
    /// <item>Qualified: "Table.Column" → (null, "Table", "Column")</item>
    /// <item>Fully qualified: "DB.Schema.Table.Column" → ("DB", "Table", "Column")</item>
    /// <item>Explicit aliases: "Name AS FullName" → ColumnName = "FullName"</item>
    /// <item>Implicit aliases: "Table.Column UserName" → ColumnName = "UserName"</item>
    /// <item>Functions: "COUNT(*) AS Total" → (null, null, "Total")</item>
    /// <item>DISTINCT and TOP keywords are automatically removed</item>
    /// <item>Comments, CTEs, and USE statements are automatically removed</item>
    /// </list>
    /// </remarks>
    private static (string? DatabaseName, string? TableName, string ColumnName)? ExtractColumnDetailFromExpression(string expression)
    {
        // Extract alias (explicit or implicit) and get the column part
        var (columnPart, aliasName) = ExtractAlias(expression);

        // Check for various expression types
        if (IsLiteral(columnPart))
        {
            return null; // Skip literal values
        }

        if (IsSubquery(columnPart))
        {
            // Subquery: use alias if provided, otherwise return null
            return aliasName != null ? (null, null, aliasName) : null;
        }

        // Check for complex expressions (CASE, arithmetic, etc.)
        if (IsComplexExpression(columnPart))
        {
            // Complex expression: return alias if provided, otherwise null
            return aliasName != null ? (null, null, aliasName) : null;
        }

        if (IsFunctionCall(columnPart, out var functionName))
        {
            // Function: use alias if provided, otherwise function name
            // functionName is guaranteed to be non-null when IsFunctionCall returns true
            var columnName = aliasName ?? functionName!;
            return (null, null, columnName);
        }

        // Parse qualified column names
        return ParseQualifiedColumnName(columnPart, aliasName);
    }

    /// <summary>
    /// Extracts the alias (explicit with AS or implicit) from a column expression.
    /// </summary>
    /// <param name="expression">The column expression.</param>
    /// <returns>A tuple containing the column part and the alias name (if any).</returns>
    private static (string ColumnPart, string? AliasName) ExtractAlias(string expression)
    {
        // Step 1: Check for explicit alias using AS keyword
        var aliasMatch = ExplicitAliasRegex().Match(expression);

        if (aliasMatch.Success)
        {
            // Found explicit alias: "expression AS alias"
            var columnPart = aliasMatch.Groups[1].Value.Trim();
            var aliasName = aliasMatch.Groups[2].Value.Trim().Trim('[', ']', '"');
            return (columnPart, aliasName);
        }

        // Step 2: Check for implicit alias (space-separated)
        // Must contain a dot or closing paren/bracket to distinguish from simple column names
        var implicitAliasMatch = ImplicitAliasRegex().Match(expression);
        if (!implicitAliasMatch.Success)
        {
            // Try simpler pattern for qualified columns: table.column alias
            implicitAliasMatch = SimpleQualifiedAliasRegex().Match(expression);
        }

        if (implicitAliasMatch.Success)
        {
            // Found implicit alias: "expression alias"
            var columnPart = implicitAliasMatch.Groups[1].Value.Trim();
            var aliasName = implicitAliasMatch.Groups[2].Value.Trim();
            return (columnPart, aliasName);
        }

        // No alias found
        return (expression, null);
    }

    /// <summary>
    /// Determines whether a column part is a literal value (number or string).
    /// </summary>
    /// <param name="columnPart">The column expression to check.</param>
    /// <returns>True if the expression is a literal; otherwise, false.</returns>
    /// <remarks>
    /// Note: Double-quoted identifiers are NOT literals in SQL Server.
    /// </remarks>
    private static bool IsLiteral(string columnPart)
    {
        return LiteralRegex().IsMatch(columnPart.Trim());
    }

    /// <summary>
    /// Determines whether a column part contains a subquery (SELECT statement).
    /// </summary>
    /// <param name="columnPart">The column expression to check.</param>
    /// <returns>True if the expression contains a subquery; otherwise, false.</returns>
    private static bool IsSubquery(string columnPart)
    {
        return SubqueryDetectionRegex().IsMatch(columnPart);
    }

    /// <summary>
    /// Determines whether a column part is a complex expression (CASE, arithmetic, etc.).
    /// </summary>
    /// <param name="columnPart">The column expression to check.</param>
    /// <returns>True if the expression is a complex expression; otherwise, false.</returns>
    private static bool IsComplexExpression(string columnPart)
    {
        // Normalize whitespace for better pattern matching
        var normalizedPart = Regex.Replace(columnPart.Trim(), @"\s+", " ");
        var upperPart = normalizedPart.ToUpperInvariant();

        // Check for CASE expressions (handles multiline)
        if (upperPart.StartsWith("CASE") || upperPart.Contains(" CASE ") || upperPart.Contains(" WHEN "))
        {
            return true;
        }

        // Check for arithmetic operators (+-*/) but only if there's NO qualified column
        // If it has a table.column reference (contains dot before the operator), it's OK
        var hasDot = upperPart.Contains('.');
        var hasArithmetic = 
            upperPart.Contains(" + ") || 
            upperPart.Contains(" - ") ||
            upperPart.Contains(" * ") ||
            upperPart.Contains(" / ");

        // Only treat as complex if it has arithmetic BUT no table qualification
        return hasArithmetic && !hasDot;
    }

    /// <summary>
    /// Determines whether a column part is a function call and extracts the function name.
    /// </summary>
    /// <param name="columnPart">The column expression to check.</param>
    /// <param name="functionName">The name of the function if found.</param>
    /// <returns>True if the expression is a function call; otherwise, false.</returns>
    private static bool IsFunctionCall(string columnPart, out string? functionName)
    {
        functionName = null;

        if (!columnPart.Contains('('))
        {
            return false;
        }

        var functionMatch = FunctionRegex().Match(columnPart);
        if (functionMatch.Success)
        {
            functionName = functionMatch.Groups[1].Value;
            return true;
        }

        return false;
    }

    /// <summary>
    /// Parses a qualified column name and extracts database, table, and column components.
    /// </summary>
    /// <param name="columnPart">The column expression to parse.</param>
    /// <param name="aliasName">The alias name if one was detected.</param>
    /// <returns>
    /// A tuple with (DatabaseName, TableName, ColumnName) if parsing succeeds;
    /// otherwise, null.
    /// </returns>
    /// <remarks>
    /// Handles 1-part through 5-part identifiers:
    /// <list type="bullet">
    /// <item>1-part: ColumnName</item>
    /// <item>2-part: Table.Column</item>
    /// <item>3-part: DB.Table.Column (or Schema.Table.Column)</item>
    /// <item>4-part: DB.Schema.Table.Column</item>
    /// <item>5-part: Server.DB.Schema.Table.Column</item>
    /// </list>
    /// </remarks>
    private static (string? DatabaseName, string? TableName, string ColumnName)? ParseQualifiedColumnName(
        string columnPart,
        string? aliasName)
    {
        // Remove double quotes for processing (SQL Server allows "identifier" syntax)
        var cleanColumnPart = columnPart.Trim().Replace("\"", "");

        // Patterns ordered from most specific (5-part) to least specific (1-part)
        var patterns = new[]
        {
            // Five-part: [server].[db].[schema].[table].[column]
            (@"^\[?([^\]\.]+)\]?\.\[?([^\]\.]+)\]?\.\[?([^\]\.]+)\]?\.\[?([^\]\.]+)\]?\.\[?([^\]\.]+)\]?$", 5),
 
            // Four-part: [db].[schema].[table].[column] or db.schema.table.column
            (@"^\[?([^\]\.]+)\]?\.\[?([^\]\.]+)\]?\.\[?([^\]\.]+)\]?\.\[?([^\]\.]+)\]?$", 4),

            // Three-part: [db].[table].[column] or db.table.column
            (@"^\[?([^\]\.]+)\]?\.\[?([^\]\.]+)\]?\.\[?([^\]\.]+)\]?$", 3),

            // Two-part: [table].[column] or table.column
            (@"^\[?([^\]\.]+)\]?\.\[?([^\]\.]+)\]?$", 2),

            // Single part: [column] or column
            (@"^\[?([^\]\.]+)\]?$", 1),
        };

        // Try each pattern to determine the column's qualification level
        foreach (var (pattern, parts) in patterns)
        {
            var match = Regex.Match(cleanColumnPart.Trim(), pattern);
            if (match.Success)
            {
                return parts switch
                {
                    // server.db.schema.table.column
                    // Extract: server (1st) as DB, table (4th), column (5th)
                    5 => (match.Groups[DatabaseGroupIndex].Value,
                          match.Groups[ColumnGroupIndex].Value,
                          aliasName ?? match.Groups[FifthPartGroupIndex].Value),

                    // db.schema.table.column
                    // Extract: db (1st), table (3rd - skip schema), column (4th)
                    4 => (match.Groups[DatabaseGroupIndex].Value,
                          match.Groups[TableGroupIndex].Value,
                          aliasName ?? match.Groups[ColumnGroupIndex].Value),

                    // Could be db.table.column or schema.table.column
                    // Assume db.table.column format
                    3 => (match.Groups[DatabaseGroupIndex].Value,
                          match.Groups[SecondaryDatabaseGroupIndex].Value,
                          aliasName ?? match.Groups[TableGroupIndex].Value),

                    // table.column (most common qualified format)
                    2 => (null,
                  match.Groups[DatabaseGroupIndex].Value,
                     aliasName ?? match.Groups[SecondaryDatabaseGroupIndex].Value),

                    // Simple column name (no qualification)
                    1 => (null,
                          null,
                          aliasName ?? match.Groups[DatabaseGroupIndex].Value),

                    _ => null
                };
            }
        }

        return null;
    }

    /// <summary>
    /// Extracts the database name from the first or second capture group of a regex match.
    /// </summary>
    /// <param name="match">The regex match containing database name captures.</param>
    /// <returns>
    /// The database name from Group[1] if available, otherwise from Group[2],
    /// otherwise an empty string.
    /// </returns>
    /// <remarks>
    /// Different patterns capture the database name in different groups depending on
    /// whether it's a three-part or four-part identifier.
    /// </remarks>
    private static string ExtractDatabaseNameFromMatch(Match match)
    {
        return match.Groups[DatabaseGroupIndex].Success &&
            !string.IsNullOrWhiteSpace(match.Groups[DatabaseGroupIndex].Value)
                ? match.Groups[DatabaseGroupIndex].Value
                : match.Groups[SecondaryDatabaseGroupIndex].Success &&
                !string.IsNullOrWhiteSpace(match.Groups[SecondaryDatabaseGroupIndex].Value)
                    ? match.Groups[SecondaryDatabaseGroupIndex].Value
                    : string.Empty;
    }

    /// <summary>
    /// Removes both single-line (--) and multi-line (/* */) SQL comments from the SQL text.
    /// </summary>
    /// <param name="sql">The SQL text containing comments.</param>
    /// <returns>The SQL text with all comments replaced by spaces.</returns>
    /// <remarks>
    /// Comments are replaced with spaces rather than being removed entirely to preserve
    /// the relative positions of tokens in the SQL.
    /// </remarks>
    private static string RemoveComments(string sql)
    {
        sql = MultilineRegex().Replace(sql, " ");
        sql = SingleLineRegex().Replace(sql, " ");
        return sql;
    }

    /// <summary>
    /// Removes Common Table Expression (CTE) definitions from the SQL, leaving only the main query.
    /// </summary>
    /// <param name="sql">The SQL text that may contain CTEs.</param>
    /// <returns>The SQL text with CTE definitions removed.</returns>
    /// <remarks>
    /// <para>Removes patterns like:</para>
    /// <code>
    /// WITH CTE_Name AS (
    ///  SELECT ...
    /// )
    /// SELECT * FROM CTE_Name
    /// </code>
    /// <para>Becomes: <c>SELECT * FROM CTE_Name</c></para>
    /// </remarks>
    private static string RemoveCTEs(string sql)
    {
        return CtePatternRegex().Replace(sql, "");
    }

    /// <summary>
    /// Removes USE database statements and GO batch separators from the SQL.
    /// </summary>
    /// <param name="sql">The SQL text that may contain USE statements.</param>
    /// <returns>The SQL text with USE statements and GO separators removed and trimmed.</returns>
    /// <remarks>
    /// <para>Handles various formats:</para>
    /// <list type="bullet">
    /// <item>USE MyDatabase;</item>
    /// <item>USE [MyDatabase]</item>
    /// <item>USE MyDatabase\nGO</item>
    /// <item>Standalone GO statements</item>
    /// </list>
    /// </remarks>
    private static string RemoveUseStatements(string sql)
    {
        sql = UseWithGoRegex().Replace(sql, "");
        sql = UseStatementRegex().Replace(sql, "");
        sql = StandaloneGoRegex().Replace(sql, "");

        return sql.Trim();
    }

    #endregion
}
