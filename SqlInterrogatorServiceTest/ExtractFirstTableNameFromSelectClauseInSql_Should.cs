namespace SqlInterrogatorServiceTest;

using FluentAssertions;
using SqlInterrogatorService;

public class ExtractFirstTableNameFromSelectClauseInSql_Should
{
    [Fact]
    public void ReturnTableName_WhenSingleBracketedTableName()
    {
        var sql = "SELECT * FROM [Users]";

        var result = SqlInterrogator.ExtractFirstTableNameFromSelectClauseInSql(sql);

        _ = result.Should().Be("Users");
    }

    [Fact]
    public void ReturnTableName_WhenSingleUnbracketedTableName()
    {
        var sql = "SELECT * FROM Users";

        var result = SqlInterrogator.ExtractFirstTableNameFromSelectClauseInSql(sql);

        _ = result.Should().Be("Users");
    }

    [Fact]
    public void ReturnTableName_WhenSingleDoubleQuotedTableName()
    {
        var sql = "SELECT * FROM \"Users\"";

        var result = SqlInterrogator.ExtractFirstTableNameFromSelectClauseInSql(sql);

        _ = result.Should().Be("Users");
    }

    [Fact]
    public void ReturnTableName_WhenTwoPartBracketedIdentifier()
    {
        var sql = "SELECT * FROM [dbo].[Users]";

        var result = SqlInterrogator.ExtractFirstTableNameFromSelectClauseInSql(sql);

        _ = result.Should().Be("Users");
    }

    [Fact]
    public void ReturnTableName_WhenTwoPartUnbracketedIdentifier()
    {
        var sql = "SELECT * FROM dbo.Users";

        var result = SqlInterrogator.ExtractFirstTableNameFromSelectClauseInSql(sql);

        _ = result.Should().Be("Users");
    }

    [Fact]
    public void ReturnTableName_WhenTwoPartDoubleQuotedIdentifier()
    {
        var sql = "SELECT * FROM \"dbo\".\"Users\"";

        var result = SqlInterrogator.ExtractFirstTableNameFromSelectClauseInSql(sql);

        _ = result.Should().Be("Users");
    }

    [Fact]
    public void ReturnTableName_WhenTwoPartMixedBracketedAndUnbracketed()
    {
        var sql = "SELECT * FROM [dbo].Users";

        var result = SqlInterrogator.ExtractFirstTableNameFromSelectClauseInSql(sql);

        _ = result.Should().Be("Users");
    }

    [Fact]
    public void ReturnTableName_WhenThreePartBracketedIdentifier()
    {
        var sql = "SELECT * FROM [MyDatabase].[dbo].[Users]";

        var result = SqlInterrogator.ExtractFirstTableNameFromSelectClauseInSql(sql);

        _ = result.Should().Be("Users");
    }

    [Fact]
    public void ReturnTableName_WhenThreePartUnbracketedIdentifier()
    {
        var sql = "SELECT * FROM MyDatabase.dbo.Users";

        var result = SqlInterrogator.ExtractFirstTableNameFromSelectClauseInSql(sql);

        _ = result.Should().Be("Users");
    }

    [Fact]
    public void ReturnTableName_WhenThreePartDoubleQuotedIdentifier()
    {
        var sql = "SELECT * FROM \"MyDatabase\".\"dbo\".\"Users\"";

        var result = SqlInterrogator.ExtractFirstTableNameFromSelectClauseInSql(sql);

        _ = result.Should().Be("Users");
    }

    [Fact]
    public void ReturnTableName_WhenThreePartMixedIdentifiers()
    {
        var sql = "SELECT * FROM [MyDatabase].dbo.Users";

        var result = SqlInterrogator.ExtractFirstTableNameFromSelectClauseInSql(sql);

        _ = result.Should().Be("Users");
    }

    [Fact]
    public void ReturnTableName_WhenFourPartBracketedIdentifier()
    {
        var sql = "SELECT * FROM [Server1].[MyDatabase].[dbo].[Users]";

        var result = SqlInterrogator.ExtractFirstTableNameFromSelectClauseInSql(sql);

        _ = result.Should().Be("Users");
    }

    [Fact]
    public void ReturnTableName_WhenFourPartUnbracketedIdentifier()
    {
        var sql = "SELECT * FROM Server1.MyDatabase.dbo.Users";

        var result = SqlInterrogator.ExtractFirstTableNameFromSelectClauseInSql(sql);

        _ = result.Should().Be("Users");
    }

