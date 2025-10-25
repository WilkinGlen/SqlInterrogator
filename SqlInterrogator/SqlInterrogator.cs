namespace SqlInterrogatorService;

using System.Text.RegularExpressions;

public static partial class SqlInterrogator
{
    [GeneratedRegex(@"--[^\r\n]*", RegexOptions.Multiline)]
    private static partial Regex MultilineRegex();
    [GeneratedRegex(@"/\*.*?\*/", RegexOptions.Singleline)]
    private static partial Regex SingleLineRegex();
    [GeneratedRegex(@"^\s*SELECT\s+", RegexOptions.IgnoreCase, "en-UG")]
    private static partial Regex IgnoreCaseRegex();
    [GeneratedRegex(@"USE\s+\[?[\w\s]+\]?\s*;?\s*\r?\n\s*GO\s*", RegexOptions.IgnoreCase | RegexOptions.Multiline)]
    private static partial Regex UseWithGoRegex();
    [GeneratedRegex(@"USE\s+\[?[\w\s]+\]?\s*;?\s*", RegexOptions.IgnoreCase)]
    private static partial Regex UseStatementRegex();
    [GeneratedRegex(@"^\s*GO\s*$", RegexOptions.IgnoreCase | RegexOptions.Multiline)]
    private static partial Regex StandaloneGoRegex();
    [GeneratedRegex(@"SELECT\s+(.*?)\s+FROM", RegexOptions.IgnoreCase | RegexOptions.Singleline)]
    private static partial Regex SelectClauseRegex();
    [GeneratedRegex(@"^\s*(?:DISTINCT|ALL|TOP\s+\d+)\s+", RegexOptions.IgnoreCase)]
    private static partial Regex DistinctTopRegex();
    [GeneratedRegex(@"^\*$")]
    private static partial Regex SelectStarRegex();
    [GeneratedRegex(@"^(.*?)\s+AS\s+(.+)$", RegexOptions.IgnoreCase)]
    private static partial Regex ExplicitAliasRegex();
    [GeneratedRegex(@"^(.*?[\.\)\]])\s+(\w+)$")]
    private static partial Regex ImplicitAliasRegex();
    [GeneratedRegex(@"^(\w+\.\w+)\s+(\w+)$")]
    private static partial Regex SimpleQualifiedAliasRegex();
    [GeneratedRegex(@"^\d+$|^'[^']*'$", RegexOptions.IgnoreCase)]
    private static partial Regex LiteralRegex();
    [GeneratedRegex(@"(\w+)\s*\(")]
    private static partial Regex FunctionRegex();
    [GeneratedRegex(@"^\s*WITH\s+.*?\)\s*(?=SELECT)", RegexOptions.IgnoreCase | RegexOptions.Singleline)]
    private static partial Regex CtePatternRegex();

    public static List<string> ExtractDatabaseNamesFromSql(string sql)
    {
        if (string.IsNullOrWhiteSpace(sql))
        {
            return [];
        }

        var databaseNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        sql = RemoveComments(sql);
        var patterns = new[]
        {
            // Pattern 1: Bracketed three-part identifier [db].[schema].[table]
            @"(?:FROM|JOIN|INTO|UPDATE|TABLE)\s+\[([^\]]+)\]\.\[([^\]]+)\]\.\[([^\]]+)\]",

            // Pattern 2: Mixed bracketed/unbracketed [db].schema.table or db.[schema].[table]
            @"(?:FROM|JOIN|INTO|UPDATE|TABLE)\s+\[([^\]]+)\]\.(?:\[?[^\]\.\s]+\]?)\.(?:\[?[^\]\.\s\(]+\]?)",
            @"(?:FROM|JOIN|INTO|UPDATE|TABLE)\s+(\w+)\.\[([^\]]+)\]\.\[([^\]]+)\]",

            // Pattern 3: Unbracketed three-part identifier db.schema.table
            @"(?:FROM|JOIN|INTO|UPDATE|TABLE)\s+(\w+)\.(\w+)\.(\w+)",

            // Pattern 4: Double-quoted identifiers "db"."schema"."table"
            @"(?:FROM|JOIN|INTO|UPDATE|TABLE)\s+""([^""]+)""\.""([^""]+)""\.""([^""]+)""",

            // Pattern 5: Mixed quoted and bracketed "db".[schema].[table]
            @"(?:FROM|JOIN|INTO|UPDATE|TABLE)\s+""([^""]+)""\.(?:\[?[^\]\.\s]+\]?)\.(?:\[?[^\]\.\s\(]+\]?)",

            // Pattern 6: Server.database.schema.table (four-part names) - extract database (2nd part)
            @"(?:FROM|JOIN|INTO|UPDATE|TABLE)\s+(?:\[?[^\]\.\s]+\]?)\.\[([^\]]+)\]\.\[([^\]]+)\]\.\[([^\]]+)\]",
            @"(?:FROM|JOIN|INTO|UPDATE|TABLE)\s+(?:\w+)\.(\w+)\.(\w+)\.(\w+)",

            // Pattern 7: Handle table hints WITH (NOLOCK) etc
            @"(?:FROM|JOIN|INTO|UPDATE|TABLE)\s+\[([^\]]+)\]\.\[([^\]]+)\]\.\[([^\]]+)\]\s+(?:WITH|AS)",
            @"(?:FROM|JOIN|INTO|UPDATE|TABLE)\s+(\w+)\.(\w+)\.(\w+)\s+(?:WITH|AS)",
        };

        foreach (var pattern in patterns)
        {
            var matches = Regex.Matches(sql, pattern, RegexOptions.IgnoreCase | RegexOptions.Multiline);
            foreach (Match match in matches)
            {
                var databaseName = ExtractDatabaseNameFromMatch(match);
                if (!string.IsNullOrWhiteSpace(databaseName))
                {
                    _ = databaseNames.Add(databaseName);
                }
            }
        }

        return [.. databaseNames];
    }

