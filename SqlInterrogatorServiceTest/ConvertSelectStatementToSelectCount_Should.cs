using FluentAssertions;
using SqlInterrogatorService;

namespace SqlInterrogatorServiceTest;

public class ConvertSelectStatementToSelectCount_Should
{
    [Fact]
    public void ReturnNull_WhenSqlIsNull()
    {
        var result = SqlInterrogator.ConvertSelectStatementToSelectCount(null!);

        _ = result.Should().BeNull();
    }

    [Fact]
    public void ReturnNull_WhenSqlIsEmpty()
    {
        var result = SqlInterrogator.ConvertSelectStatementToSelectCount("");

        _ = result.Should().BeNull();
    }

    [Fact]
    public void ReturnNull_WhenSqlIsWhitespace()
    {
        var result = SqlInterrogator.ConvertSelectStatementToSelectCount("   ");

        _ = result.Should().BeNull();
    }

    [Fact]
    public void ReturnNull_WhenUpdateStatement()
    {
        var sql = "UPDATE Users SET Active = 1";

        var result = SqlInterrogator.ConvertSelectStatementToSelectCount(sql);

        _ = result.Should().BeNull();
    }

    [Fact]
    public void ReturnNull_WhenInsertStatement()
    {
        var sql = "INSERT INTO Users (Name, Email) VALUES ('John', 'john@example.com')";

        var result = SqlInterrogator.ConvertSelectStatementToSelectCount(sql);

        _ = result.Should().BeNull();
    }

    [Fact]
    public void ReturnNull_WhenDeleteStatement()
    {
        var sql = "DELETE FROM Users WHERE Id = 1";

        var result = SqlInterrogator.ConvertSelectStatementToSelectCount(sql);

        _ = result.Should().BeNull();
    }

    [Fact]
    public void ReturnNull_WhenNoFromClause()
    {
        var sql = "SELECT GETDATE()";

        var result = SqlInterrogator.ConvertSelectStatementToSelectCount(sql);

        _ = result.Should().BeNull();
    }

    [Fact]
    public void ConvertToSelectCount_WhenSimpleSelectStar()
    {
        var sql = "SELECT * FROM Users";

        var result = SqlInterrogator.ConvertSelectStatementToSelectCount(sql);

        _ = result.Should().Be("SELECT COUNT(*) FROM Users");
    }

    [Fact]
    public void ConvertToSelectCount_WhenSimpleSelectColumns()
    {
        var sql = "SELECT Name, Email FROM Users";

        var result = SqlInterrogator.ConvertSelectStatementToSelectCount(sql);

        _ = result.Should().Be("SELECT COUNT(*) FROM Users");
    }

    [Fact]
    public void ConvertToSelectCount_WhenQualifiedColumns()
    {
        var sql = "SELECT u.Name, u.Email FROM Users u";

        var result = SqlInterrogator.ConvertSelectStatementToSelectCount(sql);

        _ = result.Should().Be("SELECT COUNT(*) FROM Users u");
    }

    [Fact]
    public void ConvertToSelectCount_WhenBracketedColumns()
    {
        var sql = "SELECT [Name], [Email] FROM Users";

        var result = SqlInterrogator.ConvertSelectStatementToSelectCount(sql);

        _ = result.Should().Be("SELECT COUNT(*) FROM Users");
    }

    [Fact]
    public void ConvertToSelectCount_WhenColumnsWithAliases()
    {
        var sql = "SELECT Name AS FullName, Email AS EmailAddress FROM Users";

        var result = SqlInterrogator.ConvertSelectStatementToSelectCount(sql);

        _ = result.Should().Be("SELECT COUNT(*) FROM Users");
    }

    [Fact]
    public void ConvertToSelectCount_WhenSimpleWhereClause()
    {
        var sql = "SELECT * FROM Users WHERE Active = 1";

        var result = SqlInterrogator.ConvertSelectStatementToSelectCount(sql);

        _ = result.Should().Be("SELECT COUNT(*) FROM Users WHERE Active = 1");
    }

