using FluentAssertions;
using SqlUrlParameteriserService;

namespace SqlUrlParameteriserServiceTest;

public class ParameteriseSqlFromUrl_Should
{
    #region Null and Empty Input Tests

    [Fact]
    public void ReturnNull_WhenSqlIsNull()
    {
        var url = "https://example.com?parameters=userId=123";

        var result = SqlUrlParameteriser.ParameteriseSqlFromUrl(null, url);

        _ = result.Should().BeNull();
    }

    [Fact]
    public void ReturnNull_WhenSqlIsEmpty()
    {
        var url = "https://example.com?parameters=userId=123";

        var result = SqlUrlParameteriser.ParameteriseSqlFromUrl("", url);

        _ = result.Should().BeNull();
    }

    [Fact]
    public void ReturnNull_WhenSqlIsWhitespace()
    {
        var url = "https://example.com?parameters=userId=123";

        var result = SqlUrlParameteriser.ParameteriseSqlFromUrl("   ", url);

        _ = result.Should().BeNull();
    }

    [Fact]
    public void ReturnNull_WhenUrlIsNull()
    {
        var sql = "SELECT * FROM Users WHERE Id = @userId";

        var result = SqlUrlParameteriser.ParameteriseSqlFromUrl(sql, null);

        _ = result.Should().BeNull();
    }

    [Fact]
    public void ReturnNull_WhenUrlIsEmpty()
    {
        var sql = "SELECT * FROM Users WHERE Id = @userId";

        var result = SqlUrlParameteriser.ParameteriseSqlFromUrl(sql, "");

        _ = result.Should().BeNull();
    }

    [Fact]
    public void ReturnNull_WhenUrlIsWhitespace()
    {
        var sql = "SELECT * FROM Users WHERE Id = @userId";

        var result = SqlUrlParameteriser.ParameteriseSqlFromUrl(sql, "   ");

        _ = result.Should().BeNull();
    }

    [Fact]
    public void ReturnNull_WhenBothAreNull()
    {
        var result = SqlUrlParameteriser.ParameteriseSqlFromUrl(null, null);

        _ = result.Should().BeNull();
    }

    #endregion

    #region No Parameters Tests

    [Fact]
    public void ReturnUnchangedSql_WhenUrlHasNoQueryString()
    {
        var sql = "SELECT * FROM Users WHERE Id = @userId";
        var url = "https://example.com";

        var result = SqlUrlParameteriser.ParameteriseSqlFromUrl(sql, url);

        _ = result.Should().Be(sql);
    }

    [Fact]
    public void ReturnUnchangedSql_WhenUrlHasNoParametersKey()
    {
        var sql = "SELECT * FROM Users WHERE Id = @userId";
        var url = "https://example.com?page=1&limit=10";

        var result = SqlUrlParameteriser.ParameteriseSqlFromUrl(sql, url);

        _ = result.Should().Be(sql);
    }

    [Fact]
    public void ReturnUnchangedSql_WhenParametersValueIsEmpty()
    {
        var sql = "SELECT * FROM Users WHERE Id = @userId";
        var url = "https://example.com?parameters=";

        var result = SqlUrlParameteriser.ParameteriseSqlFromUrl(sql, url);

        _ = result.Should().Be(sql);
    }

    #endregion

    #region Standard Format (@paramName) Tests

    [Fact]
    public void ReplaceParameter_WhenStandardFormat_NumericValue()
    {
        var sql = "SELECT * FROM Users WHERE Id = @userId";
        var url = "https://example.com?parameters=userId=123";

        var result = SqlUrlParameteriser.ParameteriseSqlFromUrl(sql, url);

        _ = result.Should().Be("SELECT * FROM Users WHERE Id = 123");
    }

    [Fact]
    public void ReplaceParameter_WhenStandardFormat_StringValue()
    {
        var sql = "SELECT * FROM Users WHERE Status = @status";
        var url = "https://example.com?parameters=status=active";

        var result = SqlUrlParameteriser.ParameteriseSqlFromUrl(sql, url);

        _ = result.Should().Be("SELECT * FROM Users WHERE Status = 'active'");
    }

