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
        var ctePattern = @"^\s*WITH\s+.*?\)\s*(?=SELECT)";
        return Regex.Replace(sql, ctePattern, "", RegexOptions.IgnoreCase | RegexOptions.Singleline);
    }
}