    [Fact]
    public void ConvertToSelectCount_WhenComplexWhereClause()
    {
        var sql = "SELECT Name, Email FROM Users WHERE Active = 1 AND CreatedDate > '2024-01-01'";

        var result = SqlInterrogator.ConvertSelectStatementToSelectCount(sql);

        _ = result.Should().Be("SELECT COUNT(*) FROM Users WHERE Active = 1 AND CreatedDate > '2024-01-01'");
    }

    [Fact]
    public void ConvertToSelectCount_WhenWhereWithParameters()
    {
        var sql = "SELECT * FROM Users WHERE Id = @userId AND Status = @status";

        var result = SqlInterrogator.ConvertSelectStatementToSelectCount(sql);

        _ = result.Should().Be("SELECT COUNT(*) FROM Users WHERE Id = @userId AND Status = @status");
    }

    [Fact]
    public void ConvertToSelectCount_WhenWhereLikeClause()
    {
        var sql = "SELECT * FROM Users WHERE Email LIKE '%@example.com'";

        var result = SqlInterrogator.ConvertSelectStatementToSelectCount(sql);

        _ = result.Should().Be("SELECT COUNT(*) FROM Users WHERE Email LIKE '%@example.com'");
    }

    [Fact]
    public void ConvertToSelectCount_WhenWhereInClause()
    {
        var sql = "SELECT * FROM Users WHERE Status IN (1, 2, 3)";

        var result = SqlInterrogator.ConvertSelectStatementToSelectCount(sql);

        _ = result.Should().Be("SELECT COUNT(*) FROM Users WHERE Status IN (1, 2, 3)");
    }

    [Fact]
    public void ConvertToSelectCount_WhenWhereIsNull()
    {
        var sql = "SELECT * FROM Users WHERE DeletedDate IS NULL";

        var result = SqlInterrogator.ConvertSelectStatementToSelectCount(sql);

        _ = result.Should().Be("SELECT COUNT(*) FROM Users WHERE DeletedDate IS NULL");
    }

    [Fact]
    public void ConvertToSelectCount_WhenWhereIsNotNull()
    {
        var sql = "SELECT * FROM Users WHERE CreatedDate IS NOT NULL";

        var result = SqlInterrogator.ConvertSelectStatementToSelectCount(sql);

        _ = result.Should().Be("SELECT COUNT(*) FROM Users WHERE CreatedDate IS NOT NULL");
    }

    [Fact]
    public void ConvertToSelectCount_WhenInnerJoin()
    {
        var sql = "SELECT u.Name, o.OrderDate FROM Users u INNER JOIN Orders o ON u.Id = o.UserId";

        var result = SqlInterrogator.ConvertSelectStatementToSelectCount(sql);

        _ = result.Should().Be("SELECT COUNT(*) FROM Users u INNER JOIN Orders o ON u.Id = o.UserId");
    }

    [Fact]
    public void ConvertToSelectCount_WhenLeftJoin()
    {
        var sql = "SELECT u.Name, o.OrderDate FROM Users u LEFT JOIN Orders o ON u.Id = o.UserId";

        var result = SqlInterrogator.ConvertSelectStatementToSelectCount(sql);

        _ = result.Should().Be("SELECT COUNT(*) FROM Users u LEFT JOIN Orders o ON u.Id = o.UserId");
    }

    [Fact]
    public void ConvertToSelectCount_WhenRightJoin()
    {
        var sql = "SELECT u.Name, o.OrderDate FROM Users u RIGHT JOIN Orders o ON u.Id = o.UserId";

        var result = SqlInterrogator.ConvertSelectStatementToSelectCount(sql);

        _ = result.Should().Be("SELECT COUNT(*) FROM Users u RIGHT JOIN Orders o ON u.Id = o.UserId");
    }

    [Fact]
    public void ConvertToSelectCount_WhenFullOuterJoin()
    {
        var sql = "SELECT u.Name, o.OrderDate FROM Users u FULL OUTER JOIN Orders o ON u.Id = o.UserId";

        var result = SqlInterrogator.ConvertSelectStatementToSelectCount(sql);

        _ = result.Should().Be("SELECT COUNT(*) FROM Users u FULL OUTER JOIN Orders o ON u.Id = o.UserId");
    }

