namespace SqlInterrogatorServiceTest;

using FluentAssertions;
using SqlInterrogatorService;

public class ExtractDatabaseNamesFromSql_Should
{
    [Fact]
    public void HandleBracketedIdentifiers()
    {
        var sql = "SELECT * FROM [MyDatabase].[dbo].[Users]";

        var result = SqlInterrogator.ExtractDatabaseNamesFromSql(sql);

        _ = result.Should().ContainSingle().Which.Should().Be("MyDatabase");
    }

    [Fact]
    public void HandleUnbracketedIdentifiers()
    {
        var sql = "SELECT * FROM MyDatabase.dbo.Users";

        var result = SqlInterrogator.ExtractDatabaseNamesFromSql(sql);

        _ = result.Should().ContainSingle().Which.Should().Be("MyDatabase");
    }

    [Fact]
    public void HandleMixedBracketedAndUnbracketed()
    {
        var sql = "SELECT * FROM [MyDatabase].dbo.Users";

        var result = SqlInterrogator.ExtractDatabaseNamesFromSql(sql);

        _ = result.Should().ContainSingle().Which.Should().Be("MyDatabase");
    }

    [Fact]
    public void HandleDoubleQuotedIdentifiers()
    {
        var sql = "SELECT * FROM \"MyDatabase\".\"dbo\".\"Users\"";

        var result = SqlInterrogator.ExtractDatabaseNamesFromSql(sql);

        _ = result.Should().ContainSingle().Which.Should().Be("MyDatabase");
    }

    [Fact]
    public void HandleJoinClauses()
    {
        var sql = @"
            SELECT * FROM [DB1].[dbo].[Users] u
            JOIN [DB2].[dbo].[Orders] o ON u.Id = o.UserId
            LEFT JOIN [DB3].[dbo].[Products] p ON o.ProductId = p.Id";

        var result = SqlInterrogator.ExtractDatabaseNamesFromSql(sql);

        _ = result.Should().HaveCount(3);
        _ = result.Should().Contain(["DB1", "DB2", "DB3"]);
    }

    [Fact]
    public void HandleAllJoinTypes()
    {
        var sql = @"
         SELECT * FROM [DB1].[dbo].[Table1]
            INNER JOIN [DB2].[dbo].[Table2] ON 1=1
                LEFT JOIN [DB3].[dbo].[Table3] ON 1=1
                    RIGHT JOIN [DB4].[dbo].[Table4] ON 1=1
                        FULL JOIN [DB5].[dbo].[Table5] ON 1=1
                            CROSS JOIN [DB6].[dbo].[Table6]";

        var result = SqlInterrogator.ExtractDatabaseNamesFromSql(sql);

        _ = result.Should().HaveCount(6);
    }

    [Fact]
    public void HandleTableAliases()
    {
        var sql = @"
                SELECT * FROM [MyDatabase].[dbo].[Users] AS u
                WHERE u.Id > 0";

        var result = SqlInterrogator.ExtractDatabaseNamesFromSql(sql);

        _ = result.Should().ContainSingle().Which.Should().Be("MyDatabase");
    }

    [Fact]
    public void HandleTableHintsWithNoLock()
    {
        var sql = "SELECT * FROM [MyDB].[dbo].[Users] WITH (NOLOCK)";

        var result = SqlInterrogator.ExtractDatabaseNamesFromSql(sql);

        _ = result.Should().ContainSingle().Which.Should().Be("MyDB");
    }

    [Fact]
    public void HandleMultipleTablesWithSameDatabase()
    {
        var sql = @"
            SELECT * FROM [MyDB].[dbo].[Users]
            JOIN [MyDB].[dbo].[Orders] ON 1=1";

        var result = SqlInterrogator.ExtractDatabaseNamesFromSql(sql);

        _ = result.Should().ContainSingle().Which.Should().Be("MyDB");
    }

    [Fact]
    public void HandleComplexQueryWithMultipleDatabases()
    {
        var sql = @"
                WITH UserCTE AS (
                SELECT * FROM [DB1].[dbo].[Users])
                     SELECT u.*, o.*, p.*
                FROM UserCTE u
                 INNER JOIN [DB2].[dbo].[Orders] o ON u.Id = o.UserId
                     LEFT OUTER JOIN [DB3].[dbo].[Products] p ON o.ProductId = p.Id
                WHERE u.Active = 1";

        var result = SqlInterrogator.ExtractDatabaseNamesFromSql(sql);

        _ = result.Should().HaveCount(3);
        _ = result.Should().Contain(["DB1", "DB2", "DB3"]);
    }

