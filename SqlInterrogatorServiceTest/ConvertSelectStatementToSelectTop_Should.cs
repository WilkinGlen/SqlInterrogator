using FluentAssertions;
using SqlInterrogatorService;

namespace SqlInterrogatorServiceTest;

public class ConvertSelectStatementToSelectTop_Should
{
    [Fact]
    public void ReturnNull_WhenSqlIsNull()
    {
        var result = SqlInterrogator.ConvertSelectStatementToSelectTop(null!, 10);

        _ = result.Should().BeNull();
    }

    [Fact]
    public void ReturnNull_WhenSqlIsEmpty()
    {
        var result = SqlInterrogator.ConvertSelectStatementToSelectTop("", 10);

        _ = result.Should().BeNull();
    }

    [Fact]
    public void ReturnNull_WhenSqlIsWhitespace()
    {
        var result = SqlInterrogator.ConvertSelectStatementToSelectTop("   ", 10);

        _ = result.Should().BeNull();
    }

    [Fact]
    public void ReturnNull_WhenTopIsZero()
    {
        var sql = "SELECT * FROM Users";

        var result = SqlInterrogator.ConvertSelectStatementToSelectTop(sql, 0);

        _ = result.Should().BeNull();
    }

    [Fact]
    public void ReturnNull_WhenTopIsNegative()
    {
        var sql = "SELECT * FROM Users";

        var result = SqlInterrogator.ConvertSelectStatementToSelectTop(sql, -1);

        _ = result.Should().BeNull();
    }

    [Fact]
    public void ReturnNull_WhenTopIsLargeNegativeNumber()
    {
        var sql = "SELECT * FROM Users";

        var result = SqlInterrogator.ConvertSelectStatementToSelectTop(sql, -999);

        _ = result.Should().BeNull();
    }

    [Fact]
    public void ReturnNull_WhenUpdateStatement()
    {
        var sql = "UPDATE Users SET Active = 1";

        var result = SqlInterrogator.ConvertSelectStatementToSelectTop(sql, 10);

        _ = result.Should().BeNull();
    }

    [Fact]
    public void ReturnNull_WhenInsertStatement()
    {
        var sql = "INSERT INTO Users (Name, Email) VALUES ('John', 'john@example.com')";

        var result = SqlInterrogator.ConvertSelectStatementToSelectTop(sql, 10);

        _ = result.Should().BeNull();
    }

    [Fact]
    public void ReturnNull_WhenDeleteStatement()
    {
        var sql = "DELETE FROM Users WHERE Id = 1";

        var result = SqlInterrogator.ConvertSelectStatementToSelectTop(sql, 10);

        _ = result.Should().BeNull();
    }

    [Fact]
    public void ReturnNull_WhenNoFromClause()
    {
        var sql = "SELECT GETDATE()";

        var result = SqlInterrogator.ConvertSelectStatementToSelectTop(sql, 10);

        _ = result.Should().BeNull();
    }

    [Fact]
    public void ConvertToSelectTop_WhenSimpleSelectStar()
    {
        var sql = "SELECT * FROM Users";

        var result = SqlInterrogator.ConvertSelectStatementToSelectTop(sql, 10);

        _ = result.Should().Be("SELECT TOP 10 FROM Users");
    }

    [Fact]
    public void ConvertToSelectTop_WhenSimpleSelectColumns()
    {
        var sql = "SELECT Name, Email FROM Users";

        var result = SqlInterrogator.ConvertSelectStatementToSelectTop(sql, 5);

        _ = result.Should().Be("SELECT TOP 5 FROM Users");
    }

    [Fact]
    public void ConvertToSelectTop_WhenTopIsOne()
    {
        var sql = "SELECT * FROM Users";

        var result = SqlInterrogator.ConvertSelectStatementToSelectTop(sql, 1);

        _ = result.Should().Be("SELECT TOP 1 FROM Users");
    }

    [Fact]
    public void ConvertToSelectTop_WhenTopIsLargeNumber()
    {
        var sql = "SELECT * FROM Users";

        var result = SqlInterrogator.ConvertSelectStatementToSelectTop(sql, 999999);

        _ = result.Should().Be("SELECT TOP 999999 FROM Users");
    }