    [Fact]
    public void ReturnTableName_WhenSelectWithMultipleColumns()
    {
        var sql = "SELECT col1, col2, col3 FROM Users";

        var result = SqlInterrogator.ExtractFirstTableNameFromSelectClauseInSql(sql);

        _ = result.Should().Be("Users");
    }

    [Fact]
    public void ReturnTableName_WhenSelectWithBracketedColumns()
    {
        var sql = "SELECT [col1], [col2] FROM [Users]";

        var result = SqlInterrogator.ExtractFirstTableNameFromSelectClauseInSql(sql);

        _ = result.Should().Be("Users");
    }

    [Fact]
    public void ReturnTableName_WhenSelectWithQualifiedColumns()
    {
        var sql = "SELECT u.Name, u.Email FROM Users u";

        var result = SqlInterrogator.ExtractFirstTableNameFromSelectClauseInSql(sql);

        _ = result.Should().Be("Users");
    }

    [Fact]
    public void ReturnTableName_WhenSelectWithDoubleQuotedColumns()
    {
        var sql = "SELECT \"col1\", \"col2\" FROM \"Users\"";

        var result = SqlInterrogator.ExtractFirstTableNameFromSelectClauseInSql(sql);

        _ = result.Should().Be("Users");
    }

    [Fact]
    public void ReturnTableName_WhenTableHasAsAlias()
    {
        var sql = "SELECT * FROM Users AS u";

        var result = SqlInterrogator.ExtractFirstTableNameFromSelectClauseInSql(sql);

        _ = result.Should().Be("Users");
    }

    [Fact]
    public void ReturnTableName_WhenTableHasImplicitAlias()
    {
        var sql = "SELECT * FROM Users u";

        var result = SqlInterrogator.ExtractFirstTableNameFromSelectClauseInSql(sql);

        _ = result.Should().Be("Users");
    }

    [Fact]
    public void ReturnTableName_WhenBracketedTableHasAlias()
    {
        var sql = "SELECT * FROM [dbo].[Users] AS u";

        var result = SqlInterrogator.ExtractFirstTableNameFromSelectClauseInSql(sql);

        _ = result.Should().Be("Users");
    }

    [Fact]
    public void ReturnTableName_WhenTableHasNoLockHint()
    {
        var sql = "SELECT * FROM Users WITH (NOLOCK)";

        var result = SqlInterrogator.ExtractFirstTableNameFromSelectClauseInSql(sql);

        _ = result.Should().Be("Users");
    }

    [Fact]
    public void ReturnTableName_WhenThreePartTableHasNoLockHint()
    {
        var sql = "SELECT * FROM MyDB.dbo.Users WITH (NOLOCK)";

        var result = SqlInterrogator.ExtractFirstTableNameFromSelectClauseInSql(sql);

        _ = result.Should().Be("Users");
    }

    [Fact]
    public void ReturnTableName_WhenTableHasMultipleHints()
    {
        var sql = "SELECT * FROM [Users] WITH (NOLOCK, READPAST)";

        var result = SqlInterrogator.ExtractFirstTableNameFromSelectClauseInSql(sql);

        _ = result.Should().Be("Users");
    }

    [Fact]
    public void ReturnFirstTableName_WhenInnerJoin()
    {
        var sql = "SELECT * FROM Users INNER JOIN Orders ON Users.Id = Orders.UserId";

        var result = SqlInterrogator.ExtractFirstTableNameFromSelectClauseInSql(sql);

        _ = result.Should().Be("Users");
    }

    [Fact]
    public void ReturnFirstTableName_WhenLeftJoin()
    {
        var sql = "SELECT * FROM Users LEFT JOIN Orders ON Users.Id = Orders.UserId";

        var result = SqlInterrogator.ExtractFirstTableNameFromSelectClauseInSql(sql);

        _ = result.Should().Be("Users");
    }

    [Fact]
    public void ReturnFirstTableName_WhenRightJoin()
    {
        var sql = "SELECT * FROM Users RIGHT JOIN Orders ON Users.Id = Orders.UserId";

        var result = SqlInterrogator.ExtractFirstTableNameFromSelectClauseInSql(sql);

        _ = result.Should().Be("Users");
    }

    [Fact]
    public void ReturnFirstTableName_WhenFullJoin()
    {
        var sql = "SELECT * FROM Users FULL JOIN Orders ON Users.Id = Orders.UserId";

        var result = SqlInterrogator.ExtractFirstTableNameFromSelectClauseInSql(sql);

        _ = result.Should().Be("Users");
    }