    [Fact]
    public void ReplaceMultipleParameters_WhenStandardFormat()
    {
        var sql = "SELECT * FROM Users WHERE Id = @userId AND Status = @status";
        var url = "https://example.com?parameters=userId=123;status=active";

        var result = SqlUrlParameteriser.ParameteriseSqlFromUrl(sql, url);

        _ = result.Should().Be("SELECT * FROM Users WHERE Id = 123 AND Status = 'active'");
    }

    [Fact]
    public void ReplaceThreeParameters_WhenStandardFormat()
    {
        var sql = "SELECT * FROM Users WHERE Id = @userId AND Status = @status AND Role = @role";
        var url = "https://example.com?parameters=userId=456;status=pending;role=admin";

        var result = SqlUrlParameteriser.ParameteriseSqlFromUrl(sql, url);

        _ = result.Should().Be("SELECT * FROM Users WHERE Id = 456 AND Status = 'pending' AND Role = 'admin'");
    }

    #endregion

    #region Bracketed Format ([@paramName]) Tests

    [Fact]
    public void ReplaceParameter_WhenBracketedFormat_NumericValue()
    {
        var sql = "SELECT * FROM Users WHERE Id = [@userId]";
        var url = "https://example.com?parameters=userId=789";

        var result = SqlUrlParameteriser.ParameteriseSqlFromUrl(sql, url);

        _ = result.Should().Be("SELECT * FROM Users WHERE Id = 789");
    }

    [Fact]
    public void ReplaceParameter_WhenBracketedFormat_StringValue()
    {
        var sql = "SELECT * FROM Users WHERE Status = [@status]";
        var url = "https://example.com?parameters=status=completed";

        var result = SqlUrlParameteriser.ParameteriseSqlFromUrl(sql, url);

        _ = result.Should().Be("SELECT * FROM Users WHERE Status = 'completed'");
    }

    [Fact]
    public void ReplaceMultipleParameters_WhenBracketedFormat()
    {
        var sql = "SELECT * FROM Users WHERE Id = [@userId] AND Status = [@status]";
        var url = "https://example.com?parameters=userId=999;status=inactive";

        var result = SqlUrlParameteriser.ParameteriseSqlFromUrl(sql, url);

        _ = result.Should().Be("SELECT * FROM Users WHERE Id = 999 AND Status = 'inactive'");
    }

    #endregion

    #region Mixed Format Tests

    [Fact]
    public void ReplaceParameters_WhenMixedFormat_StandardThenBracketed()
    {
        var sql = "SELECT * FROM Users WHERE Id = @userId AND Status = [@status]";
        var url = "https://example.com?parameters=userId=111;status=verified";

        var result = SqlUrlParameteriser.ParameteriseSqlFromUrl(sql, url);

        _ = result.Should().Be("SELECT * FROM Users WHERE Id = 111 AND Status = 'verified'");
    }

    [Fact]
    public void ReplaceParameters_WhenMixedFormat_BracketedThenStandard()
    {
        var sql = "SELECT * FROM Users WHERE Id = [@userId] AND Status = @status";
        var url = "https://example.com?parameters=userId=222;status=suspended";

        var result = SqlUrlParameteriser.ParameteriseSqlFromUrl(sql, url);

        _ = result.Should().Be("SELECT * FROM Users WHERE Id = 222 AND Status = 'suspended'");
    }

    [Fact]
    public void ReplaceParameters_WhenMixedFormat_MultipleOfEach()
    {
        var sql = "SELECT * FROM Orders WHERE UserId = @userId AND Status = [@status] AND Price > @minPrice AND Category = [@category]";
        var url = "https://example.com?parameters=userId=333;status=shipped;minPrice=50;category=Electronics";

        var result = SqlUrlParameteriser.ParameteriseSqlFromUrl(sql, url);

        _ = result.Should().Be("SELECT * FROM Orders WHERE UserId = 333 AND Status = 'shipped' AND Price > 50 AND Category = 'Electronics'");
    }