    [Fact]
    public void ConvertToSelectTop_WhenQualifiedColumns()
    {
        var sql = "SELECT u.Name, u.Email FROM Users u";

        var result = SqlInterrogator.ConvertSelectStatementToSelectTop(sql, 10);

        _ = result.Should().Be("SELECT TOP 10 FROM Users u");
    }

    [Fact]
    public void ConvertToSelectTop_WhenBracketedColumns()
    {
        var sql = "SELECT [Name], [Email] FROM Users";

        var result = SqlInterrogator.ConvertSelectStatementToSelectTop(sql, 10);

        _ = result.Should().Be("SELECT TOP 10 FROM Users");
    }

    [Fact]
    public void ConvertToSelectTop_WhenColumnsWithAliases()
    {
        var sql = "SELECT Name AS FullName, Email AS EmailAddress FROM Users";

        var result = SqlInterrogator.ConvertSelectStatementToSelectTop(sql, 10);

        _ = result.Should().Be("SELECT TOP 10 FROM Users");
    }

    [Fact]
    public void ConvertToSelectTop_WhenSimpleWhereClause()
    {
        var sql = "SELECT * FROM Users WHERE Active = 1";

        var result = SqlInterrogator.ConvertSelectStatementToSelectTop(sql, 10);

        _ = result.Should().Be("SELECT TOP 10 FROM Users WHERE Active = 1");
    }

    [Fact]
    public void ConvertToSelectTop_WhenComplexWhereClause()
    {
        var sql = "SELECT Name, Email FROM Users WHERE Active = 1 AND CreatedDate > '2024-01-01'";

        var result = SqlInterrogator.ConvertSelectStatementToSelectTop(sql, 10);

        _ = result.Should().Be("SELECT TOP 10 FROM Users WHERE Active = 1 AND CreatedDate > '2024-01-01'");
    }

    [Fact]
    public void ConvertToSelectTop_WhenWhereWithParameters()
    {
        var sql = "SELECT * FROM Users WHERE Id = @userId AND Status = @status";

        var result = SqlInterrogator.ConvertSelectStatementToSelectTop(sql, 10);

        _ = result.Should().Be("SELECT TOP 10 FROM Users WHERE Id = @userId AND Status = @status");
    }

    [Fact]
    public void ConvertToSelectTop_WhenInnerJoin()
    {
        var sql = "SELECT u.Name, o.OrderDate FROM Users u INNER JOIN Orders o ON u.Id = o.UserId";

        var result = SqlInterrogator.ConvertSelectStatementToSelectTop(sql, 10);

        _ = result.Should().Be("SELECT TOP 10 FROM Users u INNER JOIN Orders o ON u.Id = o.UserId");
    }

    [Fact]
    public void ConvertToSelectTop_WhenLeftJoin()
    {
        var sql = "SELECT u.Name, o.OrderDate FROM Users u LEFT JOIN Orders o ON u.Id = o.UserId";

        var result = SqlInterrogator.ConvertSelectStatementToSelectTop(sql, 10);

        _ = result.Should().Be("SELECT TOP 10 FROM Users u LEFT JOIN Orders o ON u.Id = o.UserId");
    }

    [Fact]
    public void ConvertToSelectTop_WhenMultipleJoins()
    {
        var sql = @"SELECT u.Name, o.OrderDate, p.ProductName 
 FROM Users u 
 INNER JOIN Orders o ON u.Id = o.UserId
          LEFT JOIN Products p ON o.ProductId = p.Id";

        var result = SqlInterrogator.ConvertSelectStatementToSelectTop(sql, 10);

        _ = result.Should().Contain("SELECT TOP 10 FROM Users u");
        _ = result.Should().Contain("INNER JOIN Orders o ON u.Id = o.UserId");
        _ = result.Should().Contain("LEFT JOIN Products p ON o.ProductId = p.Id");
    }

    [Fact]
    public void ConvertToSelectTop_WhenJoinWithWhere()
    {
        var sql = "SELECT u.Name, o.OrderDate FROM Users u JOIN Orders o ON u.Id = o.UserId WHERE u.Active = 1";

        var result = SqlInterrogator.ConvertSelectStatementToSelectTop(sql, 10);

        _ = result.Should().Be("SELECT TOP 10 FROM Users u JOIN Orders o ON u.Id = o.UserId WHERE u.Active = 1");
    }

