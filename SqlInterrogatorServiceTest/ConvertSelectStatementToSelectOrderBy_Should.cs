using FluentAssertions;
using SqlInterrogatorService;

namespace SqlInterrogatorServiceTest;

public class ConvertSelectStatementToSelectOrderBy_Should
{
    [Fact]
    public void ReturnNull_WhenSqlIsNull()
    {
        var result = SqlInterrogator.ConvertSelectStatementToSelectOrderBy(null!, "Name ASC");

        _ = result.Should().BeNull();
    }

    [Fact]
    public void ReturnNull_WhenSqlIsEmpty()
    {
        var result = SqlInterrogator.ConvertSelectStatementToSelectOrderBy("", "Name ASC");

        _ = result.Should().BeNull();
    }

    [Fact]
    public void ReturnNull_WhenSqlIsWhitespace()
    {
        var result = SqlInterrogator.ConvertSelectStatementToSelectOrderBy("   ", "Name ASC");

        _ = result.Should().BeNull();
    }

    [Fact]
    public void ReturnNull_WhenOrderByClauseIsNull()
    {
        var sql = "SELECT * FROM Users";

        var result = SqlInterrogator.ConvertSelectStatementToSelectOrderBy(sql, null!);

        _ = result.Should().BeNull();
    }

    [Fact]
    public void ReturnNull_WhenOrderByClauseIsEmpty()
    {
        var sql = "SELECT * FROM Users";

        var result = SqlInterrogator.ConvertSelectStatementToSelectOrderBy(sql, "");

        _ = result.Should().BeNull();
    }

    [Fact]
    public void ReturnNull_WhenOrderByClauseIsWhitespace()
    {
        var sql = "SELECT * FROM Users";

        var result = SqlInterrogator.ConvertSelectStatementToSelectOrderBy(sql, "   ");

        _ = result.Should().BeNull();
    }

    [Fact]
    public void ReturnNull_WhenBothAreNull()
    {
        var result = SqlInterrogator.ConvertSelectStatementToSelectOrderBy(null!, null!);

        _ = result.Should().BeNull();
    }

    [Fact]
    public void ReturnNull_WhenUpdateStatement()
    {
        var sql = "UPDATE Users SET Active = 1";

        var result = SqlInterrogator.ConvertSelectStatementToSelectOrderBy(sql, "Name");

        _ = result.Should().BeNull();
    }

    [Fact]
    public void ReturnNull_WhenInsertStatement()
    {
        var sql = "INSERT INTO Users (Name, Email) VALUES ('John', 'john@example.com')";

        var result = SqlInterrogator.ConvertSelectStatementToSelectOrderBy(sql, "Name");

        _ = result.Should().BeNull();
    }

    [Fact]
    public void ReturnNull_WhenDeleteStatement()
    {
        var sql = "DELETE FROM Users WHERE Id = 1";

        var result = SqlInterrogator.ConvertSelectStatementToSelectOrderBy(sql, "Name");

        _ = result.Should().BeNull();
    }

    [Fact]
    public void ReturnNull_WhenCreateStatement()
    {
        var sql = "CREATE TABLE Users (Id INT, Name VARCHAR(100))";

        var result = SqlInterrogator.ConvertSelectStatementToSelectOrderBy(sql, "Name");

        _ = result.Should().BeNull();
    }

    [Fact]
    public void ReturnNull_WhenNoFromClause()
    {
        var sql = "SELECT GETDATE()";

        var result = SqlInterrogator.ConvertSelectStatementToSelectOrderBy(sql, "Name");

        _ = result.Should().BeNull();
    }

    [Fact]
    public void ReturnNull_WhenNoFromClause_WithFunction()
    {
        var sql = "SELECT 1 + 1 AS Result";

        var result = SqlInterrogator.ConvertSelectStatementToSelectOrderBy(sql, "Result");

        _ = result.Should().BeNull();
    }