    #endregion

    #region Numeric Value Tests

    [Fact]
    public void HandleDecimalNumber()
    {
        var sql = "SELECT * FROM Products WHERE Price = @price";
        var url = "https://example.com?parameters=price=99.99";

        var result = SqlUrlParameteriser.ParameteriseSqlFromUrl(sql, url);

        _ = result.Should().Be("SELECT * FROM Products WHERE Price = 99.99");
    }

    [Fact]
    public void HandleNegativeNumber()
    {
        var sql = "SELECT * FROM Transactions WHERE Amount = @amount";
        var url = "https://example.com?parameters=amount=-100";

        var result = SqlUrlParameteriser.ParameteriseSqlFromUrl(sql, url);

        _ = result.Should().Be("SELECT * FROM Transactions WHERE Amount = -100");
    }

    [Fact]
    public void HandleZero()
    {
        var sql = "SELECT * FROM Products WHERE Stock = @stock";
        var url = "https://example.com?parameters=stock=0";

        var result = SqlUrlParameteriser.ParameteriseSqlFromUrl(sql, url);

        _ = result.Should().Be("SELECT * FROM Products WHERE Stock = 0");
    }

    [Fact]
    public void HandleLargeNumber()
    {
        var sql = "SELECT * FROM Data WHERE Value = @value";
        var url = "https://example.com?parameters=value=999999999";

        var result = SqlUrlParameteriser.ParameteriseSqlFromUrl(sql, url);

        _ = result.Should().Be("SELECT * FROM Data WHERE Value = 999999999");
    }

    [Fact]
    public void HandleScientificNotation()
    {
        var sql = "SELECT * FROM Scientific WHERE Measurement = @measurement";
        var url = "https://example.com?parameters=measurement=1.5e10";

        var result = SqlUrlParameteriser.ParameteriseSqlFromUrl(sql, url);

        _ = result.Should().Be("SELECT * FROM Scientific WHERE Measurement = 1.5e10");
    }

    [Fact]
    public void HandleBooleanAsNumber()
    {
        var sql = "SELECT * FROM Users WHERE Active = @active AND Verified = @verified";
        var url = "https://example.com?parameters=active=1;verified=0";

        var result = SqlUrlParameteriser.ParameteriseSqlFromUrl(sql, url);

        _ = result.Should().Be("SELECT * FROM Users WHERE Active = 1 AND Verified = 0");
    }

    #endregion

    #region Case Insensitivity Tests

    [Fact]
    public void HandleCaseInsensitiveParameterNames_UpperInSql()
    {
        var sql = "SELECT * FROM Users WHERE Id = @UserId";
        var url = "https://example.com?parameters=userid=123";

        var result = SqlUrlParameteriser.ParameteriseSqlFromUrl(sql, url);

        _ = result.Should().Be("SELECT * FROM Users WHERE Id = 123");
    }

    [Fact]
    public void HandleCaseInsensitiveParameterNames_UpperInUrl()
    {
        var sql = "SELECT * FROM Users WHERE Id = @userId";
        var url = "https://example.com?parameters=USERID=456";

        var result = SqlUrlParameteriser.ParameteriseSqlFromUrl(sql, url);

        _ = result.Should().Be("SELECT * FROM Users WHERE Id = 456");
    }

    [Fact]
    public void HandleCaseInsensitiveParameterNames_MixedCase()
    {
        var sql = "SELECT * FROM Users WHERE Id = @UsErId";
        var url = "https://example.com?parameters=uSeRiD=789";

        var result = SqlUrlParameteriser.ParameteriseSqlFromUrl(sql, url);

        _ = result.Should().Be("SELECT * FROM Users WHERE Id = 789");
    }