    [Fact]
    public void ConvertToSelectTop_WhenOrderBy()
    {
        var sql = "SELECT Name, Email FROM Users ORDER BY Name";

        var result = SqlInterrogator.ConvertSelectStatementToSelectTop(sql, 10);

        _ = result.Should().Be("SELECT TOP 10 FROM Users ORDER BY Name");
    }

    [Fact]
    public void ConvertToSelectTop_WhenOrderByMultipleColumns()
    {
        var sql = "SELECT * FROM Users ORDER BY Name ASC, CreatedDate DESC";

        var result = SqlInterrogator.ConvertSelectStatementToSelectTop(sql, 10);

        _ = result.Should().Be("SELECT TOP 10 FROM Users ORDER BY Name ASC, CreatedDate DESC");
    }

    [Fact]
    public void ConvertToSelectTop_WhenWhereAndOrderBy()
    {
        var sql = "SELECT * FROM Users WHERE Active = 1 ORDER BY Name";

        var result = SqlInterrogator.ConvertSelectStatementToSelectTop(sql, 10);

        _ = result.Should().Be("SELECT TOP 10 FROM Users WHERE Active = 1 ORDER BY Name");
    }

    [Fact]
    public void ConvertToSelectTop_WhenGroupBy()
    {
        var sql = "SELECT Name, COUNT(*) FROM Users GROUP BY Name";

        var result = SqlInterrogator.ConvertSelectStatementToSelectTop(sql, 10);

        _ = result.Should().Be("SELECT TOP 10 FROM Users GROUP BY Name");
    }

    [Fact]
    public void ConvertToSelectTop_WhenGroupByWithHaving()
    {
        var sql = "SELECT Name, COUNT(*) FROM Users GROUP BY Name HAVING COUNT(*) > 1";

        var result = SqlInterrogator.ConvertSelectStatementToSelectTop(sql, 10);

        _ = result.Should().Be("SELECT TOP 10 FROM Users GROUP BY Name HAVING COUNT(*) > 1");
    }

    [Fact]
    public void ConvertToSelectTop_WhenWhereGroupByHaving()
    {
        var sql = "SELECT Department, COUNT(*) FROM Users WHERE Active = 1 GROUP BY Department HAVING COUNT(*) > 5";

        var result = SqlInterrogator.ConvertSelectStatementToSelectTop(sql, 10);

        _ = result.Should().Be("SELECT TOP 10 FROM Users WHERE Active = 1 GROUP BY Department HAVING COUNT(*) > 5");
    }

    [Fact]
    public void ConvertToSelectTop_WhenDistinct()
    {
        var sql = "SELECT DISTINCT Name, Email FROM Users";

        var result = SqlInterrogator.ConvertSelectStatementToSelectTop(sql, 10);

        _ = result.Should().Be("SELECT TOP 10 FROM Users");
    }

    [Fact]
    public void ConvertToSelectTop_WhenExistingTop()
    {
        var sql = "SELECT TOP 100 Name, Email FROM Users";

        var result = SqlInterrogator.ConvertSelectStatementToSelectTop(sql, 10);

        _ = result.Should().Be("SELECT TOP 10 FROM Users");
    }

    [Fact]
    public void ConvertToSelectTop_WhenExistingTopWithOrderBy()
    {
        var sql = "SELECT TOP 100 * FROM Users ORDER BY CreatedDate DESC";

        var result = SqlInterrogator.ConvertSelectStatementToSelectTop(sql, 5);

        _ = result.Should().Be("SELECT TOP 5 FROM Users ORDER BY CreatedDate DESC");
    }

    [Fact]
    public void ConvertToSelectTop_WhenTwoPartTableName()
    {
        var sql = "SELECT * FROM dbo.Users";

        var result = SqlInterrogator.ConvertSelectStatementToSelectTop(sql, 10);

        _ = result.Should().Be("SELECT TOP 10 FROM dbo.Users");
    }