    public static string? ExtractFirstTableNameFromSelectClauseInSql(string sql)
    {
        if (string.IsNullOrWhiteSpace(sql))
        {
            return null;
        }

        sql = RemoveComments(sql);
        sql = RemoveCTEs(sql);
        sql = RemoveUseStatements(sql);

        if (!IgnoreCaseRegex().IsMatch(sql))
        {
            return null;
        }

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
            @"(?:FROM|JOIN)\s+""([^""]+)""\.""([^""]+)""(?!\.)(?!\."")",
            @"(?:FROM|JOIN)\s+""([^""]+)""(?!\."")",

            // Pattern 9: Single bracketed table name [table]
            @"(?:FROM|JOIN)\s+\[([^\]]+)\](?!\.\[)(?!\.)",

            // Pattern 10: Single unbracketed table name
            @"(?:FROM|JOIN)\s+(\w+)(?!\.\w)(?!\s*\()",

            // Pattern 11: Handle table hints WITH (NOLOCK) etc
            @"(?:FROM|JOIN)\s+\[([^\]]+)\]\.\[([^\]]+)\]\.\[([^\]]+)\]\s+(?:WITH|AS)",
            @"(?:FROM|JOIN)\s+(\w+)\.(\w+)\.(\w+)\s+(?:WITH|AS)",
        };

        foreach (var pattern in patterns)
        {
            var match = Regex.Match(sql, pattern, RegexOptions.IgnoreCase | RegexOptions.Multiline);
            if (match.Success)
            {
                for (var i = match.Groups.Count - 1; i >= 1; i--)
                {
                    if (match.Groups[i].Success && !string.IsNullOrWhiteSpace(match.Groups[i].Value))
                    {
                        return match.Groups[i].Value;
                    }
                }
            }
        }

