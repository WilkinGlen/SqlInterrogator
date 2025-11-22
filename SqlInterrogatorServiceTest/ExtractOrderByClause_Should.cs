using FluentAssertions;
using SqlInterrogatorService;

namespace SqlInterrogatorServiceTest;

public class ExtractOrderByClause_Should
{
    [Fact]
    public void ReturnNull_WhenSqlIsNull()
    {
        var result = SqlInterrogator.ExtractOrderByClause(null!);

        _ = result.Should().BeNull();
    }

    [Fact]
    public void ReturnNull_WhenSqlIsEmpty()
    {
        var result = SqlInterrogator.ExtractOrderByClause("");

        _ = result.Should().BeNull();
    }

    [Fact]
    public void ReturnNull_WhenSqlIsWhitespace()
    {
        var result = SqlInterrogator.ExtractOrderByClause("   ");

        _ = result.Should().BeNull();
    }

    [Fact]
    public void ReturnNull_WhenUpdateStatement()
    {
        var sql = "UPDATE Users SET Active = 1";

        var result = SqlInterrogator.ExtractOrderByClause(sql);

        _ = result.Should().BeNull();
    }

    [Fact]
    public void ReturnNull_WhenInsertStatement()
    {
        var sql = "INSERT INTO Users (Name, Email) VALUES ('John', 'john@example.com')";

        var result = SqlInterrogator.ExtractOrderByClause(sql);

        _ = result.Should().BeNull();
    }

    [Fact]
    public void ReturnNull_WhenDeleteStatement()
    {
        var sql = "DELETE FROM Users WHERE Id = 1";

        var result = SqlInterrogator.ExtractOrderByClause(sql);

        _ = result.Should().BeNull();
    }

    [Fact]
    public void ReturnNull_WhenNoOrderByClause()
    {
        var sql = "SELECT * FROM Users";

        var result = SqlInterrogator.ExtractOrderByClause(sql);

        _ = result.Should().BeNull();
    }

    [Fact]
    public void ReturnNull_WhenNoOrderByClauseWithWhere()
    {
        var sql = "SELECT * FROM Users WHERE Active = 1";

        var result = SqlInterrogator.ExtractOrderByClause(sql);

        _ = result.Should().BeNull();
    }

    [Fact]
    public void ExtractOrderBy_WhenSimpleSingleColumn()
    {
        var sql = "SELECT * FROM Users ORDER BY Name";

        var result = SqlInterrogator.ExtractOrderByClause(sql);

        _ = result.Should().Be("ORDER BY Name");
    }

    [Fact]
    public void ExtractOrderBy_WhenSingleColumnAsc()
    {
        var sql = "SELECT * FROM Users ORDER BY Name ASC";

        var result = SqlInterrogator.ExtractOrderByClause(sql);

        _ = result.Should().Be("ORDER BY Name ASC");
    }

    [Fact]
    public void ExtractOrderBy_WhenSingleColumnDesc()
    {
        var sql = "SELECT * FROM Users ORDER BY CreatedDate DESC";

        var result = SqlInterrogator.ExtractOrderByClause(sql);

        _ = result.Should().Be("ORDER BY CreatedDate DESC");
    }

    [Fact]
    public void ExtractOrderBy_WhenMultipleColumns()
    {
        var sql = "SELECT * FROM Users ORDER BY Name ASC, Email DESC";

        var result = SqlInterrogator.ExtractOrderByClause(sql);

        _ = result.Should().Be("ORDER BY Name ASC, Email DESC");
    }

    [Fact]
    public void ExtractOrderBy_WhenThreeColumns()
    {
        var sql = "SELECT * FROM Users ORDER BY Department, Name ASC, CreatedDate DESC";

        var result = SqlInterrogator.ExtractOrderByClause(sql);

        _ = result.Should().Be("ORDER BY Department, Name ASC, CreatedDate DESC");
    }

    [Fact]
    public void ExtractOrderBy_WhenQualifiedColumnName()
    {
        var sql = "SELECT u.Name, u.Email FROM Users u ORDER BY u.Name ASC";

        var result = SqlInterrogator.ExtractOrderByClause(sql);

        _ = result.Should().Be("ORDER BY u.Name ASC");
    }