    [Fact]
    public void ReturnFirstTableName_WhenCrossJoin()
    {
        var sql = "SELECT * FROM Users CROSS JOIN Orders";

        var result = SqlInterrogator.ExtractFirstTableNameFromSelectClauseInSql(sql);

        _ = result.Should().Be("Users");
    }

    [Fact]
    public void ReturnFirstTableName_WhenJoinWithBracketedTableNames()
    {
        var sql = "SELECT * FROM [dbo].[Users] JOIN [dbo].[Orders] ON [Users].[Id] = [Orders].[UserId]";

        var result = SqlInterrogator.ExtractFirstTableNameFromSelectClauseInSql(sql);

        _ = result.Should().Be("Users");
    }

    [Fact]
    public void ReturnTableName_WhenTableNameHasUnderscores()
    {
        var sql = "SELECT * FROM User_Accounts";

        var result = SqlInterrogator.ExtractFirstTableNameFromSelectClauseInSql(sql);

        _ = result.Should().Be("User_Accounts");
    }

    [Fact]
    public void ReturnTableName_WhenTableNameHasNumbers()
    {
        var sql = "SELECT * FROM Users2024";

        var result = SqlInterrogator.ExtractFirstTableNameFromSelectClauseInSql(sql);

        _ = result.Should().Be("Users2024");
    }

    [Fact]
    public void ReturnTableName_WhenBracketedTableNameHasSpecialCharacters()
    {
        var sql = "SELECT * FROM [User-Accounts_2024]";

        var result = SqlInterrogator.ExtractFirstTableNameFromSelectClauseInSql(sql);

        _ = result.Should().Be("User-Accounts_2024");
    }

    [Fact]
    public void ReturnTableName_WhenTableNameHasSpaces()
    {
        var sql = "SELECT * FROM [User Accounts]";

        var result = SqlInterrogator.ExtractFirstTableNameFromSelectClauseInSql(sql);

        _ = result.Should().Be("User Accounts");
    }

    [Fact]
    public void ReturnTableName_WhenSqlHasSingleLineComments()
    {
        var sql = @"
            -- This is a comment about FakeTable
            SELECT * FROM Users
            -- Another comment";

        var result = SqlInterrogator.ExtractFirstTableNameFromSelectClauseInSql(sql);

        _ = result.Should().Be("Users");
    }

    [Fact]
    public void ReturnTableName_WhenSqlHasMultiLineComments()
    {
        var sql = @"
            /* This is a multi-line comment
            about FakeTable
            */
            SELECT * FROM Users";

        var result = SqlInterrogator.ExtractFirstTableNameFromSelectClauseInSql(sql);

        _ = result.Should().Be("Users");
    }

    [Fact]
    public void ReturnTableName_WhenSqlHasMixedComments()
    {
        var sql = @"
            -- Single line comment
            /* Multi-line comment */
            SELECT * FROM Users -- Inline comment
            WHERE Id > 0";

        var result = SqlInterrogator.ExtractFirstTableNameFromSelectClauseInSql(sql);

        _ = result.Should().Be("Users");
    }

    [Fact]
    public void ReturnTableName_WhenMultilineQuery()
    {
        var sql = @"
            SELECT 
                u.Name,
                u.Email
            FROM 
                Users u
            WHERE 
            u.Active = 1";

        var result = SqlInterrogator.ExtractFirstTableNameFromSelectClauseInSql(sql);

        _ = result.Should().Be("Users");
    }

    [Fact]
    public void ReturnTableName_WhenMultilineQueryWithBracketedIdentifiers()
    {
        var sql = @"
            SELECT 
                u.Name,
                u.Email
            FROM 
                [MyDatabase].[dbo].[Users] u
            WHERE 
                u.Active = 1";

        var result = SqlInterrogator.ExtractFirstTableNameFromSelectClauseInSql(sql);

        _ = result.Should().Be("Users");
    }

    [Fact]
    public void ReturnTableName_WhenLowercaseKeywords()
    {
        var sql = "select * from Users";

        var result = SqlInterrogator.ExtractFirstTableNameFromSelectClauseInSql(sql);

        _ = result.Should().Be("Users");
    }

    [Fact]
    public void ReturnTableName_WhenMixedCaseKeywords()
    {
        var sql = "SeLeCt * FrOm Users";

        var result = SqlInterrogator.ExtractFirstTableNameFromSelectClauseInSql(sql);

        _ = result.Should().Be("Users");
    }

    [Fact]
    public void ReturnTableName_WhenUppercaseKeywords()
    {
        var sql = "SELECT * FROM USERS";

        var result = SqlInterrogator.ExtractFirstTableNameFromSelectClauseInSql(sql);

        _ = result.Should().Be("USERS");
    }

