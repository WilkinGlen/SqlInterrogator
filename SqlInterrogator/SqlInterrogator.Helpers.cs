namespace SqlInterrogatorService;

using System.Text.RegularExpressions;

/// <summary>
/// Private helper methods for SqlInterrogator.
/// </summary>
public static partial class SqlInterrogator
{
    /// <summary>
    /// Gets the pattern array for database name extraction, ordered from most specific to least specific.
    /// </summary>
    private static string[] GetDatabasePatterns() => new[]
    {
        // Four-part patterns MUST come first to match before three-part patterns
        // Server.database.schema.table (four-part names) - extract database (2nd part)
    BracketedFourPartPattern,
        UnbracketedFourPartPattern,
   // Three-part patterns
        // Bracketed three-part identifier [db].[schema].[table]
        BracketedThreePartPattern,
        // Mixed bracketed/unbracketed
    MixedThreePartPattern1,
        MixedThreePartPattern2,
        // Unbracketed three-part identifier db.schema.table
        UnbracketedThreePartPattern,
        // Double-quoted identifiers "db"."schema"."table"
 DoubleQuotedThreePartPattern,
        // Mixed quoted and bracketed "db".[schema].[table]
        MixedQuotedBracketedPattern,
   // Table hints WITH (NOLOCK) etc
    BracketedWithHintPattern,
        UnbracketedWithHintPattern,
    };

    /// <summary>
    /// Gets the pattern array for table name extraction, ordered from most specific to least specific.
    /// </summary>
    private static string[] GetTableNamePatterns() => new[]
    {
        // Four-part names - extract table (4th part) - MUST come before three-part patterns
@"(?:FROM|JOIN)\s+\[([^\]]+)\]\.\[([^\]]+)\]\.\[([^\]]+)\]\.\[([^\]]+)\]",
        @"(?:FROM|JOIN)\s+(\w+)\.(\w+)\.(\w+)\.(\w+)(?!\.\w)",
        // Bracketed three-part identifier [db].[schema].[table]
        @"(?:FROM|JOIN)\s+\[([^\]]+)\]\.\[([^\]]+)\]\.\[([^\]]+)\](?!\.\[)",
        // Mixed bracketed/unbracketed three-part db.schema.table or [db].schema.table
        @"(?:FROM|JOIN)\s+\[([^\]]+)\]\.([^\]\.\s\[]+)\.([^\]\.\s\(\[]+)(?!\.\w)(?!\.\[)",
        @"(?:FROM|JOIN)\s+(\w+)\.\[([^\]]+)\]\.\[([^\]]+)\](?!\.\[)",
        // Unbracketed three-part identifier db.schema.table
   @"(?:FROM|JOIN)\s+(\w+)\.(\w+)\.(\w+)(?!\.\w)",
// Bracketed two-part identifier [schema].[table]
  @"(?:FROM|JOIN)\s+\[([^\]]+)\]\.\[([^\]]+)\](?!\.\[)",
        // Mixed two-part identifier [schema].table
        @"(?:FROM|JOIN)\s+\[([^\]]+)\]\.(\w+)(?!\.)(?!\s*\()",
        // Unbracketed two-part identifier schema.table
        @"(?:FROM|JOIN)\s+(\w+)\.(\w+)(?!\.\w)",
        // Double-quoted identifiers (MUST come after bracketed patterns)
        @"(?:FROM|JOIN)\s+""([^""]+)""\.""([^""]+)""\.""([^""]+)""\.""([^""]+)""(?!\."")",
        @"(?:FROM|JOIN)\s+""([^""]+)""\.""([^""]+)""\.""([^""]+)""(?!\."")",
        @"(?:FROM|JOIN)\s+""([^""]+)""\.""([^""]+)""(?!\."")",
    @"(?:FROM|JOIN)\s+""([^""]+)""(?!\."")",
        // Single bracketed table name [table]
        @"(?:FROM|JOIN)\s+\[([^\]]+)\](?!\.\[)(?!\.)",
        // Single unbracketed table name
        @"(?:FROM|JOIN)\s+(\w+)(?!\.\w)(?!\s*\()",
        // Table hints WITH (NOLOCK) etc
        @"(?:FROM|JOIN)\s+\[([^\]]+)\]\.\[([^\]]+)\]\.\[([^\]]+)\]\s+(?:WITH|AS)",
        @"(?:FROM|JOIN)\s+(\w+)\.(\w+)\.(\w+)\s+(?:WITH|AS)",
    };

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
    /// A tuple with (DatabaseName, TableName, (ColumnName, Alias)) if the expression is valid,
    /// otherwise null. Literals and invalid expressions return null.
    /// </returns>
    /// <remarks>
    /// <para>Handles various expression types:</para>
    /// <list type="bullet">
    /// <item>Simple: "Name" ? (null, null, (ColumnName: "Name", Alias: null))</item>
    /// <item>Qualified: "Table.Column" ? (null, "Table", (ColumnName: "Column", Alias: null))</item>
    /// <item>Fully qualified: "DB.Schema.Table.Column" ? ("DB", "Table", (ColumnName: "Column", Alias: null))</item>
    /// <item>Explicit aliases: "Name AS FullName" ? Column = (ColumnName: "Name", Alias: "FullName")</item>
    /// <item>Implicit aliases: "Table.Column UserName" ? Column = (ColumnName: "Column", Alias: "UserName")</item>
    /// <item>Functions: "COUNT(*) AS Total" ? (null, null, (ColumnName: "COUNT", Alias: "Total"))</item>
    /// <item>DISTINCT and TOP keywords are automatically removed</item>
    /// <item>Comments, CTEs, and USE statements are automatically removed</item>
    /// </list>
    /// </remarks>
    private static (string? DatabaseName, string? TableName, (string ColumnName, string? Alias) Column)? ExtractColumnDetailFromExpression(string expression)
    {
        // Extract alias (explicit with AS or implicit) and get the column part
        var (columnPart, aliasName) = ExtractAlias(expression);

        // Check for various expression types
        if (IsLiteral(columnPart))
        {
            return null; // Skip literal values
        }

        if (IsSubquery(columnPart))
        {
            // Subquery: use alias if provided, otherwise return null
            return aliasName != null ? (null, null, (aliasName, null)) : null;
        }

        // Check for complex expressions (CASE, arithmetic, etc.)
        if (IsComplexExpression(columnPart))
        {
            // Complex expression: return alias if provided, otherwise return null
            return aliasName != null ? (null, null, (aliasName, null)) : null;
        }

        // Special case: parenthesized arithmetic with qualified columns
        // Example: (o.Price * o.Quantity) - o.Discount
        // This has dots (so not marked complex) but starts with ( so won't match ParseQualifiedColumnName patterns
        if (columnPart.TrimStart().StartsWith('(') && columnPart.Contains('.') && aliasName != null)
        {
            // Extract the last qualified column reference
            var lastColumn = ExtractLastQualifiedColumn(columnPart);
            if (lastColumn.HasValue)
            {
                // Return the last column with the alias
                return (lastColumn.Value.DatabaseName, lastColumn.Value.TableName, (lastColumn.Value.Column.ColumnName, aliasName));
            }
        }

        if (IsFunctionCall(columnPart, out var functionName))
        {
            // Function: extract actual column name from function and set alias
            // functionName is guaranteed to be non-null when IsFunctionCall returns true
            return (null, null, (functionName!, aliasName));
        }

        // Parse qualified column names
        return ParseQualifiedColumnName(columnPart, aliasName);
    }