    [Fact]
    public void HandleCaseInsensitiveParameterNames_Bracketed()
    {
        var sql = "SELECT * FROM Users WHERE Id = [@USERID]";
        var url = "https://example.com?parameters=userid=999";

        var result = SqlUrlParameteriser.ParameteriseSqlFromUrl(sql, url);

        _ = result.Should().Be("SELECT * FROM Users WHERE Id = 999");
    }

    #endregion

    #region URL Encoding Tests

    [Fact]
    public void HandleUrlEncodedEmail()
    {
        var sql = "SELECT * FROM Users WHERE Email = @email";
        var url = "https://example.com?parameters=email=test%40example.com";

        var result = SqlUrlParameteriser.ParameteriseSqlFromUrl(sql, url);

        _ = result.Should().Be("SELECT * FROM Users WHERE Email = 'test@example.com'");
    }

    [Fact]
    public void HandleUrlEncodedSpaces()
    {
        var sql = "SELECT * FROM Users WHERE Name = @name";
        var url = "https://example.com?parameters=name=John%20Doe";

        var result = SqlUrlParameteriser.ParameteriseSqlFromUrl(sql, url);

        _ = result.Should().Be("SELECT * FROM Users WHERE Name = 'John Doe'");
    }

    [Fact]
    public void HandleUrlEncodedSpecialCharacters()
    {
        var sql = "SELECT * FROM Data WHERE Value = @value";
        var url = "https://example.com?parameters=value=%3Ctest%3E";

        var result = SqlUrlParameteriser.ParameteriseSqlFromUrl(sql, url);

        _ = result.Should().Be("SELECT * FROM Data WHERE Value = '<test>'");
    }

    #endregion

    #region URL Fragment Tests

    [Fact]
    public void IgnoreFragment_WhenPresent()
    {
        var sql = "SELECT * FROM Users WHERE Id = @userId";
        var url = "https://example.com?parameters=userId=123#section";

        var result = SqlUrlParameteriser.ParameteriseSqlFromUrl(sql, url);

        _ = result.Should().Be("SELECT * FROM Users WHERE Id = 123");
    }

    [Fact]
    public void IgnoreFragment_WithMultipleParameters()
    {
        var sql = "SELECT * FROM Users WHERE Id = @userId AND Status = @status";
        var url = "https://example.com?parameters=userId=456;status=active#top";

        var result = SqlUrlParameteriser.ParameteriseSqlFromUrl(sql, url);

        _ = result.Should().Be("SELECT * FROM Users WHERE Id = 456 AND Status = 'active'");
    }

    #endregion

    #region Multiple Occurrences Tests

    [Fact]
    public void ReplaceSameParameterMultipleTimes()
    {
        var sql = "SELECT * FROM Users WHERE CreatedBy = @userId OR ModifiedBy = @userId";
        var url = "https://example.com?parameters=userId=123";

        var result = SqlUrlParameteriser.ParameteriseSqlFromUrl(sql, url);

        _ = result.Should().Be("SELECT * FROM Users WHERE CreatedBy = 123 OR ModifiedBy = 123");
    }

    [Fact]
    public void ReplaceSameParameterMultipleTimes_Mixed()
    {
        var sql = "SELECT * FROM Audit WHERE CreatedBy = @userId OR ModifiedBy = [@userId] OR DeletedBy = @userId";
        var url = "https://example.com?parameters=userId=456";

        var result = SqlUrlParameteriser.ParameteriseSqlFromUrl(sql, url);

        _ = result.Should().Be("SELECT * FROM Audit WHERE CreatedBy = 456 OR ModifiedBy = 456 OR DeletedBy = 456");
    }

    #endregion

    #region Word Boundary Tests

    [Fact]
    public void NotReplacePartOfLongerParameterName()
    {
        var sql = "SELECT * FROM Users WHERE Id = @userId AND Email = @userIdEmail";
        var url = "https://example.com?parameters=userId=123";

        var result = SqlUrlParameteriser.ParameteriseSqlFromUrl(sql, url);

        _ = result.Should().Be("SELECT * FROM Users WHERE Id = 123 AND Email = @userIdEmail");
    }