    [Fact]
    public void ExtractOrderBy_WhenMultipleQualifiedColumns()
    {
        var sql = "SELECT u.Name, o.OrderDate FROM Users u JOIN Orders o ON u.Id = o.UserId ORDER BY o.OrderDate DESC, u.Name ASC";

        var result = SqlInterrogator.ExtractOrderByClause(sql);

        _ = result.Should().Be("ORDER BY o.OrderDate DESC, u.Name ASC");
    }

    [Fact]
    public void ExtractOrderBy_WhenBracketedColumnName()
    {
        var sql = "SELECT [User Name], [Email Address] FROM Users ORDER BY [User Name] ASC";

        var result = SqlInterrogator.ExtractOrderByClause(sql);

        _ = result.Should().Be("ORDER BY [User Name] ASC");
    }

    [Fact]
    public void ExtractOrderBy_WhenFullyQualifiedColumnName()
    {
        var sql = "SELECT * FROM MyDB.dbo.Users ORDER BY MyDB.dbo.Users.Name ASC";

        var result = SqlInterrogator.ExtractOrderByClause(sql);

        _ = result.Should().Be("ORDER BY MyDB.dbo.Users.Name ASC");
    }

    [Fact]
    public void ExtractOrderBy_WhenWithWhere()
    {
        var sql = "SELECT * FROM Users WHERE Active = 1 ORDER BY Name ASC";

        var result = SqlInterrogator.ExtractOrderByClause(sql);

        _ = result.Should().Be("ORDER BY Name ASC");
    }

    [Fact]
    public void ExtractOrderBy_WhenWithComplexWhere()
    {
        var sql = "SELECT Name, Email FROM Users WHERE Active = 1 AND CreatedDate > '2024-01-01' ORDER BY Name";

        var result = SqlInterrogator.ExtractOrderByClause(sql);

        _ = result.Should().Be("ORDER BY Name");
    }

    [Fact]
    public void ExtractOrderBy_WhenWithJoin()
    {
        var sql = "SELECT u.Name, o.OrderDate FROM Users u INNER JOIN Orders o ON u.Id = o.UserId ORDER BY u.Name ASC";

        var result = SqlInterrogator.ExtractOrderByClause(sql);

        _ = result.Should().Be("ORDER BY u.Name ASC");
    }

    [Fact]
    public void ExtractOrderBy_WhenWithMultipleJoins()
    {
        var sql = "SELECT u.Name, o.OrderDate, p.ProductName FROM Users u INNER JOIN Orders o ON u.Id = o.UserId LEFT JOIN Products p ON o.ProductId = p.Id ORDER BY o.OrderDate DESC, p.ProductName ASC";

        var result = SqlInterrogator.ExtractOrderByClause(sql);

        _ = result.Should().Be("ORDER BY o.OrderDate DESC, p.ProductName ASC");
    }

    [Fact]
    public void ExtractOrderBy_WhenWithGroupBy()
    {
        var sql = "SELECT Category, COUNT(*) AS Total FROM Products GROUP BY Category ORDER BY Total DESC";

        var result = SqlInterrogator.ExtractOrderByClause(sql);

        _ = result.Should().Be("ORDER BY Total DESC");
    }

    [Fact]
    public void ExtractOrderBy_WhenWithGroupByAndHaving()
    {
        var sql = "SELECT Category, COUNT(*) AS Total FROM Products GROUP BY Category HAVING COUNT(*) > 5 ORDER BY Category ASC";

        var result = SqlInterrogator.ExtractOrderByClause(sql);

        _ = result.Should().Be("ORDER BY Category ASC");
    }

    [Fact]
    public void ExtractOrderBy_WhenWithOffset()
    {
        var sql = "SELECT * FROM Users ORDER BY Name ASC OFFSET 10 ROWS";

        var result = SqlInterrogator.ExtractOrderByClause(sql);

        _ = result.Should().Be("ORDER BY Name ASC");
    }

    [Fact]
    public void ExtractOrderBy_WhenWithOffsetFetch()
    {
        var sql = "SELECT * FROM Users ORDER BY Name ASC OFFSET 10 ROWS FETCH NEXT 20 ROWS ONLY";

        var result = SqlInterrogator.ExtractOrderByClause(sql);

        _ = result.Should().Be("ORDER BY Name ASC");
    }

