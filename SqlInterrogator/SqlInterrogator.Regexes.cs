namespace SqlInterrogatorService;

using System.Text.RegularExpressions;

/// <summary>
/// Regex patterns and constants for SqlInterrogator.
/// </summary>
public static partial class SqlInterrogator
{
    // Regex group indices for clarity and maintainability
    private const int DatabaseGroupIndex = 1;
    private const int SecondaryDatabaseGroupIndex = 2;
    private const int TableGroupIndex = 3;
    private const int ColumnGroupIndex = 4;
    private const int FifthPartGroupIndex = 5;

    // Estimated average columns in SELECT statement for StringBuilder capacity
    private const int EstimatedColumnsPerQuery = 4;

    // Regex timeout in milliseconds to prevent ReDoS attacks and infinite loops
    private const int RegexTimeoutMilliseconds = 1000;

    // These regex patterns are generated at compile-time using the [GeneratedRegex] attribute
    // for optimal performance. The source generator creates the implementation automatically.
    // All patterns include a 1-second timeout to prevent ReDoS attacks and infinite loops.

    /// <summary>Matches single-line SQL comments (-- comment).</summary>
    [GeneratedRegex(@"--[^\r\n]*", RegexOptions.Multiline, matchTimeoutMilliseconds: RegexTimeoutMilliseconds)]
    private static partial Regex MultilineRegex();

    /// <summary>Matches multi-line SQL comments (/* comment */).</summary>
    [GeneratedRegex(@"/\*.*?\*/", RegexOptions.Singleline, matchTimeoutMilliseconds: RegexTimeoutMilliseconds)]
    private static partial Regex SingleLineRegex();

    /// <summary>Matches SELECT keyword at the start of a statement.</summary>
    [GeneratedRegex(@"^\s*SELECT\s+", RegexOptions.IgnoreCase, matchTimeoutMilliseconds: RegexTimeoutMilliseconds, "en-GB")]
    private static partial Regex IgnoreCaseRegex();

    /// <summary>Matches USE statement followed by GO on a new line.</summary>
    [GeneratedRegex(@"USE\s+\[?[\w\s]+\]?\s*;?\s*\r?\n\s*GO\s*", RegexOptions.IgnoreCase | RegexOptions.Multiline, matchTimeoutMilliseconds: RegexTimeoutMilliseconds)]
    private static partial Regex UseWithGoRegex();

    /// <summary>Matches USE statement with optional semicolon.</summary>
    [GeneratedRegex(@"USE\s+\[?[\w\s]+\]?\s*;?\s*", RegexOptions.IgnoreCase, matchTimeoutMilliseconds: RegexTimeoutMilliseconds)]
    private static partial Regex UseStatementRegex();

    /// <summary>Matches standalone GO batch separator.</summary>
    [GeneratedRegex(@"^\s*GO\s*$", RegexOptions.IgnoreCase | RegexOptions.Multiline, matchTimeoutMilliseconds: RegexTimeoutMilliseconds)]
    private static partial Regex StandaloneGoRegex();

    /// <summary>Extracts the SELECT clause between SELECT and FROM keywords.</summary>
    [GeneratedRegex(@"SELECT\s+(.*?)\s+FROM", RegexOptions.IgnoreCase | RegexOptions.Singleline, matchTimeoutMilliseconds: RegexTimeoutMilliseconds)]
    private static partial Regex SelectClauseRegex();

    /// <summary>Matches DISTINCT, ALL, or TOP keywords in SELECT clause.</summary>
    [GeneratedRegex(@"^\s*(?:DISTINCT|ALL|TOP\s+\d+)\s+", RegexOptions.IgnoreCase, matchTimeoutMilliseconds: RegexTimeoutMilliseconds)]
    private static partial Regex DistinctTopRegex();

    /// <summary>Matches SELECT * pattern.</summary>
    [GeneratedRegex(@"^\*$", RegexOptions.None, matchTimeoutMilliseconds: RegexTimeoutMilliseconds)]
    private static partial Regex SelectStarRegex();