    [Fact]
    public void ConvertToSelectCount_WhenMultipleJoins()
    {
        var sql = @"SELECT u.Name, o.OrderDate, p.ProductName 
            FROM Users u 
     INNER JOIN Orders o ON u.Id = o.UserId
            LEFT JOIN Products p ON o.ProductId = p.Id";

        var result = SqlInterrogator.ConvertSelectStatementToSelectCount(sql);

        _ = result.Should().Contain("SELECT COUNT(*) FROM Users u");
        _ = result.Should().Contain("INNER JOIN Orders o ON u.Id = o.UserId");
        _ = result.Should().Contain("LEFT JOIN Products p ON o.ProductId = p.Id");
    }

    [Fact]
    public void ConvertToSelectCount_WhenJoinWithWhere()
    {
        var sql = "SELECT u.Name, o.OrderDate FROM Users u JOIN Orders o ON u.Id = o.UserId WHERE u.Active = 1";

        var result = SqlInterrogator.ConvertSelectStatementToSelectCount(sql);

        _ = result.Should().Be("SELECT COUNT(*) FROM Users u JOIN Orders o ON u.Id = o.UserId WHERE u.Active = 1");
    }

    [Fact]
    public void ConvertToSelectCount_WhenOrderBy()
    {
        var sql = "SELECT Name, Email FROM Users ORDER BY Name";

        var result = SqlInterrogator.ConvertSelectStatementToSelectCount(sql);

        _ = result.Should().Be("SELECT COUNT(*) FROM Users ORDER BY Name");
    }

    [Fact]
    public void ConvertToSelectCount_WhenOrderByMultipleColumns()
    {
        var sql = "SELECT * FROM Users ORDER BY Name ASC, CreatedDate DESC";

        var result = SqlInterrogator.ConvertSelectStatementToSelectCount(sql);

        _ = result.Should().Be("SELECT COUNT(*) FROM Users ORDER BY Name ASC, CreatedDate DESC");
    }

    [Fact]
    public void ConvertToSelectCount_WhenWhereAndOrderBy()
    {
        var sql = "SELECT * FROM Users WHERE Active = 1 ORDER BY Name";

        var result = SqlInterrogator.ConvertSelectStatementToSelectCount(sql);

        _ = result.Should().Be("SELECT COUNT(*) FROM Users WHERE Active = 1 ORDER BY Name");
    }

    [Fact]
    public void ConvertToSelectCount_WhenGroupBy()
    {
        var sql = "SELECT Name, COUNT(*) FROM Users GROUP BY Name";

        var result = SqlInterrogator.ConvertSelectStatementToSelectCount(sql);

        _ = result.Should().Be("SELECT COUNT(*) FROM Users GROUP BY Name");
    }

    [Fact]
    public void ConvertToSelectCount_WhenGroupByWithHaving()
    {
        var sql = "SELECT Name, COUNT(*) FROM Users GROUP BY Name HAVING COUNT(*) > 1";

        var result = SqlInterrogator.ConvertSelectStatementToSelectCount(sql);

        _ = result.Should().Be("SELECT COUNT(*) FROM Users GROUP BY Name HAVING COUNT(*) > 1");
    }

    [Fact]
    public void ConvertToSelectCount_WhenWhereGroupByHaving()
    {
        var sql = "SELECT Department, COUNT(*) FROM Users WHERE Active = 1 GROUP BY Department HAVING COUNT(*) > 5";

        var result = SqlInterrogator.ConvertSelectStatementToSelectCount(sql);

        _ = result.Should().Be("SELECT COUNT(*) FROM Users WHERE Active = 1 GROUP BY Department HAVING COUNT(*) > 5");
    }

    [Fact]
    public void ConvertToSelectCount_WhenDistinct()
    {
        var sql = "SELECT DISTINCT Name, Email FROM Users";

        var result = SqlInterrogator.ConvertSelectStatementToSelectCount(sql);

        _ = result.Should().Be("SELECT COUNT(*) FROM Users");
    }