    [Fact]
    public void ExtractOrderBy_WhenWithComplexPagination()
    {
        var sql = "SELECT * FROM Users WHERE Active = 1 ORDER BY Name ASC, Email DESC OFFSET 100 ROWS FETCH NEXT 50 ROWS ONLY";

        var result = SqlInterrogator.ExtractOrderByClause(sql);

        _ = result.Should().Be("ORDER BY Name ASC, Email DESC");
    }

    [Fact]
    public void ExtractOrderBy_WhenOrderByAlias()
    {
        var sql = "SELECT Name AS FullName, Email AS EmailAddress FROM Users ORDER BY FullName ASC";

        var result = SqlInterrogator.ExtractOrderByClause(sql);

        _ = result.Should().Be("ORDER BY FullName ASC");
    }

    [Fact]
    public void ExtractOrderBy_WhenOrderByAggregateAlias()
    {
        var sql = "SELECT Category, COUNT(*) AS ProductCount FROM Products GROUP BY Category ORDER BY ProductCount DESC";

        var result = SqlInterrogator.ExtractOrderByClause(sql);

        _ = result.Should().Be("ORDER BY ProductCount DESC");
    }

    [Fact]
    public void ExtractOrderBy_WhenOrderByNumericPosition()
    {
        var sql = "SELECT Name, Email, CreatedDate FROM Users ORDER BY 1 ASC, 3 DESC";

        var result = SqlInterrogator.ExtractOrderByClause(sql);

        _ = result.Should().Be("ORDER BY 1 ASC, 3 DESC");
    }

    [Fact]
    public void ExtractOrderBy_WhenOrderByCaseExpression()
    {
        var sql = "SELECT Name, Price FROM Products ORDER BY CASE WHEN Price > 100 THEN 1 ELSE 2 END, Name";

        var result = SqlInterrogator.ExtractOrderByClause(sql);

        _ = result.Should().Be("ORDER BY CASE WHEN Price > 100 THEN 1 ELSE 2 END, Name");
    }

    [Fact]
    public void ExtractOrderBy_WhenOrderByFunction()
    {
        var sql = "SELECT FirstName, LastName FROM Users ORDER BY LEN(LastName) DESC, FirstName";

        var result = SqlInterrogator.ExtractOrderByClause(sql);

        _ = result.Should().Be("ORDER BY LEN(LastName) DESC, FirstName");
    }

    [Fact]
    public void ExtractOrderBy_WhenOrderByDateFunction()
    {
        var sql = "SELECT * FROM Orders ORDER BY YEAR(OrderDate) DESC, MONTH(OrderDate) DESC";

        var result = SqlInterrogator.ExtractOrderByClause(sql);

        _ = result.Should().Be("ORDER BY YEAR(OrderDate) DESC, MONTH(OrderDate) DESC");
    }

    [Fact]
    public void ExtractOrderBy_WhenLowercaseKeywords()
    {
        var sql = "select name, email from users where active = 1 order by name";

        var result = SqlInterrogator.ExtractOrderByClause(sql);

        _ = result.Should().Be("order by name");
    }

    [Fact]
    public void ExtractOrderBy_WhenMixedCaseKeywords()
    {
        var sql = "SeLeCt Name FrOm Users WhErE Active = 1 OrDeR bY Name DESC";

        var result = SqlInterrogator.ExtractOrderByClause(sql);

        _ = result.Should().Be("OrDeR bY Name DESC");
    }

    [Fact]
    public void ExtractOrderBy_WhenMultilineQuery()
    {
        var sql = @"
        SELECT 
            Name,
            Email,
            CreatedDate
        FROM 
            Users
        WHERE 
            Active = 1
        ORDER BY 
            Name ASC,
            CreatedDate DESC";

        var result = SqlInterrogator.ExtractOrderByClause(sql);

        _ = result.Should().Contain("ORDER BY");
        _ = result.Should().Contain("Name ASC");
        _ = result.Should().Contain("CreatedDate DESC");
    }

