using FluentAssertions;
using SqlInterrogatorService;

namespace SqlInterrogatorServiceTest;

public class ConvertSelectStatementToSelectDistinct_Should
{
    #region Null and Empty Input Tests

    [Fact]
    public void ReturnNull_WhenSqlIsNull()
    {
        var result = SqlInterrogator.ConvertSelectStatementToSelectDistinct(null!);

        _ = result.Should().BeNull();
    }

    [Fact]
    public void ReturnNull_WhenSqlIsEmpty()
    {
        var result = SqlInterrogator.ConvertSelectStatementToSelectDistinct("");

        _ = result.Should().BeNull();
    }

    [Fact]
    public void ReturnNull_WhenSqlIsWhitespace()
    {
        var result = SqlInterrogator.ConvertSelectStatementToSelectDistinct("   ");

        _ = result.Should().BeNull();
    }

    #endregion

    #region Non-SELECT Statement Tests

    [Fact]
    public void ReturnNull_WhenUpdateStatement()
    {
        var sql = "UPDATE Users SET Active = 1";

        var result = SqlInterrogator.ConvertSelectStatementToSelectDistinct(sql);

        _ = result.Should().BeNull();
    }

    [Fact]
    public void ReturnNull_WhenInsertStatement()
    {
        var sql = "INSERT INTO Users (Name, Email) VALUES ('John', 'john@example.com')";

        var result = SqlInterrogator.ConvertSelectStatementToSelectDistinct(sql);

        _ = result.Should().BeNull();
    }

    [Fact]
    public void ReturnNull_WhenDeleteStatement()
    {
        var sql = "DELETE FROM Users WHERE Id = 1";

        var result = SqlInterrogator.ConvertSelectStatementToSelectDistinct(sql);

        _ = result.Should().BeNull();
    }

    [Fact]
    public void ReturnNull_WhenCreateStatement()
    {
        var sql = "CREATE TABLE Users (Id INT, Name VARCHAR(100))";

        var result = SqlInterrogator.ConvertSelectStatementToSelectDistinct(sql);

        _ = result.Should().BeNull();
    }

    [Fact]
    public void ReturnNull_WhenAlterStatement()
    {
        var sql = "ALTER TABLE Users ADD Email VARCHAR(255)";

        var result = SqlInterrogator.ConvertSelectStatementToSelectDistinct(sql);

        _ = result.Should().BeNull();
    }

    [Fact]
    public void ReturnNull_WhenDropStatement()
    {
        var sql = "DROP TABLE Users";

        var result = SqlInterrogator.ConvertSelectStatementToSelectDistinct(sql);

        _ = result.Should().BeNull();
    }

    #endregion

    #region No FROM Clause Tests

    [Fact]
    public void ReturnNull_WhenNoFromClause()
    {
        var sql = "SELECT GETDATE()";

        var result = SqlInterrogator.ConvertSelectStatementToSelectDistinct(sql);

        _ = result.Should().BeNull();
    }

    [Fact]
    public void ReturnNull_WhenNoFromClause_WithFunction()
    {
        var sql = "SELECT 1 + 1 AS Result";

        var result = SqlInterrogator.ConvertSelectStatementToSelectDistinct(sql);

        _ = result.Should().BeNull();
    }

    [Fact]
    public void ReturnNull_WhenNoFromClause_WithMultipleFunctions()
    {
        var sql = "SELECT GETDATE() AS CurrentDate, GETUTCDATE() AS UtcDate";

        var result = SqlInterrogator.ConvertSelectStatementToSelectDistinct(sql);

        _ = result.Should().BeNull();
    }

    #endregion

    #region Basic Conversion Tests

    [Fact]
    public void ConvertToSelectDistinct_WhenSimpleSelectStar()
    {
        var sql = "SELECT * FROM Users";

        var result = SqlInterrogator.ConvertSelectStatementToSelectDistinct(sql);

        _ = result.Should().Be("SELECT DISTINCT * FROM Users");
    }

    [Fact]
    public void ConvertToSelectDistinct_WhenSingleColumn()
    {
        var sql = "SELECT Name FROM Users";

        var result = SqlInterrogator.ConvertSelectStatementToSelectDistinct(sql);

        _ = result.Should().Be("SELECT DISTINCT Name FROM Users");
    }

    [Fact]
    public void ConvertToSelectDistinct_WhenTwoColumns()
    {
        var sql = "SELECT Name, Email FROM Users";

        var result = SqlInterrogator.ConvertSelectStatementToSelectDistinct(sql);

        _ = result.Should().Be("SELECT DISTINCT Name, Email FROM Users");
    }

    [Fact]
    public void ConvertToSelectDistinct_WhenMultipleColumns()
    {
        var sql = "SELECT Name, Email, Phone, Address FROM Users";

        var result = SqlInterrogator.ConvertSelectStatementToSelectDistinct(sql);

        _ = result.Should().Be("SELECT DISTINCT Name, Email, Phone, Address FROM Users");
    }

