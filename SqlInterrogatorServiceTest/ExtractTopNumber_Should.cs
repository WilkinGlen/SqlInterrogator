using FluentAssertions;
using SqlInterrogatorService;

namespace SqlInterrogatorServiceTest;

public class ExtractTopNumber_Should
{
    [Fact]
    public void ReturnZero_WhenSqlIsNull()
    {
        var result = SqlInterrogator.ExtractTopNumber(null!);

        _ = result.Should().Be(0);
    }

    [Fact]
    public void ReturnZero_WhenSqlIsEmpty()
    {
        var result = SqlInterrogator.ExtractTopNumber("");

        _ = result.Should().Be(0);
    }

    [Fact]
    public void ReturnZero_WhenSqlIsWhitespace()
    {
        var result = SqlInterrogator.ExtractTopNumber("   ");

        _ = result.Should().Be(0);
    }

    [Fact]
    public void ReturnZero_WhenUpdateStatement()
    {
        var sql = "UPDATE Users SET Active = 1";

        var result = SqlInterrogator.ExtractTopNumber(sql);

        _ = result.Should().Be(0);
    }

    [Fact]
    public void ReturnZero_WhenInsertStatement()
    {
        var sql = "INSERT INTO Users (Name, Email) VALUES ('John', 'john@example.com')";

        var result = SqlInterrogator.ExtractTopNumber(sql);

        _ = result.Should().Be(0);
    }

    [Fact]
    public void ReturnZero_WhenDeleteStatement()
    {
        var sql = "DELETE FROM Users WHERE Id = 1";

        var result = SqlInterrogator.ExtractTopNumber(sql);

        _ = result.Should().Be(0);
    }

    [Fact]
    public void ReturnZero_WhenNoTopClause()
    {
        var sql = "SELECT * FROM Users";

        var result = SqlInterrogator.ExtractTopNumber(sql);

        _ = result.Should().Be(0);
    }

    [Fact]
    public void ReturnZero_WhenNoTopClauseWithWhere()
    {
        var sql = "SELECT Name, Email FROM Users WHERE Active = 1";

        var result = SqlInterrogator.ExtractTopNumber(sql);

        _ = result.Should().Be(0);
    }

    [Fact]
    public void ExtractTop_WhenTop1()
    {
        var sql = "SELECT TOP 1 * FROM Users";

        var result = SqlInterrogator.ExtractTopNumber(sql);

        _ = result.Should().Be(1);
    }

    [Fact]
    public void ExtractTop_WhenTop10()
    {
        var sql = "SELECT TOP 10 Name, Email FROM Users";

        var result = SqlInterrogator.ExtractTopNumber(sql);

        _ = result.Should().Be(10);
    }

    [Fact]
    public void ExtractTop_WhenTop100()
    {
        var sql = "SELECT TOP 100 * FROM Users ORDER BY CreatedDate DESC";

        var result = SqlInterrogator.ExtractTopNumber(sql);

        _ = result.Should().Be(100);
    }

    [Fact]
    public void ExtractTop_WhenTop1000()
    {
        var sql = "SELECT TOP 1000 * FROM LargeTable";

        var result = SqlInterrogator.ExtractTopNumber(sql);

        _ = result.Should().Be(1000);
    }

    [Fact]
    public void ExtractTop_WhenLargeNumber()
    {
        var sql = "SELECT TOP 999999 * FROM Users";

        var result = SqlInterrogator.ExtractTopNumber(sql);

        _ = result.Should().Be(999999);
    }

    [Fact]
    public void ExtractTop_WhenDistinctTop()
    {
        var sql = "SELECT DISTINCT TOP 50 Category FROM Products";

        var result = SqlInterrogator.ExtractTopNumber(sql);

        _ = result.Should().Be(50);
    }

    [Fact]
    public void ExtractTop_WhenDistinctTopWithWhere()
    {
        var sql = "SELECT DISTINCT TOP 25 Category FROM Products WHERE Active = 1";

        var result = SqlInterrogator.ExtractTopNumber(sql);

        _ = result.Should().Be(25);
    }

    [Fact]
    public void ExtractTop_WhenLowercaseKeywords()
    {
        var sql = "select top 5 name from users";

        var result = SqlInterrogator.ExtractTopNumber(sql);

        _ = result.Should().Be(5);
    }