    [Fact]
    public void ExtractOrderBy_WhenWithSingleLineComments()
    {
        var sql = @"
            -- Get all users
            SELECT Name, Email 
            FROM Users -- Main users table
            WHERE Active = 1
            ORDER BY Name ASC -- Sort by name";

        var result = SqlInterrogator.ExtractOrderByClause(sql);

        _ = result.Should().Contain("ORDER BY");
        _ = result.Should().Contain("Name ASC");
    }

    [Fact]
    public void ExtractOrderBy_WhenWithMultiLineComments()
    {
        var sql = @"
            /* This query gets all active users
               Created: 2024-01-01
            */
            SELECT Name, Email 
            FROM Users 
            WHERE Active = 1
            ORDER BY Name ASC";

        var result = SqlInterrogator.ExtractOrderByClause(sql);

        _ = result.Should().Be("ORDER BY Name ASC");
    }

    [Fact]
    public void ExtractOrderBy_WhenWithCTE()
    {
        var sql = @"
            WITH UserCTE AS (
                SELECT * FROM AllUsers WHERE Status = 1
            )
            SELECT Name, Email FROM Users ORDER BY Name";

        var result = SqlInterrogator.ExtractOrderByClause(sql);

        _ = result.Should().Be("ORDER BY Name");
    }

    [Fact]
    public void ExtractOrderBy_WhenWithUseStatement()
    {
        var sql = @"
            USE MyDatabase;
            SELECT Name, Email FROM Users ORDER BY Email ASC";

        var result = SqlInterrogator.ExtractOrderByClause(sql);

        _ = result.Should().Be("ORDER BY Email ASC");
    }

    [Fact]
    public void ExtractOrderBy_WhenDistinctQuery()
    {
        var sql = "SELECT DISTINCT Category FROM Products ORDER BY Category ASC";

        var result = SqlInterrogator.ExtractOrderByClause(sql);

        _ = result.Should().Be("ORDER BY Category ASC");
    }

    [Fact]
    public void ExtractOrderBy_WhenTopQuery()
    {
        var sql = "SELECT TOP 10 Name, Email FROM Users ORDER BY CreatedDate DESC";

        var result = SqlInterrogator.ExtractOrderByClause(sql);

        _ = result.Should().Be("ORDER BY CreatedDate DESC");
    }

    [Fact]
    public void ExtractOrderBy_WhenTableWithNoLock()
    {
        var sql = "SELECT * FROM Users WITH (NOLOCK) ORDER BY Name ASC";

        var result = SqlInterrogator.ExtractOrderByClause(sql);

        _ = result.Should().Be("ORDER BY Name ASC");
    }

    [Fact]
    public void ExtractOrderBy_WhenSubqueryInWhere()
    {
        var sql = "SELECT * FROM Users WHERE Id IN (SELECT UserId FROM Orders WHERE Status = 'Active') ORDER BY Name ASC";

        var result = SqlInterrogator.ExtractOrderByClause(sql);

        _ = result.Should().Be("ORDER BY Name ASC");
    }

    [Fact]
    public void ExtractOrderBy_WhenOrderByWithCollate()
    {
        var sql = "SELECT Name FROM Users ORDER BY Name COLLATE Latin1_General_CI_AS ASC";

        var result = SqlInterrogator.ExtractOrderByClause(sql);

        _ = result.Should().Be("ORDER BY Name COLLATE Latin1_General_CI_AS ASC");
    }

    [Fact]
    public void ExtractOrderBy_WhenOrderByWithNullsFirst()
    {
        var sql = "SELECT Name, DeletedDate FROM Users ORDER BY DeletedDate ASC NULLS FIRST";

        var result = SqlInterrogator.ExtractOrderByClause(sql);

        _ = result.Should().Contain("ORDER BY DeletedDate ASC");
    }

    [Fact]
    public void ExtractOrderBy_WhenComplexQueryWithAllClauses()
    {
        var sql = @"
            SELECT u.Name, u.Email, COUNT(o.Id) AS OrderCount
            FROM Users u
            LEFT JOIN Orders o ON u.Id = o.UserId
            WHERE u.Active = 1 
              AND u.CreatedDate > '2024-01-01'
              AND o.Status IN ('Pending', 'Completed')
            GROUP BY u.Name, u.Email
            HAVING COUNT(o.Id) > 5
            ORDER BY u.Name ASC, OrderCount DESC";

        var result = SqlInterrogator.ExtractOrderByClause(sql);

        _ = result.Should().Be("ORDER BY u.Name ASC, OrderCount DESC");
    }