    /// <summary>
    /// Extracts the last qualified column reference from a complex expression.
    /// Used for expressions like (o.Price * o.Quantity) - o.Discount to extract "o.Discount".
    /// </summary>
    private static (string? DatabaseName, string? TableName, (string ColumnName, string? Alias) Column)? ExtractLastQualifiedColumn(string expression)
    {
        // Find all qualified column patterns (table.column or db.table.column)
        // Match patterns like o.Price, t.Quantity, MyDB.dbo.Users.Name
        var columnPattern = @"\b(\w+)\.(\w+)\b";
        var matches = Regex.Matches(expression, columnPattern);

        if (matches.Count == 0)
        {
            return null;
        }

        // Get the last match
        var lastMatch = matches[matches.Count - 1];
        var tableName = lastMatch.Groups[1].Value;
        var columnName = lastMatch.Groups[2].Value;

        // Return as a two-part qualified column (table.column)
        return (null, tableName, (columnName, null));
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
        var normalizedPart = WhitespaceNormalizationRegex().Replace(columnPart.Trim(), " ");
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
    /// A tuple with (DatabaseName, TableName, (ColumnName, Alias)) if parsing succeeds;
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
    private static (string? DatabaseName, string? TableName, (string ColumnName, string? Alias) Column)? ParseQualifiedColumnName(
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
            (@"^\[?([^\]\.]+)\]?\.\[?([^\]\.]+)\]?\.\[?([^\]\.]+)\]?\.\[?([^\]\.]+)\]$", 4),
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
                      (match.Groups[FifthPartGroupIndex].Value, aliasName)),

                    // db.schema.table.column
                    // Extract: db (1st), table (3rd - skip schema), column (4th)
                    4 => (match.Groups[DatabaseGroupIndex].Value,
                    match.Groups[TableGroupIndex].Value,
                         (match.Groups[ColumnGroupIndex].Value, aliasName)),

                    // Could be db.table.column or schema.table.column
                    // Assume db.table.column format
                    3 => (match.Groups[DatabaseGroupIndex].Value,
                     match.Groups[SecondaryDatabaseGroupIndex].Value,
                     (match.Groups[TableGroupIndex].Value, aliasName)),

                    // table.column (most common qualified format)
                    2 => (null,
               match.Groups[DatabaseGroupIndex].Value,
                      (match.Groups[SecondaryDatabaseGroupIndex].Value, aliasName)),

                    // Simple column name (no qualification)
                    1 => (null,
                   null,
                      (match.Groups[DatabaseGroupIndex].Value, aliasName)),

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
        return match.Groups[DatabaseGroupIndex].Success && !string.IsNullOrWhiteSpace(match.Groups[DatabaseGroupIndex].Value)
   ? match.Groups[DatabaseGroupIndex].Value
   : match.Groups[SecondaryDatabaseGroupIndex].Success && !string.IsNullOrWhiteSpace(match.Groups[SecondaryDatabaseGroupIndex].Value)
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

    /// <summary>
    /// Splits WHERE clause conditions by AND/OR operators while respecting parentheses.
    /// </summary>
    /// <param name="whereClause">The WHERE clause text to split.</param>
    /// <returns>A list of individual conditions.</returns>
    /// <remarks>
    /// This method tracks parentheses depth and only splits on AND/OR operators that are not
    /// inside nested conditions or function calls.
    /// </remarks>
    private static List<string> SplitWhereConditions(string whereClause)
    {
        var result = new List<string>();
        var currentCondition = new System.Text.StringBuilder();
        var parenDepth = 0;
        var i = 0;

        while (i < whereClause.Length)
        {
            var ch = whereClause[i];

            if (ch == '(')
            {
                parenDepth++;
                _ = currentCondition.Append(ch);
                i++;
            }
            else if (ch == ')')
            {
                parenDepth--;
                _ = currentCondition.Append(ch);
                i++;
            }
            else if (parenDepth == 0)
            {
                // Check for AND/OR operators at depth 0
                var remainingLength = whereClause.Length - i;
                var canCheckAnd = remainingLength >= 3;
                var canCheckOr = remainingLength >= 2;

                if (canCheckAnd)
                {
                    var next4 = remainingLength >= 4 ? whereClause.Substring(i, 4) : string.Empty;
                    var next5 = remainingLength >= 5 ? whereClause.Substring(i, 5) : string.Empty;

                    // Check for " AND " (with spaces)
                    if (next5.Equals(" AND ", StringComparison.OrdinalIgnoreCase))
                    {
                        if (currentCondition.Length > 0)
                        {
                            result.Add(currentCondition.ToString());
                            _ = currentCondition.Clear();
                        }
                        i += 5;
                        continue;
                    }
                    // Check for "AND " at start or after whitespace
                    else if (next4.Equals("AND ", StringComparison.OrdinalIgnoreCase) &&
               (i == 0 || char.IsWhiteSpace(whereClause[i - 1])))
                    {
                        if (currentCondition.Length > 0)
                        {
                            result.Add(currentCondition.ToString());
                            _ = currentCondition.Clear();
                        }
                        i += 4;
                        continue;
                    }
                }

                if (canCheckOr)
                {
                    var next3 = remainingLength >= 3 ? whereClause.Substring(i, 3) : string.Empty;
                    var next4 = remainingLength >= 4 ? whereClause.Substring(i, 4) : string.Empty;

                    // Check for " OR " (with spaces)
                    if (next4.Equals(" OR ", StringComparison.OrdinalIgnoreCase))
                    {
                        if (currentCondition.Length > 0)
                        {
                            result.Add(currentCondition.ToString());
                            _ = currentCondition.Clear();
                        }
                        i += 4;
                        continue;
                    }
                    // Check for "OR " at start or after whitespace
                    else if (next3.Equals("OR ", StringComparison.OrdinalIgnoreCase) &&
              (i == 0 || char.IsWhiteSpace(whereClause[i - 1])))
                    {
                        if (currentCondition.Length > 0)
                        {
                            result.Add(currentCondition.ToString());
                            _ = currentCondition.Clear();
                        }
                        i += 3;
                        continue;
                    }
                }

                _ = currentCondition.Append(ch);
                i++;
            }
            else
            {
                _ = currentCondition.Append(ch);
                i++;
            }
        }

        // Add the last condition
        if (currentCondition.Length > 0)
        {
            result.Add(currentCondition.ToString());
        }

        return result;
    }
}