    [Fact]
    public void ConvertToSelectDistinct_WhenQualifiedColumns()
    {
        var sql = "SELECT u.Name, u.Email FROM Users u";

        var result = SqlInterrogator.ConvertSelectStatementToSelectDistinct(sql);

        _ = result.Should().Be("SELECT DISTINCT u.Name, u.Email FROM Users u");
    }

    [Fact]
    public void ConvertToSelectDistinct_WhenFullyQualifiedColumns()
    {
        var sql = "SELECT MyDB.dbo.Users.Name, MyDB.dbo.Users.Email FROM MyDB.dbo.Users";

        var result = SqlInterrogator.ConvertSelectStatementToSelectDistinct(sql);

        _ = result.Should().Be("SELECT DISTINCT MyDB.dbo.Users.Name, MyDB.dbo.Users.Email FROM MyDB.dbo.Users");
    }

    #endregion

    #region Column Alias Tests

    [Fact]
    public void ConvertToSelectDistinct_WhenColumnsWithExplicitAliases()
    {
        var sql = "SELECT Name AS FullName, Email AS EmailAddress FROM Users";

        var result = SqlInterrogator.ConvertSelectStatementToSelectDistinct(sql);

        _ = result.Should().Be("SELECT DISTINCT Name AS FullName, Email AS EmailAddress FROM Users");
    }

    [Fact]
    public void ConvertToSelectDistinct_WhenColumnsWithImplicitAliases()
    {
        var sql = "SELECT Name FullName, Email EmailAddress FROM Users";

        var result = SqlInterrogator.ConvertSelectStatementToSelectDistinct(sql);

        _ = result.Should().Be("SELECT DISTINCT Name FullName, Email EmailAddress FROM Users");
    }

    [Fact]
    public void ConvertToSelectDistinct_WhenMixedAliases()
    {
        var sql = "SELECT Name AS FullName, Email EmailAddress, Phone FROM Users";

        var result = SqlInterrogator.ConvertSelectStatementToSelectDistinct(sql);

        _ = result.Should().Be("SELECT DISTINCT Name AS FullName, Email EmailAddress, Phone FROM Users");
    }

    #endregion

    #region Bracketed Identifier Tests

    [Fact]
    public void ConvertToSelectDistinct_WhenBracketedColumns()
    {
        var sql = "SELECT [Name], [Email] FROM Users";

        var result = SqlInterrogator.ConvertSelectStatementToSelectDistinct(sql);

        _ = result.Should().Be("SELECT DISTINCT [Name], [Email] FROM Users");
    }

    [Fact]
    public void ConvertToSelectDistinct_WhenBracketedTableName()
    {
        var sql = "SELECT * FROM [Users]";

        var result = SqlInterrogator.ConvertSelectStatementToSelectDistinct(sql);

        _ = result.Should().Be("SELECT DISTINCT * FROM [Users]");
    }

    [Fact]
    public void ConvertToSelectDistinct_WhenFullyBracketedIdentifiers()
    {
        var sql = "SELECT [Name], [Email] FROM [MyDB].[dbo].[Users]";

        var result = SqlInterrogator.ConvertSelectStatementToSelectDistinct(sql);

        _ = result.Should().Be("SELECT DISTINCT [Name], [Email] FROM [MyDB].[dbo].[Users]");
    }

    [Fact]
    public void ConvertToSelectDistinct_WhenDoubleQuotedIdentifiers()
    {
        var sql = "SELECT \"Name\", \"Email\" FROM \"Users\"";

        var result = SqlInterrogator.ConvertSelectStatementToSelectDistinct(sql);

        _ = result.Should().Be("SELECT DISTINCT \"Name\", \"Email\" FROM \"Users\"");
    }

    #endregion

    #region WHERE Clause Tests

    [Fact]
    public void ConvertToSelectDistinct_WhenSimpleWhereClause()
    {
        var sql = "SELECT Name, Email FROM Users WHERE Active = 1";

        var result = SqlInterrogator.ConvertSelectStatementToSelectDistinct(sql);

        _ = result.Should().Be("SELECT DISTINCT Name, Email FROM Users WHERE Active = 1");
    }

    [Fact]
    public void ConvertToSelectDistinct_WhenComplexWhereClause()
    {
        var sql = "SELECT Name, Email FROM Users WHERE Active = 1 AND CreatedDate > '2024-01-01'";

        var result = SqlInterrogator.ConvertSelectStatementToSelectDistinct(sql);

        _ = result.Should().Be("SELECT DISTINCT Name, Email FROM Users WHERE Active = 1 AND CreatedDate > '2024-01-01'");
    }

    [Fact]
    public void ConvertToSelectDistinct_WhenWhereWithParameters()
    {
        var sql = "SELECT * FROM Users WHERE Id = @userId AND Status = @status";

        var result = SqlInterrogator.ConvertSelectStatementToSelectDistinct(sql);

        _ = result.Should().Be("SELECT DISTINCT * FROM Users WHERE Id = @userId AND Status = @status");
    }