    [Fact]
    public void ExtractTop_WhenMixedCaseKeywords()
    {
        var sql = "SeLeCt ToP 15 Name FrOm Users";

        var result = SqlInterrogator.ExtractTopNumber(sql);

        _ = result.Should().Be(15);
    }

    [Fact]
    public void ExtractTop_WhenDistinctLowercase()
    {
        var sql = "select distinct top 20 category from products";

        var result = SqlInterrogator.ExtractTopNumber(sql);

        _ = result.Should().Be(20);
    }

    [Fact]
    public void ExtractTop_WhenTopWithOrderBy()
    {
        var sql = "SELECT TOP 10 * FROM Users ORDER BY Name ASC";

        var result = SqlInterrogator.ExtractTopNumber(sql);

        _ = result.Should().Be(10);
    }

    [Fact]
    public void ExtractTop_WhenTopWithWhereAndOrderBy()
    {
        var sql = "SELECT TOP 5 Name, Email FROM Users WHERE Active = 1 ORDER BY CreatedDate DESC";

        var result = SqlInterrogator.ExtractTopNumber(sql);

        _ = result.Should().Be(5);
    }

    [Fact]
    public void ExtractTop_WhenTopWithJoin()
    {
        var sql = "SELECT TOP 10 u.Name, o.OrderDate FROM Users u INNER JOIN Orders o ON u.Id = o.UserId";

        var result = SqlInterrogator.ExtractTopNumber(sql);

        _ = result.Should().Be(10);
    }

    [Fact]
    public void ExtractTop_WhenTopWithGroupBy()
    {
        var sql = "SELECT TOP 20 Category, COUNT(*) AS Total FROM Products GROUP BY Category";

        var result = SqlInterrogator.ExtractTopNumber(sql);

        _ = result.Should().Be(20);
    }

    [Fact]
    public void ExtractTop_WhenTopWithPagination()
    {
        var sql = "SELECT TOP 50 * FROM Users ORDER BY CreatedDate DESC OFFSET 0 ROWS FETCH NEXT 10 ROWS ONLY";

        var result = SqlInterrogator.ExtractTopNumber(sql);

        _ = result.Should().Be(50);
    }

    [Fact]
    public void ExtractTop_WhenBracketedColumns()
    {
        var sql = "SELECT TOP 10 [Name], [Email] FROM Users";

        var result = SqlInterrogator.ExtractTopNumber(sql);

        _ = result.Should().Be(10);
    }

    [Fact]
    public void ExtractTop_WhenBracketedTableName()
    {
        var sql = "SELECT TOP 25 * FROM [MyDB].[dbo].[Users]";

        var result = SqlInterrogator.ExtractTopNumber(sql);

        _ = result.Should().Be(25);
    }

    [Fact]
    public void ExtractTop_WhenQualifiedColumns()
    {
        var sql = "SELECT TOP 15 u.Name, u.Email FROM Users u";

        var result = SqlInterrogator.ExtractTopNumber(sql);

        _ = result.Should().Be(15);
    }

    [Fact]
    public void ExtractTop_WhenColumnsWithAliases()
    {
        var sql = "SELECT TOP 30 Name AS FullName, Email AS EmailAddress FROM Users";

        var result = SqlInterrogator.ExtractTopNumber(sql);

        _ = result.Should().Be(30);
    }

    [Fact]
    public void ExtractTop_WhenMultilineQuery()
    {
        var sql = @"
            SELECT TOP 100 
                Name,
                Email,
                CreatedDate
            FROM 
                Users
            WHERE 
                Active = 1";

        var result = SqlInterrogator.ExtractTopNumber(sql);

        _ = result.Should().Be(100);
    }

    [Fact]
    public void ExtractTop_WhenSingleLineComments()
    {
        var sql = @"
            -- Get top users
            SELECT TOP 10 Name, Email 
            FROM Users -- Main users table
            WHERE Active = 1";

        var result = SqlInterrogator.ExtractTopNumber(sql);

        _ = result.Should().Be(10);
    }