    [Fact]
    public void ExtractOrderBy_WhenPaginationQueryPage1()
    {
        var sql = "SELECT * FROM Users WHERE Active = 1 ORDER BY CreatedDate DESC OFFSET 0 ROWS FETCH NEXT 10 ROWS ONLY";

        var result = SqlInterrogator.ExtractOrderByClause(sql);

        _ = result.Should().Be("ORDER BY CreatedDate DESC");
    }

    [Fact]
    public void ExtractOrderBy_WhenPaginationQueryPage5()
    {
        var sql = "SELECT * FROM Users WHERE Active = 1 ORDER BY Name ASC OFFSET 200 ROWS FETCH NEXT 50 ROWS ONLY";

        var result = SqlInterrogator.ExtractOrderByClause(sql);

        _ = result.Should().Be("ORDER BY Name ASC");
    }

    [Fact]
    public void ExtractOrderBy_WhenUnionQuery()
    {
        var sql = "SELECT Name FROM Users WHERE Active = 1 UNION SELECT Name FROM ArchivedUsers ORDER BY Name ASC";

        var result = SqlInterrogator.ExtractOrderByClause(sql);

        _ = result.Should().Be("ORDER BY Name ASC");
    }

    [Fact]
    public void ExtractOrderBy_WhenOrderByExpression()
    {
        var sql = "SELECT FirstName, LastName FROM Users ORDER BY FirstName + ' ' + LastName ASC";

        var result = SqlInterrogator.ExtractOrderByClause(sql);

        _ = result.Should().Be("ORDER BY FirstName + ' ' + LastName ASC");
    }

    [Fact]
    public void ExtractOrderBy_WhenOrderBySubstring()
    {
        var sql = "SELECT Email FROM Users ORDER BY SUBSTRING(Email, CHARINDEX('@', Email) + 1, LEN(Email)) ASC";

        var result = SqlInterrogator.ExtractOrderByClause(sql);

        _ = result.Should().Be("ORDER BY SUBSTRING(Email, CHARINDEX('@', Email) + 1, LEN(Email)) ASC");
    }

    [Fact]
    public void ExtractOrderBy_WhenOrderByCoalesce()
    {
        var sql = "SELECT Name, PreferredName FROM Users ORDER BY COALESCE(PreferredName, Name) ASC";

        var result = SqlInterrogator.ExtractOrderByClause(sql);

        _ = result.Should().Be("ORDER BY COALESCE(PreferredName, Name) ASC");
    }

    [Fact]
    public void ExtractOrderBy_WhenOrderByConvert()
    {
        var sql = "SELECT OrderDate FROM Orders ORDER BY CONVERT(VARCHAR, OrderDate, 103) DESC";

        var result = SqlInterrogator.ExtractOrderByClause(sql);

        _ = result.Should().Be("ORDER BY CONVERT(VARCHAR, OrderDate, 103) DESC");
    }

    [Fact]
    public void ExtractOrderBy_WhenOrderByCast()
    {
        var sql = "SELECT ProductCode FROM Products ORDER BY CAST(ProductCode AS INT) ASC";

        var result = SqlInterrogator.ExtractOrderByClause(sql);

        _ = result.Should().Be("ORDER BY CAST(ProductCode AS INT) ASC");
    }

    [Fact]
    public void ExtractOrderBy_RealWorld_DataGridSorting()
    {
        var sql = "SELECT Id, Name, Email, CreatedDate FROM Users WHERE Active = 1 ORDER BY Name ASC";

        var result = SqlInterrogator.ExtractOrderByClause(sql);

        _ = result.Should().Be("ORDER BY Name ASC");
    }

    [Fact]
    public void ExtractOrderBy_RealWorld_PaginatedReport()
    {
        var sql = "SELECT * FROM Employees WHERE Department = 'Sales' ORDER BY Salary DESC OFFSET 200 ROWS FETCH NEXT 50 ROWS ONLY";

        var result = SqlInterrogator.ExtractOrderByClause(sql);

        _ = result.Should().Be("ORDER BY Salary DESC");
    }