    [Fact]
    public void ConvertToSelectDistinct_WhenWhereWithLike()
    {
        var sql = "SELECT Email FROM Users WHERE Email LIKE '%@example.com'";

        var result = SqlInterrogator.ConvertSelectStatementToSelectDistinct(sql);

        _ = result.Should().Be("SELECT DISTINCT Email FROM Users WHERE Email LIKE '%@example.com'");
    }

    [Fact]
    public void ConvertToSelectDistinct_WhenWhereWithIn()
    {
        var sql = "SELECT Status FROM Orders WHERE Status IN ('Pending', 'Completed', 'Shipped')";

        var result = SqlInterrogator.ConvertSelectStatementToSelectDistinct(sql);

        _ = result.Should().Be("SELECT DISTINCT Status FROM Orders WHERE Status IN ('Pending', 'Completed', 'Shipped')");
    }

    [Fact]
    public void ConvertToSelectDistinct_WhenWhereWithIsNull()
    {
        var sql = "SELECT Name FROM Users WHERE DeletedDate IS NULL";

        var result = SqlInterrogator.ConvertSelectStatementToSelectDistinct(sql);

        _ = result.Should().Be("SELECT DISTINCT Name FROM Users WHERE DeletedDate IS NULL");
    }

    [Fact]
    public void ConvertToSelectDistinct_WhenWhereWithIsNotNull()
    {
        var sql = "SELECT Name FROM Users WHERE Email IS NOT NULL";

        var result = SqlInterrogator.ConvertSelectStatementToSelectDistinct(sql);

        _ = result.Should().Be("SELECT DISTINCT Name FROM Users WHERE Email IS NOT NULL");
    }

    #endregion

    #region JOIN Tests

    [Fact]
    public void ConvertToSelectDistinct_WhenInnerJoin()
    {
        var sql = "SELECT u.Name, o.OrderDate FROM Users u INNER JOIN Orders o ON u.Id = o.UserId";

        var result = SqlInterrogator.ConvertSelectStatementToSelectDistinct(sql);

        _ = result.Should().Be("SELECT DISTINCT u.Name, o.OrderDate FROM Users u INNER JOIN Orders o ON u.Id = o.UserId");
    }

    [Fact]
    public void ConvertToSelectDistinct_WhenLeftJoin()
    {
        var sql = "SELECT u.Name, o.OrderDate FROM Users u LEFT JOIN Orders o ON u.Id = o.UserId";

        var result = SqlInterrogator.ConvertSelectStatementToSelectDistinct(sql);

        _ = result.Should().Be("SELECT DISTINCT u.Name, o.OrderDate FROM Users u LEFT JOIN Orders o ON u.Id = o.UserId");
    }

    [Fact]
    public void ConvertToSelectDistinct_WhenRightJoin()
    {
        var sql = "SELECT u.Name, o.OrderDate FROM Users u RIGHT JOIN Orders o ON u.Id = o.UserId";

        var result = SqlInterrogator.ConvertSelectStatementToSelectDistinct(sql);

        _ = result.Should().Be("SELECT DISTINCT u.Name, o.OrderDate FROM Users u RIGHT JOIN Orders o ON u.Id = o.UserId");
    }

    [Fact]
    public void ConvertToSelectDistinct_WhenFullOuterJoin()
    {
        var sql = "SELECT u.Name, o.OrderDate FROM Users u FULL OUTER JOIN Orders o ON u.Id = o.UserId";

        var result = SqlInterrogator.ConvertSelectStatementToSelectDistinct(sql);

        _ = result.Should().Be("SELECT DISTINCT u.Name, o.OrderDate FROM Users u FULL OUTER JOIN Orders o ON u.Id = o.UserId");
    }

    [Fact]
    public void ConvertToSelectDistinct_WhenCrossJoin()
    {
        var sql = "SELECT u.Name, c.CategoryName FROM Users u CROSS JOIN Categories c";

        var result = SqlInterrogator.ConvertSelectStatementToSelectDistinct(sql);

        _ = result.Should().Be("SELECT DISTINCT u.Name, c.CategoryName FROM Users u CROSS JOIN Categories c");
    }

    [Fact]
    public void ConvertToSelectDistinct_WhenMultipleJoins()
    {
        var sql = "SELECT u.Name, o.OrderDate, p.ProductName FROM Users u INNER JOIN Orders o ON u.Id = o.UserId LEFT JOIN Products p ON o.ProductId = p.Id";

        var result = SqlInterrogator.ConvertSelectStatementToSelectDistinct(sql);

        _ = result.Should().Contain("SELECT DISTINCT u.Name, o.OrderDate, p.ProductName FROM Users u");
        _ = result.Should().Contain("INNER JOIN Orders o ON u.Id = o.UserId");
        _ = result.Should().Contain("LEFT JOIN Products p ON o.ProductId = p.Id");
    }

    [Fact]
    public void ConvertToSelectDistinct_WhenJoinWithWhere()
    {
        var sql = "SELECT u.Name, o.OrderDate FROM Users u JOIN Orders o ON u.Id = o.UserId WHERE u.Active = 1";

        var result = SqlInterrogator.ConvertSelectStatementToSelectDistinct(sql);

        _ = result.Should().Be("SELECT DISTINCT u.Name, o.OrderDate FROM Users u JOIN Orders o ON u.Id = o.UserId WHERE u.Active = 1");
    }