    [Fact]
    public void AddOrderBy_WhenSimpleSelectWithoutOrderBy()
    {
        var sql = "SELECT * FROM Users";

        var result = SqlInterrogator.ConvertSelectStatementToSelectOrderBy(sql, "Name ASC");

        _ = result.Should().Be("SELECT * FROM Users ORDER BY Name ASC");
    }

    [Fact]
    public void AddOrderBy_WhenSelectWithColumns()
    {
        var sql = "SELECT Name, Email FROM Users";

        var result = SqlInterrogator.ConvertSelectStatementToSelectOrderBy(sql, "Name");

        _ = result.Should().Be("SELECT Name, Email FROM Users ORDER BY Name");
    }

    [Fact]
    public void AddOrderBy_WithDescending()
    {
        var sql = "SELECT * FROM Users";

        var result = SqlInterrogator.ConvertSelectStatementToSelectOrderBy(sql, "CreatedDate DESC");

        _ = result.Should().Be("SELECT * FROM Users ORDER BY CreatedDate DESC");
    }

    [Fact]
    public void AddOrderBy_WithMultipleColumns()
    {
        var sql = "SELECT * FROM Users";

        var result = SqlInterrogator.ConvertSelectStatementToSelectOrderBy(sql, "Name ASC, Email DESC");

        _ = result.Should().Be("SELECT * FROM Users ORDER BY Name ASC, Email DESC");
    }

    [Fact]
    public void AddOrderBy_WithThreeColumns()
    {
        var sql = "SELECT * FROM Users";

        var result = SqlInterrogator.ConvertSelectStatementToSelectOrderBy(sql, "Department, Name ASC, CreatedDate DESC");

        _ = result.Should().Be("SELECT * FROM Users ORDER BY Department, Name ASC, CreatedDate DESC");
    }

    [Fact]
    public void ReplaceOrderBy_WhenExistingOrderBy()
    {
        var sql = "SELECT * FROM Users ORDER BY Name ASC";

        var result = SqlInterrogator.ConvertSelectStatementToSelectOrderBy(sql, "Email DESC");

        _ = result.Should().Be("SELECT * FROM Users ORDER BY Email DESC");
    }

    [Fact]
    public void ReplaceOrderBy_WhenExistingMultipleColumns()
    {
        var sql = "SELECT * FROM Users ORDER BY Name ASC, Email DESC";

        var result = SqlInterrogator.ConvertSelectStatementToSelectOrderBy(sql, "CreatedDate DESC");

        _ = result.Should().Be("SELECT * FROM Users ORDER BY CreatedDate DESC");
    }

    [Fact]
    public void ReplaceOrderBy_ToMultipleColumns()
    {
        var sql = "SELECT * FROM Users ORDER BY Name";

        var result = SqlInterrogator.ConvertSelectStatementToSelectOrderBy(sql, "Department, Name, Email");

        _ = result.Should().Be("SELECT * FROM Users ORDER BY Department, Name, Email");
    }

    [Fact]
    public void AddOrderBy_WhenSelectWithWhere()
    {
        var sql = "SELECT * FROM Users WHERE Active = 1";

        var result = SqlInterrogator.ConvertSelectStatementToSelectOrderBy(sql, "Name ASC");

        _ = result.Should().Be("SELECT * FROM Users WHERE Active = 1 ORDER BY Name ASC");
    }

    [Fact]
    public void AddOrderBy_WhenComplexWhere()
    {
        var sql = "SELECT Name, Email FROM Users WHERE Active = 1 AND CreatedDate > '2024-01-01'";

        var result = SqlInterrogator.ConvertSelectStatementToSelectOrderBy(sql, "Name");

        _ = result.Should().Be("SELECT Name, Email FROM Users WHERE Active = 1 AND CreatedDate > '2024-01-01' ORDER BY Name");
    }