    [Fact]
    public void ConvertToSelectTop_WhenThreePartTableName()
    {
        var sql = "SELECT * FROM MyDB.dbo.Users";

        var result = SqlInterrogator.ConvertSelectStatementToSelectTop(sql, 10);

        _ = result.Should().Be("SELECT TOP 10 FROM MyDB.dbo.Users");
    }

    [Fact]
    public void ConvertToSelectTop_WhenBracketedTableName()
    {
        var sql = "SELECT * FROM [MyDB].[dbo].[Users]";

        var result = SqlInterrogator.ConvertSelectStatementToSelectTop(sql, 10);

        _ = result.Should().Be("SELECT TOP 10 FROM [MyDB].[dbo].[Users]");
    }

    [Fact]
    public void ConvertToSelectTop_WhenTableAlias()
    {
        var sql = "SELECT u.Name FROM Users u";

        var result = SqlInterrogator.ConvertSelectStatementToSelectTop(sql, 10);

        _ = result.Should().Be("SELECT TOP 10 FROM Users u");
    }

    [Fact]
    public void ConvertToSelectTop_WhenTableWithNoLock()
    {
        var sql = "SELECT * FROM Users WITH (NOLOCK)";

        var result = SqlInterrogator.ConvertSelectStatementToSelectTop(sql, 10);

        _ = result.Should().Be("SELECT TOP 10 FROM Users WITH (NOLOCK)");
    }

    [Fact]
    public void ConvertToSelectTop_WhenAlreadyCountStar()
    {
        var sql = "SELECT COUNT(*) FROM Users";

        var result = SqlInterrogator.ConvertSelectStatementToSelectTop(sql, 10);

        _ = result.Should().Be("SELECT TOP 10 FROM Users");
    }

    [Fact]
    public void ConvertToSelectTop_WhenMultilineQuery()
    {
        var sql = @"
            SELECT 
    Name,
          Email,
       CreatedDate
            FROM 
                Users
            WHERE 
    Active = 1";

        var result = SqlInterrogator.ConvertSelectStatementToSelectTop(sql, 10);

        _ = result.Should().Contain("SELECT TOP 10 FROM");
        _ = result.Should().Contain("Users");
        _ = result.Should().Contain("WHERE");
        _ = result.Should().Contain("Active = 1");
    }

    [Fact]
    public void ConvertToSelectTop_WhenSingleLineComments()
    {
        var sql = @"
      -- Get all users
         SELECT Name, Email 
   FROM Users -- Main users table
       WHERE Active = 1";

        var result = SqlInterrogator.ConvertSelectStatementToSelectTop(sql, 10);

        _ = result.Should().Contain("SELECT TOP 10 FROM");
        _ = result.Should().Contain("Users");
        _ = result.Should().Contain("WHERE");
    }

    [Fact]
    public void ConvertToSelectTop_WhenMultiLineComments()
    {
        var sql = @"
            /* This query gets all active users
     Created: 2024-01-01
   */
       SELECT Name, Email 
            FROM Users 
      WHERE Active = 1";

        var result = SqlInterrogator.ConvertSelectStatementToSelectTop(sql, 10);

        _ = result.Should().Contain("SELECT TOP 10 FROM");
        _ = result.Should().Contain("Users");
    }

    [Fact]
    public void ConvertToSelectTop_WhenCTE()
    {
        var sql = @"
    WITH UserCTE AS (
        SELECT * FROM AllUsers WHERE Status = 1
            )
            SELECT Name, Email FROM Users";

        var result = SqlInterrogator.ConvertSelectStatementToSelectTop(sql, 10);

        _ = result.Should().Contain("SELECT TOP 10 FROM");
        _ = result.Should().Contain("Users");
    }

    [Fact]
    public void ConvertToSelectTop_WhenUseStatement()
    {
        var sql = @"
            USE MyDatabase;
  SELECT Name, Email FROM Users";

        var result = SqlInterrogator.ConvertSelectStatementToSelectTop(sql, 10);

        _ = result.Should().Contain("SELECT TOP 10 FROM");
        _ = result.Should().Contain("Users");
    }