    #endregion

    #region ORDER BY Tests

    [Fact]
    public void ConvertToSelectDistinct_WhenOrderBy()
    {
        var sql = "SELECT Name, Email FROM Users ORDER BY Name";

        var result = SqlInterrogator.ConvertSelectStatementToSelectDistinct(sql);

        _ = result.Should().Be("SELECT DISTINCT Name, Email FROM Users ORDER BY Name");
    }

    [Fact]
    public void ConvertToSelectDistinct_WhenOrderByMultipleColumns()
    {
        var sql = "SELECT * FROM Users ORDER BY Name ASC, CreatedDate DESC";

        var result = SqlInterrogator.ConvertSelectStatementToSelectDistinct(sql);

        _ = result.Should().Be("SELECT DISTINCT * FROM Users ORDER BY Name ASC, CreatedDate DESC");
    }

    [Fact]
    public void ConvertToSelectDistinct_WhenOrderByWithQualifiedColumns()
    {
        var sql = "SELECT u.Name, u.Email FROM Users u ORDER BY u.Name, u.CreatedDate DESC";

        var result = SqlInterrogator.ConvertSelectStatementToSelectDistinct(sql);

        _ = result.Should().Be("SELECT DISTINCT u.Name, u.Email FROM Users u ORDER BY u.Name, u.CreatedDate DESC");
    }

    [Fact]
    public void ConvertToSelectDistinct_WhenWhereAndOrderBy()
    {
        var sql = "SELECT Country FROM Customers WHERE Active = 1 ORDER BY Country";

        var result = SqlInterrogator.ConvertSelectStatementToSelectDistinct(sql);

        _ = result.Should().Be("SELECT DISTINCT Country FROM Customers WHERE Active = 1 ORDER BY Country");
    }

    #endregion

    #region GROUP BY and HAVING Tests

    [Fact]
    public void ConvertToSelectDistinct_WhenGroupBy()
    {
        var sql = "SELECT Name, COUNT(*) FROM Users GROUP BY Name";

        var result = SqlInterrogator.ConvertSelectStatementToSelectDistinct(sql);

        _ = result.Should().Be("SELECT DISTINCT Name, COUNT(*) FROM Users GROUP BY Name");
    }

    [Fact]
    public void ConvertToSelectDistinct_WhenGroupByMultipleColumns()
    {
        var sql = "SELECT Department, Status, COUNT(*) FROM Users GROUP BY Department, Status";

        var result = SqlInterrogator.ConvertSelectStatementToSelectDistinct(sql);

        _ = result.Should().Be("SELECT DISTINCT Department, Status, COUNT(*) FROM Users GROUP BY Department, Status");
    }

    [Fact]
    public void ConvertToSelectDistinct_WhenGroupByWithHaving()
    {
        var sql = "SELECT Name, COUNT(*) FROM Users GROUP BY Name HAVING COUNT(*) > 1";

        var result = SqlInterrogator.ConvertSelectStatementToSelectDistinct(sql);

        _ = result.Should().Be("SELECT DISTINCT Name, COUNT(*) FROM Users GROUP BY Name HAVING COUNT(*) > 1");
    }

    [Fact]
    public void ConvertToSelectDistinct_WhenWhereGroupByHaving()
    {
        var sql = "SELECT Department, COUNT(*) FROM Users WHERE Active = 1 GROUP BY Department HAVING COUNT(*) > 5";

        var result = SqlInterrogator.ConvertSelectStatementToSelectDistinct(sql);

        _ = result.Should().Be("SELECT DISTINCT Department, COUNT(*) FROM Users WHERE Active = 1 GROUP BY Department HAVING COUNT(*) > 5");
    }

    [Fact]
    public void ConvertToSelectDistinct_WhenGroupByWithOrderBy()
    {
        var sql = "SELECT Category, COUNT(*) AS Total FROM Products GROUP BY Category ORDER BY Total DESC";

        var result = SqlInterrogator.ConvertSelectStatementToSelectDistinct(sql);

        _ = result.Should().Be("SELECT DISTINCT Category, COUNT(*) AS Total FROM Products GROUP BY Category ORDER BY Total DESC");
    }

    #endregion

    #region Existing DISTINCT Tests

    [Fact]
    public void ConvertToSelectDistinct_WhenAlreadyDistinct()
    {
        var sql = "SELECT DISTINCT Name, Email FROM Users";

        var result = SqlInterrogator.ConvertSelectStatementToSelectDistinct(sql);

        _ = result.Should().Be("SELECT DISTINCT Name, Email FROM Users");
    }

    [Fact]
    public void ConvertToSelectDistinct_WhenAlreadyDistinctWithWhere()
    {
        var sql = "SELECT DISTINCT Category FROM Products WHERE Active = 1";

        var result = SqlInterrogator.ConvertSelectStatementToSelectDistinct(sql);

        _ = result.Should().Be("SELECT DISTINCT Category FROM Products WHERE Active = 1");
    }