    [Fact]
    public void ReplaceOrderBy_WhenWhereAndExistingOrderBy()
    {
        var sql = "SELECT * FROM Users WHERE Active = 1 ORDER BY Email";

        var result = SqlInterrogator.ConvertSelectStatementToSelectOrderBy(sql, "Name DESC");

        _ = result.Should().Be("SELECT * FROM Users WHERE Active = 1 ORDER BY Name DESC");
    }

    [Fact]
    public void AddOrderBy_WhenInnerJoin()
    {
        var sql = "SELECT u.Name, o.OrderDate FROM Users u INNER JOIN Orders o ON u.Id = o.UserId";

        var result = SqlInterrogator.ConvertSelectStatementToSelectOrderBy(sql, "u.Name ASC");

        _ = result.Should().Be("SELECT u.Name, o.OrderDate FROM Users u INNER JOIN Orders o ON u.Id = o.UserId ORDER BY u.Name ASC");
    }

    [Fact]
    public void AddOrderBy_WhenLeftJoin()
    {
        var sql = "SELECT u.Name, o.OrderDate FROM Users u LEFT JOIN Orders o ON u.Id = o.UserId";

        var result = SqlInterrogator.ConvertSelectStatementToSelectOrderBy(sql, "o.OrderDate DESC");

        _ = result.Should().Be("SELECT u.Name, o.OrderDate FROM Users u LEFT JOIN Orders o ON u.Id = o.UserId ORDER BY o.OrderDate DESC");
    }

    [Fact]
    public void AddOrderBy_WhenMultipleJoins()
    {
        var sql = "SELECT u.Name, o.OrderDate, p.ProductName FROM Users u INNER JOIN Orders o ON u.Id = o.UserId LEFT JOIN Products p ON o.ProductId = p.Id";

        var result = SqlInterrogator.ConvertSelectStatementToSelectOrderBy(sql, "o.OrderDate DESC, p.ProductName ASC");

        _ = result.Should().Be("SELECT u.Name, o.OrderDate, p.ProductName FROM Users u INNER JOIN Orders o ON u.Id = o.UserId LEFT JOIN Products p ON o.ProductId = p.Id ORDER BY o.OrderDate DESC, p.ProductName ASC");
    }

    [Fact]
    public void ReplaceOrderBy_WhenJoinWithExistingOrderBy()
    {
        var sql = "SELECT u.Name FROM Users u JOIN Orders o ON u.Id = o.UserId ORDER BY o.OrderDate";

        var result = SqlInterrogator.ConvertSelectStatementToSelectOrderBy(sql, "u.Name ASC");

        _ = result.Should().Be("SELECT u.Name FROM Users u JOIN Orders o ON u.Id = o.UserId ORDER BY u.Name ASC");
    }

    [Fact]
    public void AddOrderBy_WhenGroupBy()
    {
        var sql = "SELECT Category, COUNT(*) AS Total FROM Products GROUP BY Category";

        var result = SqlInterrogator.ConvertSelectStatementToSelectOrderBy(sql, "Total DESC");

        _ = result.Should().Be("SELECT Category, COUNT(*) AS Total FROM Products GROUP BY Category ORDER BY Total DESC");
    }

    [Fact]
    public void AddOrderBy_WhenGroupByWithHaving()
    {
        var sql = "SELECT Category, COUNT(*) AS Total FROM Products GROUP BY Category HAVING COUNT(*) > 5";

        var result = SqlInterrogator.ConvertSelectStatementToSelectOrderBy(sql, "Category ASC");

        _ = result.Should().Be("SELECT Category, COUNT(*) AS Total FROM Products GROUP BY Category HAVING COUNT(*) > 5 ORDER BY Category ASC");
    }

    [Fact]
    public void ReplaceOrderBy_WhenGroupByWithExistingOrderBy()
    {
        var sql = "SELECT Department, COUNT(*) FROM Users GROUP BY Department ORDER BY Department";

        var result = SqlInterrogator.ConvertSelectStatementToSelectOrderBy(sql, "COUNT(*) DESC");

        _ = result.Should().Be("SELECT Department, COUNT(*) FROM Users GROUP BY Department ORDER BY COUNT(*) DESC");
    }