    [Fact]
    public void NotReplacePartOfLongerParameterName_MultipleShortNames()
    {
        var sql = "SELECT * FROM Data WHERE A = @id AND B = @idNumber AND C = @userId";
        var url = "https://example.com?parameters=id=1";

        var result = SqlUrlParameteriser.ParameteriseSqlFromUrl(sql, url);

        _ = result.Should().Be("SELECT * FROM Data WHERE A = 1 AND B = @idNumber AND C = @userId");
    }

    #endregion

    #region Missing Parameters Tests

    [Fact]
    public void PreserveUnmatchedParameter_WhenOneMissing()
    {
        var sql = "SELECT * FROM Users WHERE Id = @userId AND Status = @status";
        var url = "https://example.com?parameters=userId=123";

        var result = SqlUrlParameteriser.ParameteriseSqlFromUrl(sql, url);

        _ = result.Should().Be("SELECT * FROM Users WHERE Id = 123 AND Status = @status");
    }

    [Fact]
    public void PreserveUnmatchedParameters_WhenMultipleMissing()
    {
        var sql = "SELECT * FROM Users WHERE Id = @userId AND Status = @status AND Role = @role";
        var url = "https://example.com?parameters=userId=456";

        var result = SqlUrlParameteriser.ParameteriseSqlFromUrl(sql, url);

        _ = result.Should().Be("SELECT * FROM Users WHERE Id = 456 AND Status = @status AND Role = @role");
    }

    [Fact]
    public void PreserveAllParameters_WhenNoMatches()
    {
        var sql = "SELECT * FROM Users WHERE Id = @userId AND Status = @status";
        var url = "https://example.com?parameters=someOtherParam=value";

        var result = SqlUrlParameteriser.ParameteriseSqlFromUrl(sql, url);

        _ = result.Should().Be(sql);
    }

    #endregion

    #region Complex SQL Tests

    [Fact]
    public void HandleComplexQuery_WithJoins()
    {
        var sql = @"SELECT u.Name, o.OrderDate 
                    FROM Users u 
                        INNER JOIN Orders o ON u.Id = o.UserId 
                    WHERE u.Id = @userId AND o.Status = @status";
        var url = "https://example.com?parameters=userId=123;status=completed";

        var result = SqlUrlParameteriser.ParameteriseSqlFromUrl(sql, url);

        _ = result.Should().Contain("u.Id = 123");
        _ = result.Should().Contain("o.Status = 'completed'");
    }

    [Fact]
    public void HandleComplexQuery_WithMultipleConditions()
    {
        var sql = @"SELECT * FROM Products 
                    WHERE CategoryId = @categoryId 
                    AND Price > @minPrice 
                    AND Price < @maxPrice 
                    AND InStock = @inStock";
        var url = "https://example.com?parameters=categoryId=5;minPrice=100;maxPrice=500;inStock=1";

        var result = SqlUrlParameteriser.ParameteriseSqlFromUrl(sql, url);

        _ = result.Should().Contain("CategoryId = 5");
        _ = result.Should().Contain("Price > 100");
        _ = result.Should().Contain("Price < 500");
        _ = result.Should().Contain("InStock = 1");
    }

    [Fact]
    public void HandleQuery_WithSubquery()
    {
        var sql = "SELECT * FROM Users WHERE Id IN (SELECT UserId FROM Orders WHERE Status = @status) AND Active = @active";
        var url = "https://example.com?parameters=status=pending;active=1";

        var result = SqlUrlParameteriser.ParameteriseSqlFromUrl(sql, url);

        _ = result.Should().Contain("Status = 'pending'");
        _ = result.Should().Contain("Active = 1");
    }