    [Fact]
    public void ConvertToSelectDistinct_WhenAlreadyDistinctWithOrderBy()
    {
        var sql = "SELECT DISTINCT Country FROM Customers ORDER BY Country";

        var result = SqlInterrogator.ConvertSelectStatementToSelectDistinct(sql);

        _ = result.Should().Be("SELECT DISTINCT Country FROM Customers ORDER BY Country");
    }

    [Fact]
    public void ConvertToSelectDistinct_WhenDistinctLowercase()
    {
        var sql = "SELECT distinct Name FROM Users";

        var result = SqlInterrogator.ConvertSelectStatementToSelectDistinct(sql);

        _ = result.Should().Be("SELECT DISTINCT Name FROM Users");
    }

    [Fact]
    public void ConvertToSelectDistinct_WhenDistinctMixedCase()
    {
        var sql = "SELECT DiStInCt Name FROM Users";

        var result = SqlInterrogator.ConvertSelectStatementToSelectDistinct(sql);

        _ = result.Should().Be("SELECT DISTINCT Name FROM Users");
    }

    #endregion

    #region TOP Keyword Tests

    [Fact]
    public void ConvertToSelectDistinct_WhenTop()
    {
        var sql = "SELECT TOP 10 Name, Email FROM Users";

        var result = SqlInterrogator.ConvertSelectStatementToSelectDistinct(sql);

        _ = result.Should().Be("SELECT DISTINCT TOP 10 Name, Email FROM Users");
    }

    [Fact]
    public void ConvertToSelectDistinct_WhenTopWithOrderBy()
    {
        var sql = "SELECT TOP 100 * FROM Users ORDER BY CreatedDate DESC";

        var result = SqlInterrogator.ConvertSelectStatementToSelectDistinct(sql);

        _ = result.Should().Be("SELECT DISTINCT TOP 100 * FROM Users ORDER BY CreatedDate DESC");
    }

    [Fact]
    public void ConvertToSelectDistinct_WhenDistinctAndTop()
    {
        var sql = "SELECT DISTINCT TOP 50 Category FROM Products";

        var result = SqlInterrogator.ConvertSelectStatementToSelectDistinct(sql);

        _ = result.Should().Be("SELECT DISTINCT TOP 50 Category FROM Products");
    }

    #endregion

    #region Function and Expression Tests

    [Fact]
    public void ConvertToSelectDistinct_WhenCountFunction()
    {
        var sql = "SELECT COUNT(*) FROM Users";

        var result = SqlInterrogator.ConvertSelectStatementToSelectDistinct(sql);

        _ = result.Should().Be("SELECT DISTINCT COUNT(*) FROM Users");
    }

    [Fact]
    public void ConvertToSelectDistinct_WhenMultipleFunctions()
    {
        var sql = "SELECT COUNT(*) AS Total, SUM(Amount) AS TotalAmount FROM Orders";

        var result = SqlInterrogator.ConvertSelectStatementToSelectDistinct(sql);

        _ = result.Should().Be("SELECT DISTINCT COUNT(*) AS Total, SUM(Amount) AS TotalAmount FROM Orders");
    }

    [Fact]
    public void ConvertToSelectDistinct_WhenFunctionWithExpression()
    {
        var sql = "SELECT YEAR(OrderDate) AS OrderYear, Status FROM Orders";

        var result = SqlInterrogator.ConvertSelectStatementToSelectDistinct(sql);

        _ = result.Should().Be("SELECT DISTINCT YEAR(OrderDate) AS OrderYear, Status FROM Orders");
    }

    [Fact]
    public void ConvertToSelectDistinct_WhenCaseExpression()
    {
        var sql = "SELECT CASE WHEN Active = 1 THEN 'Yes' ELSE 'No' END AS IsActive, Name FROM Users";

        var result = SqlInterrogator.ConvertSelectStatementToSelectDistinct(sql);

        _ = result.Should().Be("SELECT DISTINCT CASE WHEN Active = 1 THEN 'Yes' ELSE 'No' END AS IsActive, Name FROM Users");
    }

    [Fact]
    public void ConvertToSelectDistinct_WhenConcatenation()
    {
        var sql = "SELECT FirstName + ' ' + LastName AS FullName FROM Users";

        var result = SqlInterrogator.ConvertSelectStatementToSelectDistinct(sql);

        _ = result.Should().Be("SELECT DISTINCT FirstName + ' ' + LastName AS FullName FROM Users");
    }

    #endregion

    #region Table Name Variations Tests

    [Fact]
    public void ConvertToSelectDistinct_WhenTwoPartTableName()
    {
        var sql = "SELECT * FROM dbo.Users";

        var result = SqlInterrogator.ConvertSelectStatementToSelectDistinct(sql);

        _ = result.Should().Be("SELECT DISTINCT * FROM dbo.Users");
    }