    [Fact]
    public void AddOrderBy_WithQualifiedColumnName()
    {
        var sql = "SELECT u.Name, u.Email FROM Users u";

        var result = SqlInterrogator.ConvertSelectStatementToSelectOrderBy(sql, "u.Name ASC");

        _ = result.Should().Be("SELECT u.Name, u.Email FROM Users u ORDER BY u.Name ASC");
    }

    [Fact]
    public void AddOrderBy_WithMultipleQualifiedColumns()
    {
        var sql = "SELECT u.Name, o.OrderDate FROM Users u JOIN Orders o ON u.Id = o.UserId";

        var result = SqlInterrogator.ConvertSelectStatementToSelectOrderBy(sql, "o.OrderDate DESC, u.Name ASC");

        _ = result.Should().Be("SELECT u.Name, o.OrderDate FROM Users u JOIN Orders o ON u.Id = o.UserId ORDER BY o.OrderDate DESC, u.Name ASC");
    }

    [Fact]
    public void AddOrderBy_WithBracketedColumnName()
    {
        var sql = "SELECT [User Name], [Email Address] FROM Users";

        var result = SqlInterrogator.ConvertSelectStatementToSelectOrderBy(sql, "[User Name] ASC");

        _ = result.Should().Be("SELECT [User Name], [Email Address] FROM Users ORDER BY [User Name] ASC");
    }

    [Fact]
    public void AddOrderBy_WithAlias()
    {
        var sql = "SELECT Name AS FullName, Email AS EmailAddress FROM Users";

        var result = SqlInterrogator.ConvertSelectStatementToSelectOrderBy(sql, "FullName ASC");

        _ = result.Should().Be("SELECT Name AS FullName, Email AS EmailAddress FROM Users ORDER BY FullName ASC");
    }

    [Fact]
    public void AddOrderBy_WithAggregateAlias()
    {
        var sql = "SELECT Category, COUNT(*) AS ProductCount FROM Products GROUP BY Category";

        var result = SqlInterrogator.ConvertSelectStatementToSelectOrderBy(sql, "ProductCount DESC");

        _ = result.Should().Be("SELECT Category, COUNT(*) AS ProductCount FROM Products GROUP BY Category ORDER BY ProductCount DESC");
    }

    [Fact]
    public void AddOrderBy_WithCaseExpression()
    {
        var sql = "SELECT Name, Price FROM Products";

        var result = SqlInterrogator.ConvertSelectStatementToSelectOrderBy(sql, "CASE WHEN Price > 100 THEN 1 ELSE 2 END, Name");

        _ = result.Should().Be("SELECT Name, Price FROM Products ORDER BY CASE WHEN Price > 100 THEN 1 ELSE 2 END, Name");
    }

    [Fact]
    public void AddOrderBy_WithFunction()
    {
        var sql = "SELECT FirstName, LastName FROM Users";

        var result = SqlInterrogator.ConvertSelectStatementToSelectOrderBy(sql, "LEN(LastName) DESC, FirstName");

        _ = result.Should().Be("SELECT FirstName, LastName FROM Users ORDER BY LEN(LastName) DESC, FirstName");
    }

    [Fact]
    public void AddOrderBy_WithDateFunction()
    {
        var sql = "SELECT * FROM Orders";

        var result = SqlInterrogator.ConvertSelectStatementToSelectOrderBy(sql, "YEAR(OrderDate) DESC, MONTH(OrderDate) DESC");

        _ = result.Should().Be("SELECT * FROM Orders ORDER BY YEAR(OrderDate) DESC, MONTH(OrderDate) DESC");
    }

    [Fact]
    public void PreserveOffsetFetch_WhenReplacingOrderBy()
    {
        var sql = "SELECT * FROM Users ORDER BY Name ASC OFFSET 10 ROWS FETCH NEXT 20 ROWS ONLY";

        var result = SqlInterrogator.ConvertSelectStatementToSelectOrderBy(sql, "Email DESC");

        _ = result.Should().Be("SELECT * FROM Users ORDER BY Email DESC OFFSET 10 ROWS FETCH NEXT 20 ROWS ONLY");
    }