    [Fact]
    public void HandleQuery_WithOrderByAndGroupBy()
    {
        var sql = @"SELECT Category, COUNT(*) 
                    FROM Products 
                    WHERE Price > @minPrice 
                    GROUP BY Category 
                    ORDER BY @sortColumn";
        var url = "https://example.com?parameters=minPrice=50;sortColumn=Category";

        var result = SqlUrlParameteriser.ParameteriseSqlFromUrl(sql, url);

        _ = result.Should().Contain("Price > 50");
        _ = result.Should().Contain("ORDER BY 'Category'");
    }

    #endregion

    #region Whitespace Handling Tests

    [Fact]
    public void TrimWhitespace_InParameterNames()
    {
        var sql = "SELECT * FROM Users WHERE Id = @userId";
        var url = "https://example.com?parameters= userId =123";

        var result = SqlUrlParameteriser.ParameteriseSqlFromUrl(sql, url);

        _ = result.Should().Be("SELECT * FROM Users WHERE Id = 123");
    }

    [Fact]
    public void TrimWhitespace_InParameterValues()
    {
        var sql = "SELECT * FROM Users WHERE Status = @status";
        var url = "https://example.com?parameters=status= active ";

        var result = SqlUrlParameteriser.ParameteriseSqlFromUrl(sql, url);

        _ = result.Should().Be("SELECT * FROM Users WHERE Status = 'active'");
    }

    [Fact]
    public void TrimWhitespace_InMultipleParameters()
    {
        var sql = "SELECT * FROM Users WHERE Id = @userId AND Status = @status";
        var url = "https://example.com?parameters= userId = 123 ; status = pending ";

        var result = SqlUrlParameteriser.ParameteriseSqlFromUrl(sql, url);

        _ = result.Should().Be("SELECT * FROM Users WHERE Id = 123 AND Status = 'pending'");
    }

    #endregion

    #region Empty and Special Values Tests

    [Fact]
    public void HandleEmptyStringValue()
    {
        var sql = "SELECT * FROM Users WHERE MiddleName = @middleName";
        var url = "https://example.com?parameters=middleName=";

        var result = SqlUrlParameteriser.ParameteriseSqlFromUrl(sql, url);

        _ = result.Should().Be("SELECT * FROM Users WHERE MiddleName = ''");
    }

    [Fact]
    public void HandleDateTimeString()
    {
        var sql = "SELECT * FROM Orders WHERE OrderDate > @startDate";
        var url = "https://example.com?parameters=startDate=2024-01-01";

        var result = SqlUrlParameteriser.ParameteriseSqlFromUrl(sql, url);

        _ = result.Should().Be("SELECT * FROM Orders WHERE OrderDate > '2024-01-01'");
    }

    [Fact]
    public void HandleGuidString()
    {
        var sql = "SELECT * FROM Users WHERE Id = @userId";
        var url = "https://example.com?parameters=userId=550e8400-e29b-41d4-a716-446655440000";

        var result = SqlUrlParameteriser.ParameteriseSqlFromUrl(sql, url);

        _ = result.Should().Be("SELECT * FROM Users WHERE Id = '550e8400-e29b-41d4-a716-446655440000'");
    }

    [Fact]
    public void HandleValueWithEqualsSign()
    {
        var sql = "SELECT * FROM Data WHERE Query = @query";
        var url = "https://example.com?parameters=query=SELECT * FROM table WHERE id=1";

        var result = SqlUrlParameteriser.ParameteriseSqlFromUrl(sql, url);

        _ = result.Should().Be("SELECT * FROM Data WHERE Query = 'SELECT * FROM table WHERE id=1'");
    }

    #endregion

    #region URL Variations Tests

    [Fact]
    public void HandleUrlWithPath()
    {
        var sql = "SELECT * FROM Users WHERE Id = @userId";
        var url = "https://example.com/api/users?parameters=userId=123";

        var result = SqlUrlParameteriser.ParameteriseSqlFromUrl(sql, url);

        _ = result.Should().Be("SELECT * FROM Users WHERE Id = 123");
    }