    /// <summary>Matches explicit column aliases using AS keyword.</summary>
    [GeneratedRegex(@"^(.*?)\s+AS\s+(.+)$", RegexOptions.IgnoreCase, matchTimeoutMilliseconds: RegexTimeoutMilliseconds)]
    private static partial Regex ExplicitAliasRegex();

    /// <summary>Matches implicit aliases after qualified columns or functions.</summary>
    [GeneratedRegex(@"^(.*?[\.\)\]])\s+(\w+)$", RegexOptions.None, matchTimeoutMilliseconds: RegexTimeoutMilliseconds)]
    private static partial Regex ImplicitAliasRegex();

    /// <summary>Matches simple qualified column aliases (table.column alias).</summary>
    [GeneratedRegex(@"^(\w+\.\w+)\s+(\w+)$", RegexOptions.None, matchTimeoutMilliseconds: RegexTimeoutMilliseconds)]
    private static partial Regex SimpleQualifiedAliasRegex();

    /// <summary>Matches numeric or string literals.</summary>
    [GeneratedRegex(@"^\d+$|^'[^']*'$", RegexOptions.IgnoreCase, matchTimeoutMilliseconds: RegexTimeoutMilliseconds)]
    private static partial Regex LiteralRegex();

    /// <summary>Matches SQL function calls including window functions and common SQL functions.</summary>
    [GeneratedRegex(@"(ROW_NUMBER|RANK|DENSE_RANK|NTILE|LEAD|LAG|FIRST_VALUE|LAST_VALUE|PERCENT_RANK|CUME_DIST|COUNT|SUM|AVG|MIN|MAX|CAST|CONVERT|COALESCE|ISNULL|CONCAT|SUBSTRING|UPPER|LOWER|LTRIM|RTRIM|LEFT|RIGHT|DATEADD|DATEDIFF|DATEPART|GETDATE|\w+)\s*\(", RegexOptions.IgnoreCase, matchTimeoutMilliseconds: RegexTimeoutMilliseconds)]
    private static partial Regex FunctionRegex();

    /// <summary>Matches Common Table Expressions (WITH ... AS (...)).</summary>
    [GeneratedRegex(@"^\s*WITH\s+.*?\)\s*(?=SELECT)", RegexOptions.IgnoreCase | RegexOptions.Singleline, matchTimeoutMilliseconds: RegexTimeoutMilliseconds)]
    private static partial Regex CtePatternRegex();

    /// <summary>Matches SELECT keyword embedded in column expressions (subqueries).</summary>
    [GeneratedRegex(@"SELECT\s+", RegexOptions.IgnoreCase, matchTimeoutMilliseconds: RegexTimeoutMilliseconds)]
    private static partial Regex SubqueryDetectionRegex();

    /// <summary>Matches four-part identifier prefix pattern (word. or [word].) before FROM/JOIN keywords.</summary>
    [GeneratedRegex(@"(\w+|\[[^\]]+\])\.\s*$", RegexOptions.None, matchTimeoutMilliseconds: RegexTimeoutMilliseconds)]
    private static partial Regex FourPartIdentifierPrefixRegex();

    /// <summary>Matches whitespace for normalization.</summary>
    [GeneratedRegex(@"\s+", RegexOptions.None, matchTimeoutMilliseconds: RegexTimeoutMilliseconds)]
    private static partial Regex WhitespaceNormalizationRegex();

    /// <summary>Matches WHERE clause extraction pattern.</summary>
    [GeneratedRegex(@"\bWHERE\b\s+(.*?)(?:\bORDER\s+BY\b|\bGROUP\s+BY\b|\bHAVING\b|\bUNION\b|;|$)", RegexOptions.IgnoreCase | RegexOptions.Singleline, matchTimeoutMilliseconds: RegexTimeoutMilliseconds)]
    private static partial Regex WhereClauseRegex();

    /// <summary>Matches comparison operators in WHERE conditions.</summary>
    [GeneratedRegex(@"([\w\[\]\.""]+)\s*(>=|<=|!=|<>|>|<|=|LIKE|IN|IS\s+NOT|IS|NOT\s+IN|NOT\s+LIKE)\s*([\w\[\]\.""]+|'[^']*'|\([^)]+\))?", RegexOptions.IgnoreCase, matchTimeoutMilliseconds: RegexTimeoutMilliseconds)]
    private static partial Regex WhereConditionRegex();