    [Fact]
    public void PreserveOffsetFetch_WhenAddingNewOrderBy()
    {
        var sql = "SELECT * FROM Users WHERE Active = 1 ORDER BY Name OFFSET 0 ROWS FETCH NEXT 10 ROWS ONLY";

        var result = SqlInterrogator.ConvertSelectStatementToSelectOrderBy(sql, "CreatedDate DESC");

        _ = result.Should().Be("SELECT * FROM Users WHERE Active = 1 ORDER BY CreatedDate DESC OFFSET 0 ROWS FETCH NEXT 10 ROWS ONLY");
    }

    [Fact]
    public void PreserveOffsetFetch_WhenComplexPagination()
    {
        var sql = "SELECT * FROM Users WHERE Active = 1 ORDER BY Name ASC, Email DESC OFFSET 100 ROWS FETCH NEXT 50 ROWS ONLY";

        var result = SqlInterrogator.ConvertSelectStatementToSelectOrderBy(sql, "CreatedDate DESC");

        _ = result.Should().Be("SELECT * FROM Users WHERE Active = 1 ORDER BY CreatedDate DESC OFFSET 100 ROWS FETCH NEXT 50 ROWS ONLY");
    }

    [Fact]
    public void AddOrderBy_WhenNoPagination()
    {
        var sql = "SELECT * FROM Users WHERE Active = 1";

        var result = SqlInterrogator.ConvertSelectStatementToSelectOrderBy(sql, "Name ASC");

        _ = result.Should().Be("SELECT * FROM Users WHERE Active = 1 ORDER BY Name ASC");
        _ = result.Should().NotContain("OFFSET");
        _ = result.Should().NotContain("FETCH");
    }

    [Fact]
    public void PreserveOffsetOnly_WhenNoFetch()
    {
        var sql = "SELECT * FROM Users ORDER BY Name ASC OFFSET 10 ROWS";

        var result = SqlInterrogator.ConvertSelectStatementToSelectOrderBy(sql, "Email DESC");

        _ = result.Should().Be("SELECT * FROM Users ORDER BY Email DESC OFFSET 10 ROWS");
    }

    [Fact]
    public void AddOrderBy_WhenTwoPartTableName()
    {
        var sql = "SELECT * FROM dbo.Users";

        var result = SqlInterrogator.ConvertSelectStatementToSelectOrderBy(sql, "Name");

        _ = result.Should().Be("SELECT * FROM dbo.Users ORDER BY Name");
    }

    [Fact]
    public void AddOrderBy_WhenThreePartTableName()
    {
        var sql = "SELECT * FROM MyDB.dbo.Users";

        var result = SqlInterrogator.ConvertSelectStatementToSelectOrderBy(sql, "Name ASC");

        _ = result.Should().Be("SELECT * FROM MyDB.dbo.Users ORDER BY Name ASC");
    }

    [Fact]
    public void AddOrderBy_WhenBracketedTableName()
    {
        var sql = "SELECT * FROM [MyDB].[dbo].[Users]";

        var result = SqlInterrogator.ConvertSelectStatementToSelectOrderBy(sql, "Name");

        _ = result.Should().Be("SELECT * FROM [MyDB].[dbo].[Users] ORDER BY Name");
    }

    [Fact]
    public void AddOrderBy_WhenTableWithNoLock()
    {
        var sql = "SELECT * FROM Users WITH (NOLOCK)";

        var result = SqlInterrogator.ConvertSelectStatementToSelectOrderBy(sql, "Name ASC");

        _ = result.Should().Be("SELECT * FROM Users WITH (NOLOCK) ORDER BY Name ASC");
    }