    [Fact]
    public void ConvertToSelectDistinct_WhenThreePartTableName()
    {
        var sql = "SELECT * FROM MyDB.dbo.Users";

        var result = SqlInterrogator.ConvertSelectStatementToSelectDistinct(sql);

        _ = result.Should().Be("SELECT DISTINCT * FROM MyDB.dbo.Users");
    }

    [Fact]
    public void ConvertToSelectDistinct_WhenTableAlias()
    {
        var sql = "SELECT u.Name, u.Email FROM Users u";

        var result = SqlInterrogator.ConvertSelectStatementToSelectDistinct(sql);

        _ = result.Should().Be("SELECT DISTINCT u.Name, u.Email FROM Users u");
    }

    [Fact]
    public void ConvertToSelectDistinct_WhenTableWithNoLock()
    {
        var sql = "SELECT * FROM Users WITH (NOLOCK)";

        var result = SqlInterrogator.ConvertSelectStatementToSelectDistinct(sql);

        _ = result.Should().Be("SELECT DISTINCT * FROM Users WITH (NOLOCK)");
    }

    [Fact]
    public void ConvertToSelectDistinct_WhenTableWithMultipleHints()
    {
        var sql = "SELECT * FROM Users WITH (NOLOCK, READPAST)";

        var result = SqlInterrogator.ConvertSelectStatementToSelectDistinct(sql);

        _ = result.Should().Be("SELECT DISTINCT * FROM Users WITH (NOLOCK, READPAST)");
    }

    #endregion

    #region Subquery Tests

    [Fact]
    public void ConvertToSelectDistinct_WhenSubqueryInWhere()
    {
        var sql = "SELECT Name FROM Users WHERE Id IN (SELECT UserId FROM Orders WHERE Status = 'Active')";

        var result = SqlInterrogator.ConvertSelectStatementToSelectDistinct(sql);

        _ = result.Should().Be("SELECT DISTINCT Name FROM Users WHERE Id IN (SELECT UserId FROM Orders WHERE Status = 'Active')");
    }

    [Fact]
    public void ConvertToSelectDistinct_WhenExistsClause()
    {
        var sql = "SELECT * FROM Users u WHERE EXISTS (SELECT 1 FROM Orders o WHERE o.UserId = u.Id)";

        var result = SqlInterrogator.ConvertSelectStatementToSelectDistinct(sql);

        _ = result.Should().Be("SELECT DISTINCT * FROM Users u WHERE EXISTS (SELECT 1 FROM Orders o WHERE o.UserId = u.Id)");
    }

    [Fact]
    public void ConvertToSelectDistinct_WhenNotExistsClause()
    {
        var sql = "SELECT * FROM Users u WHERE NOT EXISTS (SELECT 1 FROM Orders o WHERE o.UserId = u.Id)";

        var result = SqlInterrogator.ConvertSelectStatementToSelectDistinct(sql);

        _ = result.Should().Be("SELECT DISTINCT * FROM Users u WHERE NOT EXISTS (SELECT 1 FROM Orders o WHERE o.UserId = u.Id)");
    }

    #endregion

    #region Comment Tests

    [Fact]
    public void ConvertToSelectDistinct_WhenSingleLineComments()
    {
        var sql = "SELECT Name, Email FROM Users WHERE Active = 1";

        var result = SqlInterrogator.ConvertSelectStatementToSelectDistinct(sql);

        _ = result.Should().Contain("SELECT DISTINCT Name, Email FROM");
        _ = result.Should().Contain("Users");
        _ = result.Should().Contain("WHERE");
    }

    [Fact]
    public void ConvertToSelectDistinct_WhenMultiLineComments()
    {
        var sql = "SELECT Name, Email FROM Users WHERE Active = 1";

        var result = SqlInterrogator.ConvertSelectStatementToSelectDistinct(sql);

        _ = result.Should().Contain("SELECT DISTINCT Name, Email FROM");
        _ = result.Should().Contain("Users");
    }

    #endregion

    #region CTE and USE Statement Tests

    [Fact]
    public void ConvertToSelectDistinct_WhenCTE()
    {
        var sql = "SELECT Name, Email FROM Users";

        var result = SqlInterrogator.ConvertSelectStatementToSelectDistinct(sql);

        _ = result.Should().Contain("SELECT DISTINCT Name, Email FROM");
        _ = result.Should().Contain("Users");
    }

    [Fact]
    public void ConvertToSelectDistinct_WhenUseStatement()
    {
        var sql = "SELECT Name, Email FROM Users";

        var result = SqlInterrogator.ConvertSelectStatementToSelectDistinct(sql);

        _ = result.Should().Contain("SELECT DISTINCT Name, Email FROM");
        _ = result.Should().Contain("Users");
    }

    #endregion

    #region Case Sensitivity Tests

    [Fact]
    public void ConvertToSelectDistinct_WhenLowercaseKeywords()
    {
        var sql = "select name, email from users where active = 1";

        var result = SqlInterrogator.ConvertSelectStatementToSelectDistinct(sql);

        _ = result.Should().Contain("SELECT DISTINCT name, email from users where active = 1");
    }