    [Fact]
    public void ExtractTop_WhenMultiLineComments()
    {
        var sql = @"
            /* This query gets top active users
               Created: 2024-01-01
            */
            SELECT TOP 20 Name, Email 
            FROM Users 
            WHERE Active = 1";

        var result = SqlInterrogator.ExtractTopNumber(sql);

        _ = result.Should().Be(20);
    }

    [Fact]
    public void ExtractTop_WhenCTE()
    {
        var sql = @"
            WITH UserCTE AS (
                SELECT * FROM AllUsers WHERE Status = 1
            )
            SELECT TOP 15 Name, Email FROM Users";

        var result = SqlInterrogator.ExtractTopNumber(sql);

        _ = result.Should().Be(15);
    }

    [Fact]
    public void ExtractTop_WhenUseStatement()
    {
        var sql = @"
            USE MyDatabase;
            SELECT TOP 25 Name, Email FROM Users";

        var result = SqlInterrogator.ExtractTopNumber(sql);

        _ = result.Should().Be(25);
    }

    [Fact]
    public void ExtractTop_WhenTableWithNoLock()
    {
        var sql = "SELECT TOP 10 * FROM Users WITH (NOLOCK)";

        var result = SqlInterrogator.ExtractTopNumber(sql);

        _ = result.Should().Be(10);
    }

    [Fact]
    public void ExtractTop_WhenSubqueryInWhere()
    {
        var sql = "SELECT TOP 5 * FROM Users WHERE Id IN (SELECT UserId FROM Orders WHERE Status = 'Active')";

        var result = SqlInterrogator.ExtractTopNumber(sql);

        _ = result.Should().Be(5);
    }

    [Fact]
    public void ExtractTop_WhenDoubleQuotedIdentifiers()
    {
        var sql = "SELECT TOP 10 \"Name\", \"Email\" FROM \"Users\"";

        var result = SqlInterrogator.ExtractTopNumber(sql);

        _ = result.Should().Be(10);
    }

    [Fact]
    public void ExtractTop_RealWorld_TopSellers()
    {
        var sql = "SELECT TOP 10 ProductName, SalesTotal FROM Products ORDER BY SalesTotal DESC";

        var result = SqlInterrogator.ExtractTopNumber(sql);

        _ = result.Should().Be(10);
    }

    [Fact]
    public void ExtractTop_RealWorld_RecentOrders()
    {
        var sql = "SELECT TOP 20 * FROM Orders WHERE Status = 'Completed' ORDER BY OrderDate DESC";

        var result = SqlInterrogator.ExtractTopNumber(sql);

        _ = result.Should().Be(20);
    }

    [Fact]
    public void ExtractTop_RealWorld_DataSample()
    {
        var sql = "SELECT TOP 1000 * FROM LargeTable WHERE Category = @category";

        var result = SqlInterrogator.ExtractTopNumber(sql);

        _ = result.Should().Be(1000);
    }

    [Fact]
    public void ExtractTop_WhenExtraWhitespace()
    {
        var sql = "SELECT   TOP   10   Name   FROM   Users";

        var result = SqlInterrogator.ExtractTopNumber(sql);

        _ = result.Should().Be(10);
    }

    [Fact]
    public void ExtractTop_WhenDistinctWithExtraWhitespace()
    {
        var sql = "SELECT   DISTINCT   TOP   50   Category   FROM   Products";

        var result = SqlInterrogator.ExtractTopNumber(sql);

        _ = result.Should().Be(50);
    }

    [Fact]
    public void ExtractTop_Consistency_SameResultMultipleCalls()
    {
        var sql = "SELECT TOP 100 * FROM Users";

        var result1 = SqlInterrogator.ExtractTopNumber(sql);
        var result2 = SqlInterrogator.ExtractTopNumber(sql);

        _ = result1.Should().Be(100);
        _ = result2.Should().Be(100);
        _ = result1.Should().Be(result2);
    }

    [Fact]
    public void ExtractTop_WhenComplexQueryWithAllClauses()
    {
        var sql = @"
            SELECT DISTINCT TOP 50 u.Name, u.Email, COUNT(o.Id) AS OrderCount
            FROM Users u
            LEFT JOIN Orders o ON u.Id = o.UserId
            WHERE u.Active = 1 
              AND u.CreatedDate > '2024-01-01'
              AND o.Status IN ('Pending', 'Completed')
            GROUP BY u.Name, u.Email
            HAVING COUNT(o.Id) > 5
            ORDER BY u.Name ASC";

        var result = SqlInterrogator.ExtractTopNumber(sql);

        _ = result.Should().Be(50);
    }