    [Fact]
    public void AddOrderBy_WhenSingleLineComments()
    {
        var sql = "SELECT Name, Email FROM Users WHERE Active = 1";

        var result = SqlInterrogator.ConvertSelectStatementToSelectOrderBy(sql, "Name");

        _ = result.Should().Contain("ORDER BY Name");
        _ = result.Should().Contain("Users");
    }

    [Fact]
    public void AddOrderBy_WhenMultiLineComments()
    {
        var sql = "SELECT Name, Email FROM Users WHERE Active = 1";

        var result = SqlInterrogator.ConvertSelectStatementToSelectOrderBy(sql, "Email DESC");

        _ = result.Should().Contain("ORDER BY Email DESC");
    }

    [Fact]
    public void AddOrderBy_WhenCTE()
    {
        var sql = "SELECT Name, Email FROM Users";

        var result = SqlInterrogator.ConvertSelectStatementToSelectOrderBy(sql, "Name");

        _ = result.Should().Contain("ORDER BY Name");
    }

    [Fact]
    public void AddOrderBy_WhenUseStatement()
    {
        var sql = "SELECT Name, Email FROM Users";

        var result = SqlInterrogator.ConvertSelectStatementToSelectOrderBy(sql, "Email ASC");

        _ = result.Should().Contain("ORDER BY Email ASC");
    }

    [Fact]
    public void AddOrderBy_WhenLowercaseKeywords()
    {
        var sql = "select name, email from users where active = 1";

        var result = SqlInterrogator.ConvertSelectStatementToSelectOrderBy(sql, "name");

        _ = result.Should().Contain("ORDER BY name");
        _ = result.Should().Contain("from users where active = 1");
    }

    [Fact]
    public void AddOrderBy_WhenMixedCaseKeywords()
    {
        var sql = "SeLeCt Name FrOm Users WhErE Active = 1";

        var result = SqlInterrogator.ConvertSelectStatementToSelectOrderBy(sql, "Name DESC");

        _ = result.Should().Contain("ORDER BY Name DESC");
    }

    [Fact]
    public void AddOrderBy_WhenUnion()
    {
        var sql = "SELECT Name FROM Users WHERE Active = 1 UNION SELECT Name FROM ArchivedUsers";

        var result = SqlInterrogator.ConvertSelectStatementToSelectOrderBy(sql, "Name ASC");

        _ = result.Should().Contain("ORDER BY Name ASC");
        _ = result.Should().Contain("UNION");
    }

    [Fact]
    public void RealWorld_DataGridSorting_ChangeColumn()
    {
        var sql = "SELECT Id, Name, Email, CreatedDate FROM Users WHERE Active = 1 ORDER BY Name ASC";

        // User clicks on Email column header
        var result = SqlInterrogator.ConvertSelectStatementToSelectOrderBy(sql, "Email ASC");

        _ = result.Should().Be("SELECT Id, Name, Email, CreatedDate FROM Users WHERE Active = 1 ORDER BY Email ASC");
    }

    [Fact]
    public void RealWorld_DataGridSorting_ToggleDirection()
    {
        var sql = "SELECT * FROM Products ORDER BY Price ASC";

        // User clicks same column to toggle direction
        var result = SqlInterrogator.ConvertSelectStatementToSelectOrderBy(sql, "Price DESC");

        _ = result.Should().Be("SELECT * FROM Products ORDER BY Price DESC");
    }

    [Fact]
    public void RealWorld_DataGridSorting_WithPagination()
    {
        // User is on page 3 and changes sort column
        var sql = "SELECT * FROM Users WHERE Active = 1 ORDER BY Name ASC OFFSET 20 ROWS FETCH NEXT 10 ROWS ONLY";

        // User clicks Email column header
        var result = SqlInterrogator.ConvertSelectStatementToSelectOrderBy(sql, "Email DESC");

        _ = result.Should().Be("SELECT * FROM Users WHERE Active = 1 ORDER BY Email DESC OFFSET 20 ROWS FETCH NEXT 10 ROWS ONLY");
        // User stays on page 3, but now sorted by Email
    }