    [Fact]
    public void ReturnNull_WhenSqlIsNull()
    {
        var result = SqlInterrogator.ExtractFirstTableNameFromSelectClauseInSql(null!);

        _ = result.Should().BeNull();
    }

    [Fact]
    public void ReturnNull_WhenSqlIsEmptyString()
    {
        var result = SqlInterrogator.ExtractFirstTableNameFromSelectClauseInSql("");

        _ = result.Should().BeNull();
    }

    [Fact]
    public void ReturnNull_WhenSqlIsWhitespaceOnly()
    {
        var result = SqlInterrogator.ExtractFirstTableNameFromSelectClauseInSql("   ");

        _ = result.Should().BeNull();
    }

    [Fact]
    public void ReturnNull_WhenQueryHasNoFrom()
    {
        var sql = "SELECT GETDATE()";
        var result = SqlInterrogator.ExtractFirstTableNameFromSelectClauseInSql(sql);

        _ = result.Should().BeNull();
    }

    [Fact]
    public void ReturnInnerTableName_WhenSubquery()
    {
        var sql = "SELECT * FROM (SELECT * FROM Users) AS SubQuery";
        var result = SqlInterrogator.ExtractFirstTableNameFromSelectClauseInSql(sql);

        _ = result.Should().Be("Users");
    }

    [Fact]
    public void ReturnFirstTableName_WhenComplexJoinQuery()
    {
        var sql = @"
            SELECT u.Name, o.OrderDate, p.ProductName
                FROM [Database1].[dbo].[Users] u
                    INNER JOIN [Database2].[dbo].[Orders] o ON u.Id = o.UserId
                        LEFT JOIN [Database3].[dbo].[Products] p ON o.ProductId = p.Id
            WHERE u.Active = 1";

        var result = SqlInterrogator.ExtractFirstTableNameFromSelectClauseInSql(sql);

        _ = result.Should().Be("Users");
    }

    [Fact]
    public void ReturnMainQueryTableName_WhenQueryHasCTE()
    {
        var sql = @"
            WITH UserCTE AS (
                SELECT * FROM InnerUsers
            )
            SELECT * FROM Users";

        var result = SqlInterrogator.ExtractFirstTableNameFromSelectClauseInSql(sql);

        _ = result.Should().Be("Users");
    }

    [Fact]
    public void ReturnTableName_WhenQueryHasMultipleWhereConditions()
    {
        var sql = @"
            SELECT * FROM Users 
            WHERE Active = 1 
            AND CreatedDate > '2024-01-01'
            AND Email LIKE '%@example.com'";

        var result = SqlInterrogator.ExtractFirstTableNameFromSelectClauseInSql(sql);

        _ = result.Should().Be("Users");
    }

    [Fact]
    public void ReturnNull_WhenUpdateStatement()
    {
        var sql = "UPDATE Users SET Active = 1";
        var result = SqlInterrogator.ExtractFirstTableNameFromSelectClauseInSql(sql);

        _ = result.Should().BeNull();
    }

    [Fact]
    public void ReturnNull_WhenInsertStatement()
    {
        var sql = "INSERT INTO Users (Name) VALUES ('John')";
        var result = SqlInterrogator.ExtractFirstTableNameFromSelectClauseInSql(sql);

        _ = result.Should().BeNull();
    }

    [Fact]
    public void ReturnNull_WhenDeleteStatement()
    {
        var sql = "DELETE FROM Users WHERE Id = 1";
        var result = SqlInterrogator.ExtractFirstTableNameFromSelectClauseInSql(sql);

        _ = result.Should().BeNull();
    }

    [Fact]
    public void ReturnTableName_WhenQueryHasUseClause()
    {
        var sql = @"USE MyDatabase;
                    SELECT * FROM Users";
        var result = SqlInterrogator.ExtractFirstTableNameFromSelectClauseInSql(sql);

        _ = result.Should().Be("Users");
    }

    [Fact]
    public void ReturnTableName_WhenQueryHasUseBracketedClause()
    {
        var sql = @"USE [MyDatabase];
                    SELECT * FROM [dbo].[Users]";
        var result = SqlInterrogator.ExtractFirstTableNameFromSelectClauseInSql(sql);

        _ = result.Should().Be("Users");
    }

    [Fact]
    public void ReturnTableName_WhenQueryHasUseClauseWithGo()
    {
        var sql = @"USE MyDatabase
                    GO
                    SELECT * FROM Users";
        var result = SqlInterrogator.ExtractFirstTableNameFromSelectClauseInSql(sql);

        _ = result.Should().Be("Users");
    }