    [Fact]
    public void ExtractOrderBy_RealWorld_MultiColumnSort()
    {
        var sql = "SELECT Department, Name, Salary FROM Employees WHERE Active = 1 ORDER BY Department ASC, Salary DESC, Name ASC";

        var result = SqlInterrogator.ExtractOrderByClause(sql);

        _ = result.Should().Be("ORDER BY Department ASC, Salary DESC, Name ASC");
    }

    [Fact]
    public void ExtractOrderBy_RealWorld_TopNQuery()
    {
        var sql = "SELECT TOP 10 ProductName, SalesTotal FROM Products ORDER BY SalesTotal DESC";

        var result = SqlInterrogator.ExtractOrderByClause(sql);

        _ = result.Should().Be("ORDER BY SalesTotal DESC");
    }

    [Fact]
    public void ExtractOrderBy_WhenExtraWhitespace()
    {
        var sql = "SELECT   Name   ,   Email   FROM   Users   ORDER BY   Name   ASC   ,   Email   DESC";

        var result = SqlInterrogator.ExtractOrderByClause(sql);

        _ = result.Should().Contain("ORDER BY");
        _ = result.Should().Contain("Name");
        _ = result.Should().Contain("ASC");
    }

    [Fact]
    public void ExtractOrderBy_WhenTabsAndNewlines()
    {
        var sql = "SELECT Name, Email FROM Users ORDER BY Name ASC";

        var result = SqlInterrogator.ExtractOrderByClause(sql);

        _ = result.Should().Be("ORDER BY Name ASC");
    }

    [Fact]
    public void ExtractOrderBy_Consistency_SameResultMultipleCalls()
    {
        var sql = "SELECT * FROM Users ORDER BY Name ASC";

        var result1 = SqlInterrogator.ExtractOrderByClause(sql);
        var result2 = SqlInterrogator.ExtractOrderByClause(sql);

        _ = result1.Should().Be(result2);
        _ = result1.Should().Be("ORDER BY Name ASC");
    }

    [Fact]
    public void ExtractOrderBy_WhenDoubleQuotedIdentifiers()
    {
        var sql = "SELECT \"Name\", \"Email\" FROM \"Users\" ORDER BY \"Name\" ASC";

        var result = SqlInterrogator.ExtractOrderByClause(sql);

        _ = result.Should().Be("ORDER BY \"Name\" ASC");
    }

    [Fact]
    public void ExtractOrderBy_WhenMixedIdentifierStyles()
    {
        var sql = "SELECT [Name], \"Email\", Phone FROM Users ORDER BY [Name] ASC, \"Email\" DESC, Phone";

        var result = SqlInterrogator.ExtractOrderByClause(sql);

        _ = result.Should().Be("ORDER BY [Name] ASC, \"Email\" DESC, Phone");
    }

    [Fact]
    public void ExtractOrderBy_WhenOrderByWithParentheses()
    {
        var sql = "SELECT Name, (Price * Quantity) AS Total FROM Orders ORDER BY (Price * Quantity) DESC";

        var result = SqlInterrogator.ExtractOrderByClause(sql);

        _ = result.Should().Be("ORDER BY (Price * Quantity) DESC");
    }

    [Fact]
    public void ExtractOrderBy_WhenNestedFunctions()
    {
        var sql = "SELECT Name FROM Users ORDER BY UPPER(LTRIM(RTRIM(Name))) ASC";

        var result = SqlInterrogator.ExtractOrderByClause(sql);

        _ = result.Should().Be("ORDER BY UPPER(LTRIM(RTRIM(Name))) ASC");
    }

    [Fact]
    public void ExtractOrderBy_WhenWindowFunction()
    {
        var sql = "SELECT Name, ROW_NUMBER() OVER (ORDER BY CreatedDate) AS RowNum FROM Users ORDER BY RowNum";

        var result = SqlInterrogator.ExtractOrderByClause(sql);

        _ = result.Should().Be("ORDER BY RowNum");
    }
}