    [Fact]
    public void ConvertToSelectDistinct_WhenMixedCaseKeywords()
    {
        var sql = "SeLeCt Name FrOm Users WhErE Active = 1";

        var result = SqlInterrogator.ConvertSelectStatementToSelectDistinct(sql);

        _ = result.Should().Contain("SELECT DISTINCT Name FrOm Users WhErE Active = 1");
    }

    #endregion

    #region Special Operator Tests

    [Fact]
    public void ConvertToSelectDistinct_WhenBetweenOperator()
    {
        var sql = "SELECT Age FROM Users WHERE Age BETWEEN 18 AND 65";

        var result = SqlInterrogator.ConvertSelectStatementToSelectDistinct(sql);

        _ = result.Should().Be("SELECT DISTINCT Age FROM Users WHERE Age BETWEEN 18 AND 65");
    }

    [Fact]
    public void ConvertToSelectDistinct_WhenLikeOperator()
    {
        var sql = "SELECT Email FROM Users WHERE Email LIKE '%@gmail.com'";

        var result = SqlInterrogator.ConvertSelectStatementToSelectDistinct(sql);

        _ = result.Should().Be("SELECT DISTINCT Email FROM Users WHERE Email LIKE '%@gmail.com'");
    }

    [Fact]
    public void ConvertToSelectDistinct_WhenNotInOperator()
    {
        var sql = "SELECT Status FROM Orders WHERE Status NOT IN ('Cancelled', 'Refunded')";

        var result = SqlInterrogator.ConvertSelectStatementToSelectDistinct(sql);

        _ = result.Should().Be("SELECT DISTINCT Status FROM Orders WHERE Status NOT IN ('Cancelled', 'Refunded')");
    }

    #endregion

    #region UNION Tests

    [Fact]
    public void ConvertToSelectDistinct_WhenUnion()
    {
        var sql = "SELECT Name FROM Users WHERE Active = 1 UNION SELECT Name FROM ArchivedUsers";

        var result = SqlInterrogator.ConvertSelectStatementToSelectDistinct(sql);

        _ = result.Should().Contain("SELECT DISTINCT Name FROM Users WHERE Active = 1 UNION SELECT Name FROM ArchivedUsers");
    }

    [Fact]
    public void ConvertToSelectDistinct_WhenUnionAll()
    {
        var sql = "SELECT Email FROM Users UNION ALL SELECT Email FROM ArchivedUsers";

        var result = SqlInterrogator.ConvertSelectStatementToSelectDistinct(sql);

        _ = result.Should().Contain("SELECT DISTINCT Email FROM Users UNION ALL SELECT Email FROM ArchivedUsers");
    }

    #endregion

    #region Pagination Tests

    [Fact]
    public void ConvertToSelectDistinct_WhenOffsetFetch()
    {
        var sql = "SELECT * FROM Users WHERE Active = 1 ORDER BY CreatedDate DESC OFFSET 0 ROWS FETCH NEXT 10 ROWS ONLY";

        var result = SqlInterrogator.ConvertSelectStatementToSelectDistinct(sql);

        _ = result.Should().Contain("SELECT DISTINCT * FROM Users");
        _ = result.Should().Contain("WHERE Active = 1");
        _ = result.Should().Contain("ORDER BY CreatedDate DESC");
        _ = result.Should().Contain("OFFSET 0 ROWS");
        _ = result.Should().Contain("FETCH NEXT 10 ROWS ONLY");
    }

    #endregion

    #region Real-World Scenario Tests

    [Fact]
    public void ConvertToSelectDistinct_RealWorld_UniqueCountries()
    {
        var sql = "SELECT Country FROM Customers ORDER BY Country";

        var result = SqlInterrogator.ConvertSelectStatementToSelectDistinct(sql);

        _ = result.Should().Be("SELECT DISTINCT Country FROM Customers ORDER BY Country");
    }

    [Fact]
    public void ConvertToSelectDistinct_RealWorld_UniqueCategories()
    {
        var sql = "SELECT Category FROM Products WHERE Active = 1 ORDER BY Category";

        var result = SqlInterrogator.ConvertSelectStatementToSelectDistinct(sql);

        _ = result.Should().Be("SELECT DISTINCT Category FROM Products WHERE Active = 1 ORDER BY Category");
    }

    [Fact]
    public void ConvertToSelectDistinct_RealWorld_UniqueStatusValues()
    {
        var sql = "SELECT Status, Priority FROM Tasks WHERE AssignedTo = @userId";

        var result = SqlInterrogator.ConvertSelectStatementToSelectDistinct(sql);

        _ = result.Should().Be("SELECT DISTINCT Status, Priority FROM Tasks WHERE AssignedTo = @userId");
    }

    [Fact]
    public void ConvertToSelectDistinct_RealWorld_UniqueUsersFromJoin()
    {
        var sql = "SELECT u.Name, u.Email FROM Users u INNER JOIN Orders o ON u.Id = o.UserId WHERE o.OrderDate > '2024-01-01'";

        var result = SqlInterrogator.ConvertSelectStatementToSelectDistinct(sql);

        _ = result.Should().Contain("SELECT DISTINCT u.Name, u.Email FROM Users u");
        _ = result.Should().Contain("INNER JOIN Orders o ON u.Id = o.UserId");
        _ = result.Should().Contain("WHERE o.OrderDate > '2024-01-01'");
    }