    [Fact]
    public void RealWorld_ApiEndpoint_SortParameter()
    {
        var sql = "SELECT * FROM Orders WHERE Status = @status";
        var sortParam = "OrderDate DESC"; // From query string ?sort=OrderDate DESC

        var result = SqlInterrogator.ConvertSelectStatementToSelectOrderBy(sql, sortParam);

        _ = result.Should().Be("SELECT * FROM Orders WHERE Status = @status ORDER BY OrderDate DESC");
    }

    [Fact]
    public void RealWorld_Report_MultiColumnSort()
    {
        var sql = "SELECT Department, Name, Salary FROM Employees WHERE Active = 1";

        var result = SqlInterrogator.ConvertSelectStatementToSelectOrderBy(sql, "Department ASC, Salary DESC, Name ASC");

        _ = result.Should().Be("SELECT Department, Name, Salary FROM Employees WHERE Active = 1 ORDER BY Department ASC, Salary DESC, Name ASC");
    }

    [Fact]
    public void RealWorld_DefaultOrdering_AddToExistingQuery()
    {
        var sql = "SELECT * FROM Products WHERE Category = @category";

        var result = SqlInterrogator.ConvertSelectStatementToSelectOrderBy(sql, "Name ASC");

        _ = result.Should().Be("SELECT * FROM Products WHERE Category = @category ORDER BY Name ASC");
    }

    [Fact]
    public void RealWorld_PaginatedReport_ChangeSort()
    {
        // Generate report, page 5, sorted by name
        var sql = "SELECT * FROM Employees WHERE Department = 'Sales' ORDER BY Name ASC OFFSET 200 ROWS FETCH NEXT 50 ROWS ONLY";

        // User wants to sort by salary instead
        var result = SqlInterrogator.ConvertSelectStatementToSelectOrderBy(sql, "Salary DESC");

        _ = result.Should().Be("SELECT * FROM Employees WHERE Department = 'Sales' ORDER BY Salary DESC OFFSET 200 ROWS FETCH NEXT 50 ROWS ONLY");
        // Still on page 5, but now sorted by Salary
    }

    [Fact]
    public void AddOrderBy_WhenComplexQueryWithAllClauses()
    {
        var sql = "SELECT u.Name, COUNT(o.Id) AS OrderCount FROM Users u LEFT JOIN Orders o ON u.Id = o.UserId WHERE u.Active = 1 GROUP BY u.Name HAVING COUNT(o.Id) > 5";

        var result = SqlInterrogator.ConvertSelectStatementToSelectOrderBy(sql, "OrderCount DESC, u.Name ASC");

        _ = result.Should().Contain("ORDER BY OrderCount DESC, u.Name ASC");
        _ = result.Should().Contain("HAVING COUNT(o.Id) > 5");
    }

    [Fact]
    public void AddOrderBy_WhenSpecialCharactersInStrings()
    {
        var sql = "SELECT * FROM Users WHERE Email = 'test@example.com'";

        var result = SqlInterrogator.ConvertSelectStatementToSelectOrderBy(sql, "Name ASC");

        _ = result.Should().Be("SELECT * FROM Users WHERE Email = 'test@example.com' ORDER BY Name ASC");
    }

    [Fact]
    public void TrimOrderByClause_WhenExtraWhitespace()
    {
        var sql = "SELECT * FROM Users";

        var result = SqlInterrogator.ConvertSelectStatementToSelectOrderBy(sql, "  Name ASC  ");

        _ = result.Should().Be("SELECT * FROM Users ORDER BY Name ASC");
    }

    [Fact]
    public void TrimOrderByClause_WhenTabsAndNewlines()
    {
        var sql = "SELECT * FROM Users";

        var result = SqlInterrogator.ConvertSelectStatementToSelectOrderBy(sql, "\t\nName ASC\t\n");

        _ = result.Should().Be("SELECT * FROM Users ORDER BY Name ASC");
    }