        return null;
    }

    public static List<(string? DatabaseName, string? TableName, string ColumnName)> ExtractColumnDetailsFromSelectClauseInSql(string sql)
    {
        var columns = new List<(string? DatabaseName, string? TableName, string ColumnName)>();

        if (string.IsNullOrWhiteSpace(sql))
        {
            return columns;
        }

        sql = RemoveComments(sql);
        sql = RemoveCTEs(sql);
        sql = RemoveUseStatements(sql);

        if (!IgnoreCaseRegex().IsMatch(sql))
        {
            return columns;
        }

        // Extract the SELECT clause (between SELECT and FROM)
        var selectClauseMatch = SelectClauseRegex().Match(sql);
        if (!selectClauseMatch.Success)
        {
            return columns;
        }

        var selectClause = selectClauseMatch.Groups[1].Value;

        // Remove DISTINCT, TOP, etc. keywords
        selectClause = DistinctTopRegex().Replace(selectClause, "");

        // Handle SELECT *
        if (SelectStarRegex().IsMatch(selectClause.Trim()))
        {
            columns.Add((null, null, "*"));
            return columns;
        }

        // Split by comma, but not within parentheses (for functions like CONCAT, etc.)
        var columnExpressions = SplitByCommaRespectingParentheses(selectClause);

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

    private static List<string> SplitByCommaRespectingParentheses(string text)
    {
        var result = new List<string>();
        var currentPart = new System.Text.StringBuilder();
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
                result.Add(currentPart.ToString());
                _ = currentPart.Clear();
            }
            else
            {
                _ = currentPart.Append(ch);
            }
        }

        if (currentPart.Length > 0)
        {
            result.Add(currentPart.ToString());
        }

        return result;
    }

    private static (string? DatabaseName, string? TableName, string ColumnName)? ExtractColumnDetailFromExpression(string expression)
    {
        // Remove AS alias if present (match the column name before AS)
        var aliasMatch = ExplicitAliasRegex().Match(expression);
        string columnPart;
        string? aliasName = null;

        if (aliasMatch.Success)
        {
            columnPart = aliasMatch.Groups[1].Value.Trim();
            aliasName = aliasMatch.Groups[2].Value.Trim().Trim('[', ']', '"');
        }
        else
        {
            // Check for implicit alias (must be a simple identifier after a space)
            // Match patterns like "u.Name UserName" or "CONCAT(...) FullName" or "[Users].[Name] UserName"  
            // Look for a column reference followed by whitespace and then a simple word (the alias)
            // Must contain either a dot (qualified column) or closing paren (function)
            var implicitAliasMatch = ImplicitAliasRegex().Match(expression);
            if (!implicitAliasMatch.Success)
            {
                // Try simpler pattern for qualified columns without functions: word.word word
                implicitAliasMatch = SimpleQualifiedAliasRegex().Match(expression);
            }

            if (implicitAliasMatch.Success)
            {
                columnPart = implicitAliasMatch.Groups[1].Value.Trim();
                aliasName = implicitAliasMatch.Groups[2].Value.Trim();
            }
            else
            {
                columnPart = expression;
            }
        }

        // Skip numeric literals and string literals (but NOT double-quoted identifiers)
        // Double-quoted identifiers don't have spaces and don't mix quotes
        if (LiteralRegex().IsMatch(columnPart.Trim()))
        {
            // It's a literal
            return null;
        }

        // If it's a function call, extract the alias or function name
        if (columnPart.Contains('('))
        {
            var functionMatch = FunctionRegex().Match(columnPart);
            if (functionMatch.Success)
            {
                var columnName = aliasName ?? functionMatch.Groups[1].Value;
                return (null, null, columnName);
            }

            return null;
        }

        // Try to match qualified column names: [db].[schema].[table].[column] or variations
        // Remove quotes for processing
        var cleanColumnPart = columnPart.Trim().Replace("\"", "");

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

        foreach (var (pattern, parts) in patterns)
        {
            var match = Regex.Match(cleanColumnPart.Trim(), pattern);
            if (match.Success)
            {
                switch (parts)
                {
                    case 5:
                        // server.db.schema.table.column - extract server (1st) and table (4th), column (5th)
                        return (match.Groups[1].Value, match.Groups[4].Value, aliasName ?? match.Groups[5].Value);

                    case 4:
                        // db.schema.table.column - extract db (1st) and table (3rd), column (4th)
                        return (match.Groups[1].Value, match.Groups[3].Value, aliasName ?? match.Groups[4].Value);

                    case 3:
                        // Could be db.table.column or schema.table.column
                        // Assume it's db.table.column
                        return (match.Groups[1].Value, match.Groups[2].Value, aliasName ?? match.Groups[3].Value);

                    case 2:
                        // table.column
                        return (null, match.Groups[1].Value, aliasName ?? match.Groups[2].Value);

                    case 1:
                        // Just column name
                        return (null, null, aliasName ?? match.Groups[1].Value);
                }
            }
        }

        return null;
    }

    private static string ExtractDatabaseNameFromMatch(Match match)
    {
        return match.Groups[1].Success && !string.IsNullOrWhiteSpace(match.Groups[1].Value)
         ? match.Groups[1].Value
        : match.Groups[2].Success && !string.IsNullOrWhiteSpace(match.Groups[2].Value)
               ? match.Groups[2].Value
           : string.Empty;
    }

    private static string RemoveComments(string sql)
    {
        sql = MultilineRegex().Replace(sql, " ");
        sql = SingleLineRegex().Replace(sql, " ");
        return sql;
    }

    private static string RemoveCTEs(string sql)
    {
        return CtePatternRegex().Replace(sql, "");
    }

    private static string RemoveUseStatements(string sql)
    {
        sql = UseWithGoRegex().Replace(sql, "");
        sql = UseStatementRegex().Replace(sql, "");
        sql = StandaloneGoRegex().Replace(sql, "");

        return sql.Trim();
    }
}