    [Fact]
    public void ConvertToSelectTop_WhenSubqueryInWhere()
    {
        var sql = "SELECT * FROM Users WHERE Id IN (SELECT UserId FROM Orders WHERE Status = 'Active')";

        var result = SqlInterrogator.ConvertSelectStatementToSelectTop(sql, 10);

        _ = result.Should().Be("SELECT TOP 10 FROM Users WHERE Id IN (SELECT UserId FROM Orders WHERE Status = 'Active')");
    }

    [Fact]
    public void ConvertToSelectTop_WhenLowercaseKeywords()
    {
        var sql = "select name, email from users where active = 1";

        var result = SqlInterrogator.ConvertSelectStatementToSelectTop(sql, 10);

        _ = result.Should().Contain("SELECT TOP 10 from users where active = 1");
    }

    [Fact]
    public void ConvertToSelectTop_WhenMixedCaseKeywords()
    {
        var sql = "SeLeCt Name FrOm Users WhErE Active = 1";

        var result = SqlInterrogator.ConvertSelectStatementToSelectTop(sql, 10);

        _ = result.Should().Contain("SELECT TOP 10 FrOm Users WhErE Active = 1");
    }

    [Fact]
    public void ConvertToSelectTop_WhenComplexQueryWithAllClauses()
    {
        var sql = @"
        SELECT DISTINCT u.Name, u.Email, COUNT(o.Id) AS OrderCount
      FROM Users u
      LEFT JOIN Orders o ON u.Id = o.UserId
            WHERE u.Active = 1 
    AND u.CreatedDate > '2024-01-01'
  AND o.Status IN ('Pending', 'Completed')
  GROUP BY u.Name, u.Email
        HAVING COUNT(o.Id) > 5
ORDER BY u.Name ASC";

        var result = SqlInterrogator.ConvertSelectStatementToSelectTop(sql, 10);

        _ = result.Should().Contain("SELECT TOP 10 FROM Users u");
        _ = result.Should().Contain("LEFT JOIN Orders o ON u.Id = o.UserId");
        _ = result.Should().Contain("WHERE u.Active = 1");
        _ = result.Should().Contain("GROUP BY u.Name, u.Email");
        _ = result.Should().Contain("HAVING COUNT(o.Id) > 5");
        _ = result.Should().Contain("ORDER BY u.Name ASC");
    }

    [Fact]
    public void ConvertToSelectTop_WhenPaginationQuery()
    {
        var sql = @"
        SELECT * 
      FROM Users 
     WHERE Active = 1 
      ORDER BY CreatedDate DESC 
     OFFSET 0 ROWS 
         FETCH NEXT 10 ROWS ONLY";

        var result = SqlInterrogator.ConvertSelectStatementToSelectTop(sql, 5);

        _ = result.Should().Contain("SELECT TOP 5 FROM Users");
        _ = result.Should().Contain("WHERE Active = 1");
        _ = result.Should().Contain("ORDER BY CreatedDate DESC");
        _ = result.Should().Contain("OFFSET 0 ROWS");
        _ = result.Should().Contain("FETCH NEXT 10 ROWS ONLY");
    }

    [Fact]
    public void ConvertToSelectTop_WhenDoubleQuotedIdentifiers()
    {
        var sql = "SELECT \"Name\", \"Email\" FROM \"Users\"";

        var result = SqlInterrogator.ConvertSelectStatementToSelectTop(sql, 10);

        _ = result.Should().Be("SELECT TOP 10 FROM \"Users\"");
    }

    [Fact]
    public void ConvertToSelectTop_WhenSpecialCharactersInStrings()
    {
        var sql = "SELECT * FROM Users WHERE Email = 'test@example.com' AND Name LIKE '%O''Brien%'";

        var result = SqlInterrogator.ConvertSelectStatementToSelectTop(sql, 10);

        _ = result.Should().Be("SELECT TOP 10 FROM Users WHERE Email = 'test@example.com' AND Name LIKE '%O''Brien%'");
    }

    [Fact]
    public void ConvertToSelectTop_WhenTopN_SalesScenario()
    {
        var sql = "SELECT ProductName, SalesTotal FROM Products ORDER BY SalesTotal DESC";

        var result = SqlInterrogator.ConvertSelectStatementToSelectTop(sql, 5);

        _ = result.Should().Be("SELECT TOP 5 FROM Products ORDER BY SalesTotal DESC");
    }