    [Fact]
    public void IgnoreSqlComments()
    {
        var sql = @"
       -- This is a comment about [FakeDB].[dbo].[FakeTable]
            SELECT * FROM [RealDB].[dbo].[RealTable]
          /* Multi-line comment
            with [AnotherFakeDB].[dbo].[AnotherFakeTable]
          */";

        var result = SqlInterrogator.ExtractDatabaseNamesFromSql(sql);

        _ = result.Should().ContainSingle().Which.Should().Be("RealDB");
    }

    [Fact]
    public void HandleCaseInsensitiveKeywords()
    {
        var sql = @"
            select * from [DB1].[dbo].[Table1]
            INNER join [DB2].[dbo].[Table2] on 1=1
            Update [DB3].[dbo].[Table3] set Col = 1";

        var result = SqlInterrogator.ExtractDatabaseNamesFromSql(sql);

        _ = result.Should().HaveCount(3);
    }

    [Fact]
    public void HandleEmptyOrNullSql()
    {
        var result1 = SqlInterrogator.ExtractDatabaseNamesFromSql(null!);
        var result2 = SqlInterrogator.ExtractDatabaseNamesFromSql("");
        var result3 = SqlInterrogator.ExtractDatabaseNamesFromSql("   ");

        _ = result1.Should().BeEmpty();
        _ = result2.Should().BeEmpty();
        _ = result3.Should().BeEmpty();
    }

    [Fact]
    public void HandleDatabaseNamesWithSpecialCharacters()
    {
        var sql = "SELECT * FROM [My-Database_2024].[dbo].[Users]";

        var result = SqlInterrogator.ExtractDatabaseNamesFromSql(sql);

        _ = result.Should().ContainSingle().Which.Should().Be("My-Database_2024");
    }

    [Fact]
    public void HandleMultilineQueries()
    {
        var sql = @"
          SELECT 
            u.Name,
                o.OrderDate
          FROM 
            [Database1].[dbo].[Users] u
            INNER JOIN 
                [Database2].[dbo].[Orders] o 
            ON 
                u.Id = o.UserId";

        var result = SqlInterrogator.ExtractDatabaseNamesFromSql(sql);

        _ = result.Should().HaveCount(2);
        _ = result.Should().Contain(["Database1", "Database2"]);
    }

    [Fact]
    public void HandleMergeStatement()
    {
        var sql = "MERGE INTO [TargetDB].[dbo].[Users] AS target " +
                  "USING [SourceDB].[dbo].[TempUsers] AS source " +
                  "ON target.Id = source.Id";

        var result = SqlInterrogator.ExtractDatabaseNamesFromSql(sql);

        _ = result.Should().Contain("TargetDB");
        _ = result.Should().Contain("SourceDB");
        _ = result.Should().HaveCount(2);
    }

    [Fact]
    public void HandleMergeWithInsertUpdate()
    {
        var sql = @"MERGE MyDB.dbo.Users AS target
                    USING TempDB.dbo.TempUsers AS source
                        ON target.Id = source.Id
                    WHEN MATCHED THEN UPDATE SET target.Name = source.Name
                    WHEN NOT MATCHED THEN INSERT (Id, Name) VALUES (source.Id, source.Name);";

        var result = SqlInterrogator.ExtractDatabaseNamesFromSql(sql);

        _ = result.Should().Contain("MyDB");
        _ = result.Should().Contain("TempDB");
    }

    [Fact]
    public void HandleMergeWithBracketedIdentifiers()
    {
        var sql = @"MERGE [Database1].[dbo].[Target] t
                    USING [Database2].[dbo].[Source] s
                        ON t.Key = s.Key
                    WHEN MATCHED THEN UPDATE SET t.Value = s.Value;";

        var result = SqlInterrogator.ExtractDatabaseNamesFromSql(sql);

        _ = result.Should().Contain("Database1");
        _ = result.Should().Contain("Database2");
        _ = result.Should().HaveCount(2);
    }