    [Fact]
    public void ExtractTop_WhenCrossJoin()
    {
        var sql = "SELECT TOP 100 u.Name, c.CategoryName FROM Users u CROSS JOIN Categories c";

        var result = SqlInterrogator.ExtractTopNumber(sql);

        _ = result.Should().Be(100);
    }

    [Fact]
    public void ExtractTop_WhenExistsClause()
    {
        var sql = "SELECT TOP 5 * FROM Users u WHERE EXISTS (SELECT 1 FROM Orders o WHERE o.UserId = u.Id)";

        var result = SqlInterrogator.ExtractTopNumber(sql);

        _ = result.Should().Be(5);
    }

    [Fact]
    public void ExtractTop_WhenBetweenOperator()
    {
        var sql = "SELECT TOP 10 * FROM Users WHERE Age BETWEEN 18 AND 65";

        var result = SqlInterrogator.ExtractTopNumber(sql);

        _ = result.Should().Be(10);
    }

    [Fact]
    public void ExtractTop_WithParameters()
    {
        var sql = "SELECT TOP 10 * FROM Users WHERE Status = @status ORDER BY CreatedDate DESC";

        var result = SqlInterrogator.ExtractTopNumber(sql);

        _ = result.Should().Be(10);
    }

    [Fact]
    public void ExtractTop_WhenUnion()
    {
        var sql = "SELECT TOP 10 Name FROM Users WHERE Active = 1 UNION SELECT Name FROM ArchivedUsers";

        var result = SqlInterrogator.ExtractTopNumber(sql);

        _ = result.Should().Be(10);
    }

    [Fact]
    public void ExtractTop_WhenCountFunction()
    {
        var sql = "SELECT TOP 5 COUNT(*) AS Total, Department FROM Employees GROUP BY Department";

        var result = SqlInterrogator.ExtractTopNumber(sql);

        _ = result.Should().Be(5);
    }

    [Fact]
    public void ExtractTop_WhenCaseExpression()
    {
        var sql = "SELECT TOP 10 CASE WHEN Active = 1 THEN 'Yes' ELSE 'No' END AS IsActive, Name FROM Users";

        var result = SqlInterrogator.ExtractTopNumber(sql);

        _ = result.Should().Be(10);
    }

    [Fact]
    public void ExtractTop_DifferentTopValues()
    {
        var sql1 = "SELECT TOP 1 * FROM Users";
        var sql5 = "SELECT TOP 5 * FROM Users";
        var sql10 = "SELECT TOP 10 * FROM Users";
        var sql100 = "SELECT TOP 100 * FROM Users";
        var sql1000 = "SELECT TOP 1000 * FROM Users";

        _ = SqlInterrogator.ExtractTopNumber(sql1).Should().Be(1);
        _ = SqlInterrogator.ExtractTopNumber(sql5).Should().Be(5);
        _ = SqlInterrogator.ExtractTopNumber(sql10).Should().Be(10);
        _ = SqlInterrogator.ExtractTopNumber(sql100).Should().Be(100);
        _ = SqlInterrogator.ExtractTopNumber(sql1000).Should().Be(1000);
    }

    [Fact]
    public void ExtractTop_WhenMultipleJoins()
    {
        var sql = @"SELECT TOP 25 u.Name, o.OrderDate, p.ProductName 
            FROM Users u 
            INNER JOIN Orders o ON u.Id = o.UserId
            LEFT JOIN Products p ON o.ProductId = p.Id";

        var result = SqlInterrogator.ExtractTopNumber(sql);

        _ = result.Should().Be(25);
    }

    [Fact]
    public void ExtractTop_WhenWindowFunction()
    {
        var sql = "SELECT TOP 10 Name, ROW_NUMBER() OVER (ORDER BY CreatedDate) AS RowNum FROM Users";

        var result = SqlInterrogator.ExtractTopNumber(sql);

        _ = result.Should().Be(10);
    }