    [Fact]
    public void ConvertToSelectTop_WhenRecentItems()
    {
        var sql = "SELECT * FROM Orders WHERE Status = 'Completed' ORDER BY OrderDate DESC";

        var result = SqlInterrogator.ConvertSelectStatementToSelectTop(sql, 20);

        _ = result.Should().Be("SELECT TOP 20 FROM Orders WHERE Status = 'Completed' ORDER BY OrderDate DESC");
    }

    [Fact]
    public void ConvertToSelectTop_WhenDataSampling()
    {
        var sql = "SELECT * FROM LargeTable WHERE Category = @category";

        var result = SqlInterrogator.ConvertSelectStatementToSelectTop(sql, 1000);

        _ = result.Should().Be("SELECT TOP 1000 FROM LargeTable WHERE Category = @category");
    }

    [Fact]
    public void ConvertToSelectTop_WhenAllClause()
    {
        var sql = "SELECT ALL Name, Email FROM Users";

        var result = SqlInterrogator.ConvertSelectStatementToSelectTop(sql, 10);

        _ = result.Should().Be("SELECT TOP 10 FROM Users");
    }

    [Fact]
    public void ConvertToSelectTop_WhenUnion()
    {
        var sql = "SELECT Name FROM Users WHERE Active = 1 UNION SELECT Name FROM ArchivedUsers";

        var result = SqlInterrogator.ConvertSelectStatementToSelectTop(sql, 10);

        _ = result.Should().Contain("SELECT TOP 10 FROM Users WHERE Active = 1 UNION SELECT Name FROM ArchivedUsers");
    }

    [Fact]
    public void ConvertToSelectTop_WhenExistsClause()
    {
        var sql = "SELECT * FROM Users u WHERE EXISTS (SELECT 1 FROM Orders o WHERE o.UserId = u.Id)";

        var result = SqlInterrogator.ConvertSelectStatementToSelectTop(sql, 10);

        _ = result.Should().Be("SELECT TOP 10 FROM Users u WHERE EXISTS (SELECT 1 FROM Orders o WHERE o.UserId = u.Id)");
    }

    [Fact]
    public void ConvertToSelectTop_WhenBetweenOperator()
    {
        var sql = "SELECT * FROM Users WHERE Age BETWEEN 18 AND 65";

        var result = SqlInterrogator.ConvertSelectStatementToSelectTop(sql, 10);

        _ = result.Should().Be("SELECT TOP 10 FROM Users WHERE Age BETWEEN 18 AND 65");
    }

    [Fact]
    public void ConvertToSelectTop_WithDifferentTopValues()
    {
        var sql = "SELECT * FROM Users ORDER BY CreatedDate DESC";

        var result1 = SqlInterrogator.ConvertSelectStatementToSelectTop(sql, 1);
        var result5 = SqlInterrogator.ConvertSelectStatementToSelectTop(sql, 5);
        var result100 = SqlInterrogator.ConvertSelectStatementToSelectTop(sql, 100);
        var result1000 = SqlInterrogator.ConvertSelectStatementToSelectTop(sql, 1000);

        _ = result1.Should().Contain("SELECT TOP 1 FROM");
        _ = result5.Should().Contain("SELECT TOP 5 FROM");
        _ = result100.Should().Contain("SELECT TOP 100 FROM");
        _ = result1000.Should().Contain("SELECT TOP 1000 FROM");
    }

    [Fact]
    public void ConvertToSelectTop_PreservesTableHintsWithMultipleOptions()
    {
        var sql = "SELECT * FROM Users WITH (NOLOCK, READPAST)";

        var result = SqlInterrogator.ConvertSelectStatementToSelectTop(sql, 10);

        _ = result.Should().Be("SELECT TOP 10 FROM Users WITH (NOLOCK, READPAST)");
    }

    [Fact]
    public void ConvertToSelectTop_WhenCrossJoin()
    {
        var sql = "SELECT u.Name, c.CategoryName FROM Users u CROSS JOIN Categories c";

        var result = SqlInterrogator.ConvertSelectStatementToSelectTop(sql, 10);

        _ = result.Should().Be("SELECT TOP 10 FROM Users u CROSS JOIN Categories c");
    }
}
