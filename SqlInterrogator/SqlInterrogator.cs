using System.Text.RegularExpressions;
using System.Runtime.CompilerServices;

namespace SqlInterrogatorService;

public static partial class SqlInterrogator
{
    [GeneratedRegex(@"--[^\r\n]*", RegexOptions.Multiline)]
    private static partial Regex MultilineRegex();
    [GeneratedRegex(@"/\*.*?\*/", RegexOptions.Singleline)]
    private static partial Regex SingleLineRegex();

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
}