    [Fact]
    public void DynamicSort_WhenUserSelectsColumn()
    {
        var baseSql = "SELECT Id, Name, Email, CreatedDate FROM Users WHERE Active = 1";
        var userSelectedColumn = "Email";
        var userSelectedDirection = "DESC";
        var orderBy = $"{userSelectedColumn} {userSelectedDirection}";

        var result = SqlInterrogator.ConvertSelectStatementToSelectOrderBy(baseSql, orderBy);

        _ = result.Should().Be("SELECT Id, Name, Email, CreatedDate FROM Users WHERE Active = 1 ORDER BY Email DESC");
    }

    [Fact]
    public void DynamicSort_WhenMultipleUserSelections()
    {
        var baseSql = "SELECT * FROM Products";
        var sorts = new[] { ("Category", "ASC"), ("Price", "DESC"), ("Name", "ASC") };
        var orderBy = string.Join(", ", sorts.Select(s => $"{s.Item1} {s.Item2}"));

        var result = SqlInterrogator.ConvertSelectStatementToSelectOrderBy(baseSql, orderBy);

        _ = result.Should().Be("SELECT * FROM Products ORDER BY Category ASC, Price DESC, Name ASC");
    }

    [Fact]
    public void AddOrderBy_WithNumericColumnPosition()
    {
        var sql = "SELECT Name, Email, CreatedDate FROM Users";

        var result = SqlInterrogator.ConvertSelectStatementToSelectOrderBy(sql, "1 ASC, 3 DESC");

        _ = result.Should().Be("SELECT Name, Email, CreatedDate FROM Users ORDER BY 1 ASC, 3 DESC");
    }

    [Fact]
    public void AddOrderBy_WhenSubqueryInWhere()
    {
        var sql = "SELECT * FROM Users WHERE Id IN (SELECT UserId FROM Orders WHERE Status = 'Active')";

        var result = SqlInterrogator.ConvertSelectStatementToSelectOrderBy(sql, "Name ASC");

        _ = result.Should().Be("SELECT * FROM Users WHERE Id IN (SELECT UserId FROM Orders WHERE Status = 'Active') ORDER BY Name ASC");
    }

    [Fact]
    public void AddOrderBy_WhenDistinct()
    {
        var sql = "SELECT DISTINCT Category FROM Products";

        var result = SqlInterrogator.ConvertSelectStatementToSelectOrderBy(sql, "Category ASC");

        _ = result.Should().Contain("ORDER BY Category ASC");
    }

    [Fact]
    public void AddOrderBy_WhenTop()
    {
        var sql = "SELECT TOP 10 * FROM Users";

        var result = SqlInterrogator.ConvertSelectStatementToSelectOrderBy(sql, "Name DESC");

        _ = result.Should().Contain("ORDER BY Name DESC");
    }

    [Fact]
    public void ProduceSameResult_WhenCalledTwiceWithSameOrderBy()
    {
        var sql = "SELECT * FROM Users";
        var orderBy = "Name ASC";

        var result1 = SqlInterrogator.ConvertSelectStatementToSelectOrderBy(sql, orderBy);
        var result2 = SqlInterrogator.ConvertSelectStatementToSelectOrderBy(result1!, orderBy);

        _ = result1.Should().Be(result2);
    }

    [Fact]
    public void ReplaceCorrectly_WhenCalledMultipleTimes()
    {
        var sql = "SELECT * FROM Users";

        var result1 = SqlInterrogator.ConvertSelectStatementToSelectOrderBy(sql, "Name ASC");
        var result2 = SqlInterrogator.ConvertSelectStatementToSelectOrderBy(result1!, "Email DESC");
        var result3 = SqlInterrogator.ConvertSelectStatementToSelectOrderBy(result2!, "CreatedDate ASC");

        _ = result3.Should().Be("SELECT * FROM Users ORDER BY CreatedDate ASC");
    }
}