    [Fact]
    public void HandleJapaneseCharacters()
    {
        var sql = "SELECT * FROM [データベース].[dbo].[ユーザー]";

        var result = SqlInterrogator.ExtractDatabaseNamesFromSql(sql);

        _ = result.Should().Contain("データベース");
    }

    [Fact]
    public void HandleChineseCharacters()
    {
        var sql = "SELECT * FROM [数据库].[dbo].[用户表]";

        var result = SqlInterrogator.ExtractDatabaseNamesFromSql(sql);

        _ = result.Should().Contain("数据库");
    }

    [Fact]
    public void HandleMixedUnicodeAndAscii()
    {
        var sql = "SELECT * FROM [MyDB_データベース].[dbo].[Users_用户]";

        var result = SqlInterrogator.ExtractDatabaseNamesFromSql(sql);

        _ = result.Should().Contain("MyDB_データベース");
    }

    [Fact]
    public void HandleFivePartIdentifier()
    {
        var sql = "SELECT * FROM [Server1].[MyDB].[dbo].[schema].[Users]";

        var result = SqlInterrogator.ExtractDatabaseNamesFromSql(sql);

        _ = result.Should().Contain("Server1");
    }

    [Fact]
    public void HandleInsertIntoStatement()
    {
        var sql = "INSERT INTO [MyDB].[dbo].[Users] (Name) VALUES ('John')";

        var result = SqlInterrogator.ExtractDatabaseNamesFromSql(sql);

        _ = result.Should().ContainSingle().Which.Should().Be("MyDB");
    }

    [Fact]
    public void HandleUpdateStatement()
    {
        var sql = "UPDATE [MyDB].[dbo].[Users] SET Active = 1 WHERE Id = 1";

        var result = SqlInterrogator.ExtractDatabaseNamesFromSql(sql);

        _ = result.Should().ContainSingle().Which.Should().Be("MyDB");
    }

    [Fact]
    public void HandleDeleteStatement()
    {
        var sql = "DELETE FROM [MyDB].[dbo].[Users] WHERE Id = 1";

        var result = SqlInterrogator.ExtractDatabaseNamesFromSql(sql);

        _ = result.Should().ContainSingle().Which.Should().Be("MyDB");
    }

    [Fact]
    public void HandleCreateTableStatement()
    {
        var sql = "CREATE TABLE [MyDB].[dbo].[NewUsers] (Id INT, Name VARCHAR(50))";

        var result = SqlInterrogator.ExtractDatabaseNamesFromSql(sql);

        _ = result.Should().ContainSingle().Which.Should().Be("MyDB");
    }

    [Fact]
    public void HandleNestedSubqueriesWithDifferentDatabases()
    {
        var sql = @"
            SELECT * FROM [DB1].[dbo].[Users] WHERE Id IN (
                SELECT UserId FROM [DB2].[dbo].[Orders] WHERE OrderId IN (
                    SELECT OrderId FROM [DB3].[dbo].[OrderDetails]
                )
            )";

        var result = SqlInterrogator.ExtractDatabaseNamesFromSql(sql);

        _ = result.Should().HaveCount(3);
        _ = result.Should().Contain(["DB1", "DB2", "DB3"]);
    }

    [Fact]
    public void HandleKoreanCharacters()
    {
        var sql = "SELECT * FROM [데이터베이스].[dbo].[사용자]";

        var result = SqlInterrogator.ExtractDatabaseNamesFromSql(sql);

        _ = result.Should().Contain("데이터베이스");
    }

    [Fact]
    public void HandleCyrillicCharacters()
    {
        var sql = "SELECT * FROM [БазаДанных].[dbo].[Пользователи]";

        var result = SqlInterrogator.ExtractDatabaseNamesFromSql(sql);

        _ = result.Should().Contain("БазаДанных");
    }

    [Fact]
    public void HandleMultipleStatementTypes()
    {
        var sql = @"
                    INSERT INTO [DB1].[dbo].[Log] VALUES (1);
                    SELECT * FROM [DB2].[dbo].[Users];
                    UPDATE [DB3].[dbo].[Status] SET Value = 1;";

        var result = SqlInterrogator.ExtractDatabaseNamesFromSql(sql);

        _ = result.Should().HaveCount(3);
        _ = result.Should().Contain(["DB1", "DB2", "DB3"]);
    }
}