    [Fact]
    public void HandleUrlWithPort()
    {
        var sql = "SELECT * FROM Users WHERE Id = @userId";
        var url = "https://example.com:8080?parameters=userId=456";

        var result = SqlUrlParameteriser.ParameteriseSqlFromUrl(sql, url);

        _ = result.Should().Be("SELECT * FROM Users WHERE Id = 456");
    }

    [Fact]
    public void HandleUrlWithOtherQueryParameters()
    {
        var sql = "SELECT * FROM Users WHERE Id = @userId";
        var url = "https://example.com?page=1&parameters=userId=789&limit=10";

        var result = SqlUrlParameteriser.ParameteriseSqlFromUrl(sql, url);

        _ = result.Should().Be("SELECT * FROM Users WHERE Id = 789");
    }

    [Fact]
    public void HandleUrlWithParametersNotFirst()
    {
        var sql = "SELECT * FROM Users WHERE Status = @status";
        var url = "https://example.com?sort=name&order=asc&parameters=status=active";

        var result = SqlUrlParameteriser.ParameteriseSqlFromUrl(sql, url);

        _ = result.Should().Be("SELECT * FROM Users WHERE Status = 'active'");
    }

    #endregion

    #region Separator Tests

    [Fact]
    public void HandleTrailingSemicolon()
    {
        var sql = "SELECT * FROM Users WHERE Id = @userId AND Status = @status";
        var url = "https://example.com?parameters=userId=123;status=active;";

        var result = SqlUrlParameteriser.ParameteriseSqlFromUrl(sql, url);

        _ = result.Should().Be("SELECT * FROM Users WHERE Id = 123 AND Status = 'active'");
    }

    [Fact]
    public void HandleLeadingSemicolon()
    {
        var sql = "SELECT * FROM Users WHERE Id = @userId AND Status = @status";
        var url = "https://example.com?parameters=;userId=123;status=active";

        var result = SqlUrlParameteriser.ParameteriseSqlFromUrl(sql, url);

        _ = result.Should().Be("SELECT * FROM Users WHERE Id = 123 AND Status = 'active'");
    }

    [Fact]
    public void HandleMultipleSemicolons()
    {
        var sql = "SELECT * FROM Users WHERE Id = @userId AND Status = @status";
        var url = "https://example.com?parameters=userId=123;;status=active";

        var result = SqlUrlParameteriser.ParameteriseSqlFromUrl(sql, url);

        _ = result.Should().Be("SELECT * FROM Users WHERE Id = 123 AND Status = 'active'");
    }

    #endregion

    #region Real-World Scenarios

    [Fact]
    public void RealWorldExample_ProductSearch()
    {
        var sql = @"SELECT * FROM Products 
                    WHERE CategoryId = @categoryId 
                    AND Price BETWEEN @minPrice AND @maxPrice 
                    AND Name LIKE @searchTerm 
                    ORDER BY Price";
        var url = "https://shop.example.com/api/products?parameters=categoryId=5;minPrice=50;maxPrice=200;searchTerm=%laptop%";

        var result = SqlUrlParameteriser.ParameteriseSqlFromUrl(sql, url);

        _ = result.Should().Contain("CategoryId = 5");
        _ = result.Should().Contain("BETWEEN 50 AND 200");
        _ = result.Should().Contain("LIKE '%laptop%'");
    }

    [Fact]
    public void RealWorldExample_UserAuthentication()
    {
        var sql = "SELECT * FROM Users WHERE Email = [@email] AND PasswordHash = [@passwordHash]";
        var url = "https://auth.example.com/login?parameters=email=user@example.com;passwordHash=abc123def456";

        var result = SqlUrlParameteriser.ParameteriseSqlFromUrl(sql, url);

        _ = result.Should().Be("SELECT * FROM Users WHERE Email = 'user@example.com' AND PasswordHash = 'abc123def456'");
    }