    [Fact]
    public void ConvertToSelectCount_WhenTop()
    {
        var sql = "SELECT TOP 10 Name, Email FROM Users";

        var result = SqlInterrogator.ConvertSelectStatementToSelectCount(sql);

        _ = result.Should().Be("SELECT COUNT(*) FROM Users");
    }

    [Fact]
    public void ConvertToSelectCount_WhenTopWithOrderBy()
    {
        var sql = "SELECT TOP 10 * FROM Users ORDER BY CreatedDate DESC";

        var result = SqlInterrogator.ConvertSelectStatementToSelectCount(sql);

        _ = result.Should().Be("SELECT COUNT(*) FROM Users ORDER BY CreatedDate DESC");
    }

    [Fact]
    public void ConvertToSelectCount_WhenTwoPartTableName()
    {
        var sql = "SELECT * FROM dbo.Users";

        var result = SqlInterrogator.ConvertSelectStatementToSelectCount(sql);

        _ = result.Should().Be("SELECT COUNT(*) FROM dbo.Users");
    }

    [Fact]
    public void ConvertToSelectCount_WhenThreePartTableName()
    {
        var sql = "SELECT * FROM MyDB.dbo.Users";

        var result = SqlInterrogator.ConvertSelectStatementToSelectCount(sql);

        _ = result.Should().Be("SELECT COUNT(*) FROM MyDB.dbo.Users");
    }

    [Fact]
    public void ConvertToSelectCount_WhenBracketedTableName()
    {
        var sql = "SELECT * FROM [MyDB].[dbo].[Users]";

        var result = SqlInterrogator.ConvertSelectStatementToSelectCount(sql);

        _ = result.Should().Be("SELECT COUNT(*) FROM [MyDB].[dbo].[Users]");
    }

    [Fact]
    public void ConvertToSelectCount_WhenFourPartTableName()
    {
        var sql = "SELECT * FROM [Server1].[MyDB].[dbo].[Users]";

        var result = SqlInterrogator.ConvertSelectStatementToSelectCount(sql);

        _ = result.Should().Be("SELECT COUNT(*) FROM [Server1].[MyDB].[dbo].[Users]");
    }

    [Fact]
    public void ConvertToSelectCount_WhenTableAlias()
    {
        var sql = "SELECT u.Name FROM Users u";

        var result = SqlInterrogator.ConvertSelectStatementToSelectCount(sql);

        _ = result.Should().Be("SELECT COUNT(*) FROM Users u");
    }

    [Fact]
    public void ConvertToSelectCount_WhenTableAliasWithAs()
    {
        var sql = "SELECT u.Name FROM Users AS u";

        var result = SqlInterrogator.ConvertSelectStatementToSelectCount(sql);

        _ = result.Should().Be("SELECT COUNT(*) FROM Users AS u");
    }

    [Fact]
    public void ConvertToSelectCount_WhenTableWithNoLock()
    {
        var sql = "SELECT * FROM Users WITH (NOLOCK)";

        var result = SqlInterrogator.ConvertSelectStatementToSelectCount(sql);

        _ = result.Should().Be("SELECT COUNT(*) FROM Users WITH (NOLOCK)");
    }

    [Fact]
    public void ConvertToSelectCount_WhenTableWithMultipleHints()
    {
        var sql = "SELECT * FROM Users WITH (NOLOCK, READPAST)";

        var result = SqlInterrogator.ConvertSelectStatementToSelectCount(sql);

        _ = result.Should().Be("SELECT COUNT(*) FROM Users WITH (NOLOCK, READPAST)");
    }

    [Fact]
    public void ConvertToSelectCount_WhenAlreadyCountStar()
    {
        var sql = "SELECT COUNT(*) FROM Users";

        var result = SqlInterrogator.ConvertSelectStatementToSelectCount(sql);

        _ = result.Should().Be("SELECT COUNT(*) FROM Users");
    }

    [Fact]
    public void ConvertToSelectCount_WhenCountWithAlias()
    {
        var sql = "SELECT COUNT(*) AS TotalUsers FROM Users";

        var result = SqlInterrogator.ConvertSelectStatementToSelectCount(sql);

        _ = result.Should().Be("SELECT COUNT(*) FROM Users");
    }