    [Fact]
    public void ExtractTop_WhenTopWithParentheses()
    {
        var sql = "SELECT TOP(10) * FROM Users";

        var result = SqlInterrogator.ExtractTopNumber(sql);

        _ = result.Should().Be(10);
    }

    [Fact]
    public void ExtractTop_WhenTopWithParenthesesAndSpace()
    {
        var sql = "SELECT TOP (25) Name, Email FROM Users";

        var result = SqlInterrogator.ExtractTopNumber(sql);

        _ = result.Should().Be(25);
    }

    [Fact]
    public void ExtractTop_WhenTopWithParenthesesLargeNumber()
    {
        var sql = "SELECT TOP(1000) * FROM LargeTable WHERE Category = @category";

        var result = SqlInterrogator.ExtractTopNumber(sql);

        _ = result.Should().Be(1000);
    }

    [Fact]
    public void ExtractTop_WhenDistinctTopWithParentheses()
    {
        var sql = "SELECT DISTINCT TOP(50) Category FROM Products";

        var result = SqlInterrogator.ExtractTopNumber(sql);

        _ = result.Should().Be(50);
    }

    [Fact]
    public void ExtractTop_WhenDistinctTopWithParenthesesAndSpace()
    {
        var sql = "SELECT DISTINCT TOP (100) Category FROM Products WHERE Active = 1";

        var result = SqlInterrogator.ExtractTopNumber(sql);

        _ = result.Should().Be(100);
    }

    [Fact]
    public void ExtractTop_WhenTopWithParenthesesAndWhere()
    {
        var sql = "SELECT TOP(5) Name, Email FROM Users WHERE Active = 1";

        var result = SqlInterrogator.ExtractTopNumber(sql);

        _ = result.Should().Be(5);
    }

    [Fact]
    public void ExtractTop_WhenTopWithParenthesesAndOrderBy()
    {
        var sql = "SELECT TOP(20) * FROM Orders ORDER BY OrderDate DESC";

        var result = SqlInterrogator.ExtractTopNumber(sql);

        _ = result.Should().Be(20);
    }

    [Fact]
    public void ExtractTop_WhenTopWithParenthesesLowercase()
    {
        var sql = "select top(15) name from users";

        var result = SqlInterrogator.ExtractTopNumber(sql);

        _ = result.Should().Be(15);
    }

    [Fact]
    public void ExtractTop_WhenTopWithParenthesesMixedCase()
    {
        var sql = "SeLeCt ToP(30) Name FrOm Users";

        var result = SqlInterrogator.ExtractTopNumber(sql);

        _ = result.Should().Be(30);
    }

    [Fact]
    public void ExtractTop_WhenTopWithParenthesesAndJoin()
    {
        var sql = "SELECT TOP (10) u.Name, o.OrderDate FROM Users u INNER JOIN Orders o ON u.Id = o.UserId";

        var result = SqlInterrogator.ExtractTopNumber(sql);

        _ = result.Should().Be(10);
    }

    [Fact]
    public void ExtractTop_WhenTopWithParenthesesAndGroupBy()
    {
        var sql = "SELECT TOP(20) Category, COUNT(*) AS Total FROM Products GROUP BY Category";

        var result = SqlInterrogator.ExtractTopNumber(sql);

        _ = result.Should().Be(20);
    }

    [Fact]
    public void ExtractTop_WhenTopWithParenthesesExtraSpaces()
    {
        var sql = "SELECT TOP (  50  ) * FROM Users";

        var result = SqlInterrogator.ExtractTopNumber(sql);

        _ = result.Should().Be(50);
    }

    [Fact]
    public void ExtractTop_AllFormats_Comparison()
    {
        var sql1 = "SELECT TOP 10 * FROM Users";
        var sql2 = "SELECT TOP(10) * FROM Users";
        var sql3 = "SELECT TOP (10) * FROM Users";

        var result1 = SqlInterrogator.ExtractTopNumber(sql1);
        var result2 = SqlInterrogator.ExtractTopNumber(sql2);
        var result3 = SqlInterrogator.ExtractTopNumber(sql3);

        _ = result1.Should().Be(10);
        _ = result2.Should().Be(10);
        _ = result3.Should().Be(10);
        _ = result1.Should().Be(result2);
        _ = result2.Should().Be(result3);
    }
}