    [Fact]
    public void RealWorldExample_PaginationQuery()
    {
        var sql = @"SELECT * FROM Orders 
                    WHERE UserId = @userId 
                    ORDER BY OrderDate DESC 
                    OFFSET @offset ROWS 
                    FETCH NEXT @pageSize ROWS ONLY";
        var url = "https://api.example.com/orders?parameters=userId=123;offset=20;pageSize=10";

        var result = SqlUrlParameteriser.ParameteriseSqlFromUrl(sql, url);

        _ = result.Should().Contain("UserId = 123");
        _ = result.Should().Contain("OFFSET 20 ROWS");
        _ = result.Should().Contain("FETCH NEXT 10 ROWS ONLY");
    }

    [Fact]
    public void RealWorldExample_ReportingQuery()
    {
        var sql = @"SELECT 
                        u.Name, 
                        COUNT(o.Id) AS OrderCount,
                        SUM(o.Total) AS TotalAmount
                    FROM Users u
                        LEFT JOIN Orders o ON u.Id = o.UserId
                    WHERE o.OrderDate >= @startDate 
                    AND o.OrderDate <= @endDate
                    AND u.Region = @region
                    GROUP BY u.Name
                    HAVING COUNT(o.Id) > @minOrders";
        var url = "https://reports.example.com?parameters=startDate=2024-01-01;endDate=2024-12-31;region=North;minOrders=5";

        var result = SqlUrlParameteriser.ParameteriseSqlFromUrl(sql, url);

        _ = result.Should().Contain("o.OrderDate >= '2024-01-01'");
        _ = result.Should().Contain("o.OrderDate <= '2024-12-31'");
        _ = result.Should().Contain("u.Region = 'North'");
        _ = result.Should().Contain("COUNT(o.Id) > 5");
    }

    [Fact]
    public void RealWorldExample_DynamicFiltering()
    {
        var sql = @"SELECT * FROM Inventory 
                    WHERE WarehouseId = @warehouseId 
                    AND Quantity < @lowStockThreshold 
                    AND CategoryId IN (@categoryIds)
                    AND LastRestocked < @daysOld";
        var url = "https://inventory.example.com?parameters=warehouseId=10;lowStockThreshold=5;categoryIds=1,2,3;daysOld=2024-01-01";

        var result = SqlUrlParameteriser.ParameteriseSqlFromUrl(sql, url);

        _ = result.Should().Contain("WarehouseId = 10");
        _ = result.Should().Contain("Quantity < 5");
        // Note: "1,2,3" might be parsed as numeric in some cultures, so we check for either format
        var hasQuotedFormat = result.Contains("CategoryId IN ('1,2,3')");
        var hasUnquotedFormat = result.Contains("CategoryId IN (1,2,3)");
        (hasQuotedFormat || hasUnquotedFormat).Should().BeTrue("CategoryId should be replaced");
        _ = result.Should().Contain("LastRestocked < '2024-01-01'");
    }

    #endregion

    #region Edge Cases

    [Fact]
    public void HandleParameterAtStartOfSql()
    {
        var sql = "@userId = Id";
        var url = "https://example.com?parameters=userId=123";

        var result = SqlUrlParameteriser.ParameteriseSqlFromUrl(sql, url);

        _ = result.Should().Be("123 = Id");
    }

    [Fact]
    public void HandleParameterAtEndOfSql()
    {
        var sql = "SELECT * FROM Users WHERE Id = @userId";
        var url = "https://example.com?parameters=userId=999";

        var result = SqlUrlParameteriser.ParameteriseSqlFromUrl(sql, url);

        _ = result.Should().Be("SELECT * FROM Users WHERE Id = 999");
    }

    [Fact]
    public void HandleOnlyParameters_NoOtherText()
    {
        var sql = "@param1 @param2 @param3";
        var url = "https://example.com?parameters=param1=1;param2=2;param3=3";

        var result = SqlUrlParameteriser.ParameteriseSqlFromUrl(sql, url);

        _ = result.Should().Be("1 2 3");
    }

    #endregion
}