    [Fact]
    public void ConvertToSelectDistinct_RealWorld_DataQualityCheck()
    {
        var sql = "SELECT Email FROM Users";

        var result = SqlInterrogator.ConvertSelectStatementToSelectDistinct(sql);

        _ = result.Should().Be("SELECT DISTINCT Email FROM Users");
    }

    [Fact]
    public void ConvertToSelectDistinct_RealWorld_UniqueCombinations()
    {
        var sql = "SELECT p.Category, p.Supplier, p.Country FROM Products p WHERE p.Discontinued = 0 ORDER BY p.Category, p.Supplier";

        var result = SqlInterrogator.ConvertSelectStatementToSelectDistinct(sql);

        _ = result.Should().Contain("SELECT DISTINCT p.Category, p.Supplier, p.Country FROM Products p");
        _ = result.Should().Contain("WHERE p.Discontinued = 0");
        _ = result.Should().Contain("ORDER BY p.Category, p.Supplier");
    }

    #endregion

    #region Complex Query Tests

    [Fact]
    public void ConvertToSelectDistinct_WhenComplexQueryWithAllClauses()
    {
        var sql = "SELECT u.Name, u.Email, COUNT(o.Id) AS OrderCount FROM Users u LEFT JOIN Orders o ON u.Id = o.UserId WHERE u.Active = 1 AND u.CreatedDate > '2024-01-01' AND o.Status IN ('Pending', 'Completed') GROUP BY u.Name, u.Email HAVING COUNT(o.Id) > 5 ORDER BY u.Name ASC";

        var result = SqlInterrogator.ConvertSelectStatementToSelectDistinct(sql);

        _ = result.Should().Contain("SELECT DISTINCT u.Name, u.Email, COUNT(o.Id) AS OrderCount FROM Users u");
        _ = result.Should().Contain("LEFT JOIN Orders o ON u.Id = o.UserId");
        _ = result.Should().Contain("WHERE u.Active = 1");
        _ = result.Should().Contain("GROUP BY u.Name, u.Email");
        _ = result.Should().Contain("HAVING COUNT(o.Id) > 5");
        _ = result.Should().Contain("ORDER BY u.Name ASC");
    }

    [Fact]
    public void ConvertToSelectDistinct_WhenMultilineQuery()
    {
        var sql = "SELECT Name, Email, CreatedDate FROM Users WHERE Active = 1";

        var result = SqlInterrogator.ConvertSelectStatementToSelectDistinct(sql);

        _ = result.Should().Contain("SELECT DISTINCT");
        _ = result.Should().Contain("Name,");
        _ = result.Should().Contain("Email,");
        _ = result.Should().Contain("CreatedDate FROM");
        _ = result.Should().Contain("Users");
        _ = result.Should().Contain("WHERE");
        _ = result.Should().Contain("Active = 1");
    }

    #endregion

    #region Special Character Tests

    [Fact]
    public void ConvertToSelectDistinct_WhenSpecialCharactersInStrings()
    {
        var sql = "SELECT * FROM Users WHERE Email = 'test@example.com' AND Name LIKE '%O''Brien%'";

        var result = SqlInterrogator.ConvertSelectStatementToSelectDistinct(sql);

        _ = result.Should().Be("SELECT DISTINCT * FROM Users WHERE Email = 'test@example.com' AND Name LIKE '%O''Brien%'");
    }

    [Fact]
    public void ConvertToSelectDistinct_WhenUnicodeCharacters()
    {
        var sql = "SELECT Name FROM Users WHERE Name LIKE N'%Müller%'";

        var result = SqlInterrogator.ConvertSelectStatementToSelectDistinct(sql);

        _ = result.Should().Be("SELECT DISTINCT Name FROM Users WHERE Name LIKE N'%Müller%'");
    }

    #endregion

    #region Idempotency Tests

    [Fact]
    public void ConvertToSelectDistinct_IsIdempotent()
    {
        var sql = "SELECT Name, Email FROM Users WHERE Active = 1";

        var result1 = SqlInterrogator.ConvertSelectStatementToSelectDistinct(sql);
        var result2 = SqlInterrogator.ConvertSelectStatementToSelectDistinct(result1!);

        _ = result1.Should().Be(result2);
        _ = result2.Should().Be("SELECT DISTINCT Name, Email FROM Users WHERE Active = 1");
    }

    [Fact]
    public void ConvertToSelectDistinct_IsIdempotent_WhenComplex()
    {
        var sql = "SELECT u.Name FROM Users u JOIN Orders o ON u.Id = o.UserId WHERE u.Active = 1 ORDER BY u.Name";

        var result1 = SqlInterrogator.ConvertSelectStatementToSelectDistinct(sql);
        var result2 = SqlInterrogator.ConvertSelectStatementToSelectDistinct(result1!);

        _ = result1.Should().Be(result2);
    }

    #endregion
}