    // Bracketed three-part identifier [db].[schema].[table]
    // Example: [MyDB].[dbo].[Users], [DB1].[sys].[tables]
    // Negative lookahead (?!\.\[) ensures we don't match part of a four-part identifier
    private const string BracketedThreePartPattern =
          @"(?:FROM|JOIN|INTO|UPDATE|TABLE|MERGE|USING)\s+\[([^\]]+)\]\.\[([^\]]+)\]\.\[([^\]]+)\](?!\.\[)";

    // Mixed bracketed/unbracketed [db].schema.table or db.[schema].[table]
    // Example: [MyDB].dbo.Users, MyDB.[dbo].[Users]
    private const string MixedThreePartPattern1 =
        @"(?:FROM|JOIN|INTO|UPDATE|TABLE|MERGE|USING)\s+\[([^\]]+)\]\.(?:\[?[^\]\.\s]+\]?)\.(?:\[?[^\]\.\s\(]+\]?)(?!\.)";
    private const string MixedThreePartPattern2 =
        @"(?:FROM|JOIN|INTO|UPDATE|TABLE|MERGE|USING)\s+(\w+)\.\[([^\]]+)\]\.\[([^\]]+)\](?!\.\[)";

    // Unbracketed three-part identifier db.schema.table
    // Example: MyDB.dbo.Users, Database1.sys.objects
    // Negative lookahead (?!\.) ensures we don't match part of a four-part identifier
    private const string UnbracketedThreePartPattern =
@"(?:FROM|JOIN|INTO|UPDATE|TABLE|MERGE|USING)\s+(\w+)\.(\w+)\.(\w+)(?!\.)";

    // Double-quoted identifiers "db"."schema"."table"
    // Example: "MyDB"."dbo"."Users"
    private const string DoubleQuotedThreePartPattern =
        @"(?:FROM|JOIN|INTO|UPDATE|TABLE|MERGE|USING)\s+""([^""]+)""\.""([^""]+)""\.""([^""]+)""";

    // Mixed quoted and bracketed "db".[schema].[table]
    // Example: "MyDB".[dbo].[Users], "MyDB".dbo.Users
    private const string MixedQuotedBracketedPattern =
        @"(?:FROM|JOIN|INTO|UPDATE|TABLE|MERGE|USING)\s+""([^""]+)""\.(?:\[?[^\]\.\s]+\]?)\.(?:\[?[^\]\.\s\(]+\]?)";

    // Server.database.schema.table (four-part names) - extract database (2nd part)
    // Example: [Server1].[MyDB].[dbo].[Users], Server1.MyDB.dbo.Users  
    // Uses non-capturing group for server, captures database
    private const string BracketedFourPartPattern =
        @"(?:FROM|JOIN|INTO|UPDATE|TABLE|MERGE|USING)\s+\[(?:[^\]]+)\]\.\[([^\]]+)\]\.\[([^\]]+)\]\.\[([^\]]+)\]";
    private const string UnbracketedFourPartPattern =
        @"(?:FROM|JOIN|INTO|UPDATE|TABLE|MERGE|USING)\s+(?:\w+)\.(\w+)\.(\w+)\.(\w+)(?!\.)";

    // Table hints WITH (NOLOCK) etc
    // Example: [MyDB].[dbo].[Users] WITH (NOLOCK), MyDB.dbo.Users AS u
    private const string BracketedWithHintPattern =
        @"(?:FROM|JOIN|INTO|UPDATE|TABLE|MERGE|USING)\s+\[([^\]]+)\]\.\[([^\]]+)\]\.\[([^\]]+)\]\s+(?:WITH|AS)";
    private const string UnbracketedWithHintPattern =
        @"(?:FROM|JOIN|INTO|UPDATE|TABLE|MERGE|USING)\s+(\w+)\.(\w+)\.(\w+)\s+(?:WITH|AS)";
}