    [Fact]
    public void ConvertToSelectCount_WhenMultipleAggregates()
    {
        var sql = "SELECT COUNT(*), SUM(Amount), AVG(Price) FROM Orders";

        var result = SqlInterrogator.ConvertSelectStatementToSelectCount(sql);

        _ = result.Should().Be("SELECT COUNT(*) FROM Orders");
    }

    [Fact]
    public void ConvertToSelectCount_WhenCountDistinct()
    {
        var sql = "SELECT COUNT(DISTINCT Email) FROM Users";

        var result = SqlInterrogator.ConvertSelectStatementToSelectCount(sql);

        _ = result.Should().Be("SELECT COUNT(*) FROM Users");
    }

    [Fact]
    public void ConvertToSelectCount_WhenMultilineQuery()
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

        var result = SqlInterrogator.ConvertSelectStatementToSelectCount(sql);

        _ = result.Should().Contain("SELECT COUNT(*) FROM");
        _ = result.Should().Contain("Users");
        _ = result.Should().Contain("WHERE");
        _ = result.Should().Contain("Active = 1");
    }

    [Fact]
    public void ConvertToSelectCount_WhenExtraWhitespace()
    {
        var sql = "SELECT    Name  ,   Email   FROM    Users";

        var result = SqlInterrogator.ConvertSelectStatementToSelectCount(sql);

        _ = result.Should().Contain("SELECT COUNT(*) FROM");
        _ = result.Should().Contain("Users");
    }

    [Fact]
    public void ConvertToSelectCount_WhenSingleLineComments()
    {
        var sql = @"
       -- Get all users
            SELECT Name, Email 
        FROM Users -- Main users table
      WHERE Active = 1";

        var result = SqlInterrogator.ConvertSelectStatementToSelectCount(sql);

        _ = result.Should().Contain("SELECT COUNT(*) FROM");
        _ = result.Should().Contain("Users");
        _ = result.Should().Contain("WHERE");
    }

    [Fact]
    public void ConvertToSelectCount_WhenMultiLineComments()
    {
        var sql = @"
        /* This query gets all active users
               Created: 2024-01-01
     */
       SELECT Name, Email 
            FROM Users 
            WHERE Active = 1";

        var result = SqlInterrogator.ConvertSelectStatementToSelectCount(sql);

        _ = result.Should().Contain("SELECT COUNT(*) FROM");
        _ = result.Should().Contain("Users");
    }

    [Fact]
    public void ConvertToSelectCount_WhenCTE()
    {
        var sql = @"
      WITH UserCTE AS (
       SELECT * FROM AllUsers WHERE Status = 1
     )
        SELECT Name, Email FROM Users";

        var result = SqlInterrogator.ConvertSelectStatementToSelectCount(sql);

        _ = result.Should().Contain("SELECT COUNT(*) FROM");
        _ = result.Should().Contain("Users");
    }

    [Fact]
    public void ConvertToSelectCount_WhenMultipleCTEs()
    {
        var sql = @"
 WITH UserCTE AS (
              SELECT * FROM AllUsers
  ),
         OrderCTE AS (
   SELECT * FROM AllOrders
            )
     SELECT u.Name, o.OrderDate FROM Users u JOIN Orders o ON u.Id = o.UserId";

        var result = SqlInterrogator.ConvertSelectStatementToSelectCount(sql);

        _ = result.Should().Contain("SELECT COUNT(*) FROM");
        _ = result.Should().Contain("Users u");
    }

    [Fact]
    public void ConvertToSelectCount_WhenUseStatement()
    {
        var sql = @"
   USE MyDatabase;
 SELECT Name, Email FROM Users";

        var result = SqlInterrogator.ConvertSelectStatementToSelectCount(sql);

        _ = result.Should().Contain("SELECT COUNT(*) FROM");
        _ = result.Should().Contain("Users");
    }

    [Fact]
    public void ConvertToSelectCount_WhenUseWithGo()
    {
        var sql = @"
    USE MyDatabase
    GO
            SELECT * FROM Users";

        var result = SqlInterrogator.ConvertSelectStatementToSelectCount(sql);

        _ = result.Should().Contain("SELECT COUNT(*) FROM");
        _ = result.Should().Contain("Users");
    }

    [Fact]
    public void ConvertToSelectCount_WhenSubqueryInSelect()
    {
        var sql = "SELECT Name, (SELECT COUNT(*) FROM Orders WHERE Orders.UserId = Users.Id) AS OrderCount FROM Users";

        var result = SqlInterrogator.ConvertSelectStatementToSelectCount(sql);

        _ = result.Should().Contain("SELECT COUNT(*) FROM");
    }

    [Fact]
    public void ConvertToSelectCount_WhenSubqueryInWhere()
    {
        var sql = "SELECT * FROM Users WHERE Id IN (SELECT UserId FROM Orders WHERE Status = 'Active')";

        var result = SqlInterrogator.ConvertSelectStatementToSelectCount(sql);

        _ = result.Should().Be("SELECT COUNT(*) FROM Users WHERE Id IN (SELECT UserId FROM Orders WHERE Status = 'Active')");
    }

    [Fact]
    public void ConvertToSelectCount_WhenLowercaseKeywords()
    {
        var sql = "select name, email from users where active = 1";

        var result = SqlInterrogator.ConvertSelectStatementToSelectCount(sql);

        _ = result.Should().Contain("SELECT COUNT(*) from users where active = 1");
    }

    [Fact]
    public void ConvertToSelectCount_WhenMixedCaseKeywords()
    {
        var sql = "SeLeCt Name FrOm Users WhErE Active = 1";

        var result = SqlInterrogator.ConvertSelectStatementToSelectCount(sql);

        _ = result.Should().Contain("SELECT COUNT(*) FrOm Users WhErE Active = 1");
    }

    [Fact]
    public void ConvertToSelectCount_WhenComplexQueryWithAllClauses()
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

        var result = SqlInterrogator.ConvertSelectStatementToSelectCount(sql);

        _ = result.Should().Contain("SELECT COUNT(*) FROM Users u");
        _ = result.Should().Contain("LEFT JOIN Orders o ON u.Id = o.UserId");
        _ = result.Should().Contain("WHERE u.Active = 1");
        _ = result.Should().Contain("GROUP BY u.Name, u.Email");
        _ = result.Should().Contain("HAVING COUNT(o.Id) > 5");
        _ = result.Should().Contain("ORDER BY u.Name ASC");
    }

    [Fact]
    public void ConvertToSelectCount_WhenPaginationQuery()
    {
        var sql = @"
            SELECT * 
     FROM Users 
       WHERE Active = 1 
    ORDER BY CreatedDate DESC 
        OFFSET 0 ROWS 
    FETCH NEXT 10 ROWS ONLY";

        var result = SqlInterrogator.ConvertSelectStatementToSelectCount(sql);

        _ = result.Should().Contain("SELECT COUNT(*) FROM Users");
        _ = result.Should().Contain("WHERE Active = 1");
        _ = result.Should().Contain("ORDER BY CreatedDate DESC");
        _ = result.Should().Contain("OFFSET 0 ROWS");
        _ = result.Should().Contain("FETCH NEXT 10 ROWS ONLY");
    }

    [Fact]
    public void ConvertToSelectCount_WhenBracketedColumnWithSpaces()
    {
        var sql = "SELECT [User Name], [Email Address] FROM Users";

        var result = SqlInterrogator.ConvertSelectStatementToSelectCount(sql);

        _ = result.Should().Be("SELECT COUNT(*) FROM Users");
    }

    [Fact]
    public void ConvertToSelectCount_WhenDoubleQuotedIdentifiers()
    {
        var sql = "SELECT \"Name\", \"Email\" FROM \"Users\"";

        var result = SqlInterrogator.ConvertSelectStatementToSelectCount(sql);

        _ = result.Should().Be("SELECT COUNT(*) FROM \"Users\"");
    }

    [Fact]
    public void ConvertToSelectCount_WhenSpecialCharactersInStrings()
    {
        var sql = "SELECT * FROM Users WHERE Email = 'test@example.com' AND Name LIKE '%O''Brien%'";

        var result = SqlInterrogator.ConvertSelectStatementToSelectCount(sql);

        _ = result.Should().Be("SELECT COUNT(*) FROM Users WHERE Email = 'test@example.com' AND Name LIKE '%O''Brien%'");
    }

    [Fact]
    public void ConvertToSelectCount_WhenCrossJoin()
    {
        var sql = "SELECT u.Name, c.CategoryName FROM Users u CROSS JOIN Categories c";

        var result = SqlInterrogator.ConvertSelectStatementToSelectCount(sql);

        _ = result.Should().Be("SELECT COUNT(*) FROM Users u CROSS JOIN Categories c");
    }

    [Fact]
    public void ConvertToSelectCount_WhenUnion()
    {
        var sql = "SELECT Name FROM Users WHERE Active = 1 UNION SELECT Name FROM ArchivedUsers";

        var result = SqlInterrogator.ConvertSelectStatementToSelectCount(sql);

        _ = result.Should().Contain("SELECT COUNT(*) FROM Users WHERE Active = 1 UNION SELECT Name FROM ArchivedUsers");
    }

    [Fact]
    public void ConvertToSelectCount_WhenAllClause()
    {
        var sql = "SELECT ALL Name, Email FROM Users";

        var result = SqlInterrogator.ConvertSelectStatementToSelectCount(sql);

        _ = result.Should().Be("SELECT COUNT(*) FROM Users");
    }

    [Fact]
    public void ConvertToSelectCount_WhenTopPercent()
    {
        var sql = "SELECT TOP 50 PERCENT Name FROM Users ORDER BY CreatedDate DESC";

        var result = SqlInterrogator.ConvertSelectStatementToSelectCount(sql);

        _ = result.Should().Contain("SELECT COUNT(*) FROM Users");
        _ = result.Should().Contain("ORDER BY CreatedDate DESC");
    }

    [Fact]
    public void ConvertToSelectCount_WhenComplexNestedSubqueries()
    {
        var sql = @"SELECT * FROM Users WHERE Id IN 
    (SELECT UserId FROM Orders WHERE OrderId IN 
       (SELECT OrderId FROM OrderDetails WHERE ProductId = 1))";

        var result = SqlInterrogator.ConvertSelectStatementToSelectCount(sql);

        _ = result.Should().Contain("SELECT COUNT(*) FROM Users WHERE Id IN");
        _ = result.Should().Contain("(SELECT UserId FROM Orders WHERE OrderId IN");
    }

    [Fact]
    public void ConvertToSelectCount_WhenExistsClause()
    {
        var sql = "SELECT * FROM Users u WHERE EXISTS (SELECT 1 FROM Orders o WHERE o.UserId = u.Id)";

        var result = SqlInterrogator.ConvertSelectStatementToSelectCount(sql);

        _ = result.Should().Be("SELECT COUNT(*) FROM Users u WHERE EXISTS (SELECT 1 FROM Orders o WHERE o.UserId = u.Id)");
    }

    [Fact]
    public void ConvertToSelectCount_WhenBetweenOperator()
    {
        var sql = "SELECT * FROM Users WHERE Age BETWEEN 18 AND 65";

        var result = SqlInterrogator.ConvertSelectStatementToSelectCount(sql);

        _ = result.Should().Be("SELECT COUNT(*) FROM Users WHERE Age BETWEEN 18 AND 65");
    }

    [Fact]
    public void ConvertToSelectCount_WhenCaseInWhereClause()
    {
        var sql = @"SELECT * FROM Users 
       WHERE CASE WHEN Status = 1 THEN 'Active' ELSE 'Inactive' END = 'Active'";

        var result = SqlInterrogator.ConvertSelectStatementToSelectCount(sql);

        _ = result.Should().Contain("SELECT COUNT(*) FROM Users");
        _ = result.Should().Contain("WHERE CASE WHEN Status = 1 THEN 'Active' ELSE 'Inactive' END = 'Active'");
    }
}