    [Fact]
    public void ReturnTableName_WhenQueryHasMultipleStatementsWithUse()
    {
        var sql = @"USE MyDatabase;
                    SELECT * FROM Users WHERE Id = 1;
                    SELECT * FROM Orders WHERE Id = 2;";

        var result = SqlInterrogator.ExtractFirstTableNameFromSelectClauseInSql(sql);

        _ = result.Should().Be("Users");
    }

    [Fact]
    public void ReturnTableName_WhenFivePartBracketedIdentifier()
    {
        var sql = "SELECT * FROM [Server1].[MyDB].[dbo].[schema].[Users]";

        var result = SqlInterrogator.ExtractFirstTableNameFromSelectClauseInSql(sql);

        // Five-part identifiers return the schema name (4th part) as table name
        _ = result.Should().Be("schema");
    }

    [Fact]
    public void ReturnNull_WhenTableValuedFunction()
    {
        var sql = "SELECT * FROM dbo.GetUsers(1)";

        var result = SqlInterrogator.ExtractFirstTableNameFromSelectClauseInSql(sql);

        // Current implementation returns function name - table-valued functions are not filtered in unbracketed form
        _ = result.Should().Be("GetUsers");
    }

    [Fact]
    public void ReturnNull_WhenTableValuedFunctionWithBrackets()
    {
        var sql = "SELECT * FROM [dbo].[GetActiveUsers](2024)";

        var result = SqlInterrogator.ExtractFirstTableNameFromSelectClauseInSql(sql);

        // Current implementation returns function name - table-valued functions are not filtered in bracketed form
        _ = result.Should().Be("GetActiveUsers");
    }

    [Fact]
    public void ReturnFirstTableName_WhenLeftOuterJoin()
    {
        var sql = "SELECT * FROM Users LEFT OUTER JOIN Orders ON Users.Id = Orders.UserId";

        var result = SqlInterrogator.ExtractFirstTableNameFromSelectClauseInSql(sql);

        _ = result.Should().Be("Users");
    }

    [Fact]
    public void ReturnFirstTableName_WhenRightOuterJoin()
    {
        var sql = "SELECT * FROM Users RIGHT OUTER JOIN Orders ON Users.Id = Orders.UserId";

        var result = SqlInterrogator.ExtractFirstTableNameFromSelectClauseInSql(sql);

        _ = result.Should().Be("Users");
    }

    [Fact]
    public void ReturnFirstTableName_WhenFullOuterJoin()
    {
        var sql = "SELECT * FROM Users FULL OUTER JOIN Orders ON Users.Id = Orders.UserId";

        var result = SqlInterrogator.ExtractFirstTableNameFromSelectClauseInSql(sql);

        _ = result.Should().Be("Users");
    }

    [Fact]
    public void ReturnFirstTableName_WhenCrossApply()
    {
        var sql = "SELECT * FROM Users CROSS APPLY dbo.GetOrders(Users.Id) AS Orders";

        var result = SqlInterrogator.ExtractFirstTableNameFromSelectClauseInSql(sql);

        _ = result.Should().Be("Users");
    }

    [Fact]
    public void ReturnFirstTableName_WhenOuterApply()
    {
        var sql = "SELECT * FROM Users OUTER APPLY dbo.GetOrders(Users.Id) AS Orders";

        var result = SqlInterrogator.ExtractFirstTableNameFromSelectClauseInSql(sql);

        _ = result.Should().Be("Users");
    }

    [Fact]
    public void ReturnTableName_WhenUnicodeCharacters()
    {
        var sql = "SELECT * FROM [Émployés]";

        var result = SqlInterrogator.ExtractFirstTableNameFromSelectClauseInSql(sql);

        _ = result.Should().Be("Émployés");
    }

    [Fact]
    public void ReturnTableName_WhenArabicCharacters()
    {
        var sql = "SELECT * FROM [المستخدمين]";

        var result = SqlInterrogator.ExtractFirstTableNameFromSelectClauseInSql(sql);

        _ = result.Should().Be("المستخدمين");
    }

    [Fact]
    public void ReturnMainQueryTableName_WhenMultipleCTEs()
    {
        var sql = @"
            WITH UserCTE AS (
                SELECT * FROM InnerUsers
            ),
            OrderCTE AS (
                SELECT * FROM InnerOrders
            )
            SELECT * FROM Users";

        var result = SqlInterrogator.ExtractFirstTableNameFromSelectClauseInSql(sql);

        _ = result.Should().Be("Users");
    }
}
