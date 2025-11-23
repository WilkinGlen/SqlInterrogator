using FluentAssertions;
using SqlInterrogatorService;

namespace SqlInterrogatorServiceTest;

/// <summary>
/// Tests for combinations of convert methods to ensure they work correctly together.
/// Tests scenarios where multiple transformations are applied sequentially.
/// </summary>
public class ConvertMethodCombinations_Should
{
    #region COUNT + TOP Combinations

    [Fact]
    public void ConvertToCount_ThenTop_WhenSimpleQuery()
    {
        var sql = "SELECT Name, Email FROM Users WHERE Active = 1";

        // First convert to COUNT
        var countSql = SqlInterrogator.ConvertSelectStatementToSelectCount(sql);
        _ = countSql.Should().Be("SELECT COUNT(*) FROM Users WHERE Active = 1");

        // Then convert the COUNT query to TOP
        var topSql = SqlInterrogator.ConvertSelectStatementToSelectTop(countSql!, 10);
        _ = topSql.Should().Be("SELECT TOP 10 COUNT(*) FROM Users WHERE Active = 1");
    }

    [Fact]
    public void ConvertToTop_ThenCount_WhenSimpleQuery()
    {
        var sql = "SELECT Name, Email FROM Users WHERE Active = 1";

        // First convert to TOP
        var topSql = SqlInterrogator.ConvertSelectStatementToSelectTop(sql, 10);
        _ = topSql.Should().Be("SELECT TOP 10 Name, Email FROM Users WHERE Active = 1");

        // Then convert to COUNT
        var countSql = SqlInterrogator.ConvertSelectStatementToSelectCount(topSql!);
        _ = countSql.Should().Be("SELECT COUNT(*) FROM Users WHERE Active = 1");
    }

    [Fact]
    public void ConvertToCount_ThenTop_WhenDistinctQuery()
    {
        var sql = "SELECT DISTINCT Name, Email FROM Users WHERE Active = 1";

        // First convert to COUNT (uses subquery for DISTINCT)
        var countSql = SqlInterrogator.ConvertSelectStatementToSelectCount(sql);
        _ = countSql.Should().Be("SELECT COUNT(*) FROM (SELECT DISTINCT Name, Email FROM Users WHERE Active = 1) AS DistinctCount");

        // Then convert to TOP
        // The method detects DISTINCT in the original (inner) query and preserves it
        var topSql = SqlInterrogator.ConvertSelectStatementToSelectTop(countSql!, 1);
        _ = topSql.Should().Be("SELECT DISTINCT TOP 1 COUNT(*) FROM (SELECT DISTINCT Name, Email FROM Users WHERE Active = 1) AS DistinctCount");
    }

    [Fact]
    public void ConvertToTop_ThenCount_WhenDistinctQuery()
    {
        var sql = "SELECT DISTINCT Name, Email FROM Users WHERE Active = 1";

        // First convert to TOP (preserves DISTINCT)
        var topSql = SqlInterrogator.ConvertSelectStatementToSelectTop(sql, 10);
        _ = topSql.Should().Be("SELECT DISTINCT TOP 10 Name, Email FROM Users WHERE Active = 1");

        // Then convert to COUNT (should use subquery due to DISTINCT)
        var countSql = SqlInterrogator.ConvertSelectStatementToSelectCount(topSql!);
        _ = countSql.Should().Be("SELECT COUNT(*) FROM (SELECT DISTINCT TOP 10 Name, Email FROM Users WHERE Active = 1) AS DistinctCount");
    }

    #endregion

    #region COUNT + DISTINCT Combinations

    [Fact]
    public void ConvertToCount_ThenDistinct_WhenSimpleQuery()
    {
        var sql = "SELECT Name, Email FROM Users WHERE Active = 1";

        // First convert to COUNT
        var countSql = SqlInterrogator.ConvertSelectStatementToSelectCount(sql);
        _ = countSql.Should().Be("SELECT COUNT(*) FROM Users WHERE Active = 1");

        // Then convert to DISTINCT (should add DISTINCT to COUNT)
        var distinctSql = SqlInterrogator.ConvertSelectStatementToSelectDistinct(countSql!);
        _ = distinctSql.Should().Be("SELECT DISTINCT COUNT(*) FROM Users WHERE Active = 1");
    }

    [Fact]
    public void ConvertToDistinct_ThenCount_WhenSimpleQuery()
    {
        var sql = "SELECT Name, Email FROM Users WHERE Active = 1";

        // First convert to DISTINCT
        var distinctSql = SqlInterrogator.ConvertSelectStatementToSelectDistinct(sql);
        _ = distinctSql.Should().Be("SELECT DISTINCT Name, Email FROM Users WHERE Active = 1");

        // Then convert to COUNT (should use subquery)
        var countSql = SqlInterrogator.ConvertSelectStatementToSelectCount(distinctSql!);
        _ = countSql.Should().Be("SELECT COUNT(*) FROM (SELECT DISTINCT Name, Email FROM Users WHERE Active = 1) AS DistinctCount");
    }

    [Fact]
    public void ConvertToDistinct_ThenCount_IsIdempotent()
    {
        var sql = "SELECT DISTINCT Name FROM Users";

        // First conversion
        var countSql1 = SqlInterrogator.ConvertSelectStatementToSelectCount(sql);

        // Second conversion (already DISTINCT)
        var countSql2 = SqlInterrogator.ConvertSelectStatementToSelectCount(sql);

        _ = countSql1.Should().Be(countSql2);
        _ = countSql1.Should().Be("SELECT COUNT(*) FROM (SELECT DISTINCT Name FROM Users) AS DistinctCount");
    }

    #endregion

    #region TOP + DISTINCT Combinations

    [Fact]
    public void ConvertToTop_ThenDistinct_WhenSimpleQuery()
    {
        var sql = "SELECT Name, Email FROM Users WHERE Active = 1";

        // First convert to TOP
        var topSql = SqlInterrogator.ConvertSelectStatementToSelectTop(sql, 10);
        _ = topSql.Should().Be("SELECT TOP 10 Name, Email FROM Users WHERE Active = 1");

        // Then convert to DISTINCT (adds DISTINCT to TOP)
        var distinctSql = SqlInterrogator.ConvertSelectStatementToSelectDistinct(topSql!);
        _ = distinctSql.Should().Be("SELECT DISTINCT TOP 10 Name, Email FROM Users WHERE Active = 1");
    }

    [Fact]
    public void ConvertToDistinct_ThenTop_WhenSimpleQuery()
    {
        var sql = "SELECT Name, Email FROM Users WHERE Active = 1";

        // First convert to DISTINCT
        var distinctSql = SqlInterrogator.ConvertSelectStatementToSelectDistinct(sql);
        _ = distinctSql.Should().Be("SELECT DISTINCT Name, Email FROM Users WHERE Active = 1");

        // Then convert to TOP (preserves DISTINCT)
        var topSql = SqlInterrogator.ConvertSelectStatementToSelectTop(distinctSql!, 10);
        _ = topSql.Should().Be("SELECT DISTINCT TOP 10 Name, Email FROM Users WHERE Active = 1");
    }

    [Fact]
    public void ConvertToDistinct_ThenTop_PreservesDistinct()
    {
        var sql = "SELECT Name FROM Users";

        var distinctSql = SqlInterrogator.ConvertSelectStatementToSelectDistinct(sql);
        var topSql = SqlInterrogator.ConvertSelectStatementToSelectTop(distinctSql!, 5);

        _ = topSql.Should().Contain("DISTINCT");
        _ = topSql.Should().Contain("TOP 5");
        _ = topSql.Should().Be("SELECT DISTINCT TOP 5 Name FROM Users");
    }

    #endregion

    #region ORDER BY Combinations

    [Fact]
    public void ConvertToOrderBy_ThenCount_WhenSimpleQuery()
    {
        var sql = "SELECT Name, Email FROM Users WHERE Active = 1";

        // First add ORDER BY
        var orderedSql = SqlInterrogator.ConvertSelectStatementToSelectOrderBy(sql, "Name ASC");
        _ = orderedSql.Should().Be("SELECT Name, Email FROM Users WHERE Active = 1 ORDER BY Name ASC");

        // Then convert to COUNT (ORDER BY preserved but has no effect)
        var countSql = SqlInterrogator.ConvertSelectStatementToSelectCount(orderedSql!);
        _ = countSql.Should().Be("SELECT COUNT(*) FROM Users WHERE Active = 1 ORDER BY Name ASC");
    }

    [Fact]
    public void ConvertToOrderBy_ThenTop_WhenSimpleQuery()
    {
        var sql = "SELECT Name, Email FROM Users WHERE Active = 1";

        // First add ORDER BY
        var orderedSql = SqlInterrogator.ConvertSelectStatementToSelectOrderBy(sql, "Name ASC");
        _ = orderedSql.Should().Be("SELECT Name, Email FROM Users WHERE Active = 1 ORDER BY Name ASC");

        // Then convert to TOP (ORDER BY preserved)
        var topSql = SqlInterrogator.ConvertSelectStatementToSelectTop(orderedSql!, 10);
        _ = topSql.Should().Be("SELECT TOP 10 Name, Email FROM Users WHERE Active = 1 ORDER BY Name ASC");
    }

    [Fact]
    public void ConvertToTop_ThenOrderBy_ReplacesOrderBy()
    {
        var sql = "SELECT Name, Email FROM Users ORDER BY Email DESC";

        // First convert to TOP
        var topSql = SqlInterrogator.ConvertSelectStatementToSelectTop(sql, 10);
        _ = topSql.Should().Be("SELECT TOP 10 Name, Email FROM Users ORDER BY Email DESC");

        // Then change ORDER BY (should replace existing)
        var orderedSql = SqlInterrogator.ConvertSelectStatementToSelectOrderBy(topSql!, "Name ASC");
        _ = orderedSql.Should().Be("SELECT TOP 10 Name, Email FROM Users ORDER BY Name ASC");
        _ = orderedSql.Should().NotContain("Email DESC");
    }

    [Fact]
    public void ConvertToOrderBy_ThenDistinct_WhenSimpleQuery()
    {
        var sql = "SELECT Name, Email FROM Users WHERE Active = 1";

        // First add ORDER BY
        var orderedSql = SqlInterrogator.ConvertSelectStatementToSelectOrderBy(sql, "Name ASC");
        _ = orderedSql.Should().Be("SELECT Name, Email FROM Users WHERE Active = 1 ORDER BY Name ASC");

        // Then convert to DISTINCT (ORDER BY preserved)
        var distinctSql = SqlInterrogator.ConvertSelectStatementToSelectDistinct(orderedSql!);
        _ = distinctSql.Should().Be("SELECT DISTINCT Name, Email FROM Users WHERE Active = 1 ORDER BY Name ASC");
    }

    #endregion

    #region Triple Combinations

    [Fact]
    public void ConvertToDistinct_ThenTop_ThenCount()
    {
        var sql = "SELECT Name, Email FROM Users WHERE Active = 1";

        // Step 1: DISTINCT
        var distinctSql = SqlInterrogator.ConvertSelectStatementToSelectDistinct(sql);
        _ = distinctSql.Should().Be("SELECT DISTINCT Name, Email FROM Users WHERE Active = 1");

        // Step 2: TOP (preserves DISTINCT)
        var topSql = SqlInterrogator.ConvertSelectStatementToSelectTop(distinctSql!, 10);
        _ = topSql.Should().Be("SELECT DISTINCT TOP 10 Name, Email FROM Users WHERE Active = 1");

        // Step 3: COUNT (uses subquery due to DISTINCT)
        var countSql = SqlInterrogator.ConvertSelectStatementToSelectCount(topSql!);
        _ = countSql.Should().Be("SELECT COUNT(*) FROM (SELECT DISTINCT TOP 10 Name, Email FROM Users WHERE Active = 1) AS DistinctCount");
    }

    [Fact]
    public void ConvertToTop_ThenDistinct_ThenOrderBy()
    {
        var sql = "SELECT Name, Email FROM Users WHERE Active = 1";

        // Step 1: TOP
        var topSql = SqlInterrogator.ConvertSelectStatementToSelectTop(sql, 10);
        _ = topSql.Should().Be("SELECT TOP 10 Name, Email FROM Users WHERE Active = 1");

        // Step 2: DISTINCT
        var distinctSql = SqlInterrogator.ConvertSelectStatementToSelectDistinct(topSql!);
        _ = distinctSql.Should().Be("SELECT DISTINCT TOP 10 Name, Email FROM Users WHERE Active = 1");

        // Step 3: ORDER BY
        var orderedSql = SqlInterrogator.ConvertSelectStatementToSelectOrderBy(distinctSql!, "Name ASC");
        _ = orderedSql.Should().Be("SELECT DISTINCT TOP 10 Name, Email FROM Users WHERE Active = 1 ORDER BY Name ASC");
    }

    [Fact]
    public void ConvertToOrderBy_ThenDistinct_ThenTop_ThenCount()
    {
        var sql = "SELECT Name, Email FROM Users WHERE Active = 1";

        // Step 1: ORDER BY
        var orderedSql = SqlInterrogator.ConvertSelectStatementToSelectOrderBy(sql, "Name ASC");
        _ = orderedSql.Should().Be("SELECT Name, Email FROM Users WHERE Active = 1 ORDER BY Name ASC");

        // Step 2: DISTINCT
        var distinctSql = SqlInterrogator.ConvertSelectStatementToSelectDistinct(orderedSql!);
        _ = distinctSql.Should().Be("SELECT DISTINCT Name, Email FROM Users WHERE Active = 1 ORDER BY Name ASC");

        // Step 3: TOP
        var topSql = SqlInterrogator.ConvertSelectStatementToSelectTop(distinctSql!, 10);
        _ = topSql.Should().Be("SELECT DISTINCT TOP 10 Name, Email FROM Users WHERE Active = 1 ORDER BY Name ASC");

        // Step 4: COUNT
        var countSql = SqlInterrogator.ConvertSelectStatementToSelectCount(topSql!);
        _ = countSql.Should().Be("SELECT COUNT(*) FROM (SELECT DISTINCT TOP 10 Name, Email FROM Users WHERE Active = 1 ORDER BY Name ASC) AS DistinctCount");
    }

    #endregion

    #region Complex Query Combinations

    [Fact]
    public void ConvertComplexJoinQuery_ThroughMultipleTransformations()
    {
        var sql = @"SELECT u.Name, u.Email, o.OrderDate 
                    FROM Users u 
                    INNER JOIN Orders o ON u.Id = o.UserId 
                    WHERE u.Active = 1 AND o.Status = 'Completed'";

        // Add ORDER BY
        var orderedSql = SqlInterrogator.ConvertSelectStatementToSelectOrderBy(sql, "o.OrderDate DESC");
        _ = orderedSql.Should().Contain("ORDER BY o.OrderDate DESC");
        _ = orderedSql.Should().Contain("INNER JOIN");

        // Add DISTINCT
        var distinctSql = SqlInterrogator.ConvertSelectStatementToSelectDistinct(orderedSql!);
        _ = distinctSql.Should().StartWith("SELECT DISTINCT");
        _ = distinctSql.Should().Contain("INNER JOIN");
        _ = distinctSql.Should().Contain("ORDER BY o.OrderDate DESC");

        // Add TOP
        var topSql = SqlInterrogator.ConvertSelectStatementToSelectTop(distinctSql!, 20);
        _ = topSql.Should().Contain("SELECT DISTINCT TOP 20 u.Name, u.Email, o.OrderDate FROM Users u");
        _ = topSql.Should().Contain("INNER JOIN");
        _ = topSql.Should().Contain("ORDER BY o.OrderDate DESC");

        // Convert to COUNT
        var countSql = SqlInterrogator.ConvertSelectStatementToSelectCount(topSql!);
        _ = countSql.Should().StartWith("SELECT COUNT(*) FROM (SELECT DISTINCT TOP 20 u.Name, u.Email, o.OrderDate FROM Users u");
        _ = countSql.Should().Contain("INNER JOIN");
        _ = countSql.Should().EndWith(") AS DistinctCount");
    }

    [Fact]
    public void ConvertGroupByQuery_ThroughMultipleTransformations()
    {
        var sql = @"SELECT Category, COUNT(*) AS Total 
                    FROM Products 
                    WHERE Active = 1 
                    GROUP BY Category 
                    HAVING COUNT(*) > 5";

        // Add ORDER BY
        var orderedSql = SqlInterrogator.ConvertSelectStatementToSelectOrderBy(sql, "Total DESC");
        _ = orderedSql.Should().Contain("GROUP BY Category");
        _ = orderedSql.Should().Contain("HAVING COUNT(*) > 5");
        _ = orderedSql.Should().Contain("ORDER BY Total DESC");

        // Add TOP
        var topSql = SqlInterrogator.ConvertSelectStatementToSelectTop(orderedSql!, 10);
        _ = topSql.Should().StartWith("SELECT TOP 10 Category, COUNT(*) AS Total FROM");
        _ = topSql.Should().Contain("GROUP BY Category");
        _ = topSql.Should().Contain("ORDER BY Total DESC");

        // Add DISTINCT
        var distinctSql = SqlInterrogator.ConvertSelectStatementToSelectDistinct(topSql!);
        _ = distinctSql.Should().StartWith("SELECT DISTINCT TOP 10 Category, COUNT(*) AS Total FROM");
        _ = distinctSql.Should().Contain("GROUP BY Category");
    }

    [Fact]
    public void ConvertPaginationQuery_ThroughMultipleTransformations()
    {
        var sql = @"SELECT * FROM Users 
                    WHERE Active = 1 
                    ORDER BY CreatedDate DESC 
                    OFFSET 20 ROWS 
                    FETCH NEXT 10 ROWS ONLY";

        // Change ORDER BY (preserves pagination)
        var orderedSql = SqlInterrogator.ConvertSelectStatementToSelectOrderBy(sql, "Name ASC");
        _ = orderedSql.Should().Contain("ORDER BY Name ASC");
        _ = orderedSql.Should().Contain("OFFSET 20 ROWS");
        _ = orderedSql.Should().Contain("FETCH NEXT 10 ROWS ONLY");
        _ = orderedSql.Should().NotContain("CreatedDate DESC");

        // Add DISTINCT
        var distinctSql = SqlInterrogator.ConvertSelectStatementToSelectDistinct(orderedSql!);
        _ = distinctSql.Should().StartWith("SELECT DISTINCT");
        _ = distinctSql.Should().Contain("OFFSET 20 ROWS");

        // Add TOP (may conflict with OFFSET/FETCH but should preserve both)
        var topSql = SqlInterrogator.ConvertSelectStatementToSelectTop(distinctSql!, 5);
        _ = topSql.Should().Contain("SELECT DISTINCT TOP 5 * FROM");
        _ = topSql.Should().Contain("OFFSET 20 ROWS");
    }
    #endregion

    #region Edge Cases and Idempotency

    [Fact]
    public void ConvertToSameType_IsIdempotent_Top()
    {
        var sql = "SELECT * FROM Users";

        var top1 = SqlInterrogator.ConvertSelectStatementToSelectTop(sql, 10);
        var top2 = SqlInterrogator.ConvertSelectStatementToSelectTop(top1!, 10);

        _ = top1.Should().Be(top2);
        _ = top1.Should().Be("SELECT TOP 10 * FROM Users");
    }

    [Fact]
    public void ConvertToSameType_ReplacesValue_Top()
    {
        var sql = "SELECT TOP 100 * FROM Users";

        var top50 = SqlInterrogator.ConvertSelectStatementToSelectTop(sql, 50);
        var top10 = SqlInterrogator.ConvertSelectStatementToSelectTop(top50!, 10);

        _ = top50.Should().Be("SELECT TOP 50 * FROM Users");
        _ = top10.Should().Be("SELECT TOP 10 * FROM Users");
        _ = top10.Should().NotContain("100");
        _ = top10.Should().NotContain("50");
    }

    [Fact]
    public void ConvertToSameType_IsIdempotent_Distinct()
    {
        var sql = "SELECT Name FROM Users";

        var distinct1 = SqlInterrogator.ConvertSelectStatementToSelectDistinct(sql);
        var distinct2 = SqlInterrogator.ConvertSelectStatementToSelectDistinct(distinct1!);

        _ = distinct1.Should().Be(distinct2);
        _ = distinct1.Should().Be("SELECT DISTINCT Name FROM Users");
    }

    [Fact]
    public void ConvertToSameType_ReplacesClause_OrderBy()
    {
        var sql = "SELECT * FROM Users ORDER BY Name ASC";

        var order1 = SqlInterrogator.ConvertSelectStatementToSelectOrderBy(sql, "Email DESC");
        var order2 = SqlInterrogator.ConvertSelectStatementToSelectOrderBy(order1!, "CreatedDate ASC");

        _ = order1.Should().Be("SELECT * FROM Users ORDER BY Email DESC");
        _ = order2.Should().Be("SELECT * FROM Users ORDER BY CreatedDate ASC");
        _ = order2.Should().NotContain("Name");
        _ = order2.Should().NotContain("Email");
    }

    [Fact]
    public void ConvertChain_WithAllMethods_PreservesAllFeatures()
    {
        var sql = "SELECT Name, Email FROM Users WHERE Active = 1";

        // Apply all transformations
        var result = sql;
        
        result = SqlInterrogator.ConvertSelectStatementToSelectDistinct(result)!;
        _ = result.Should().StartWith("SELECT DISTINCT");

        result = SqlInterrogator.ConvertSelectStatementToSelectTop(result, 25)!;
        _ = result.Should().Contain("SELECT DISTINCT TOP 25 Name, Email FROM");

        result = SqlInterrogator.ConvertSelectStatementToSelectOrderBy(result, "Name ASC")!;
        _ = result.Should().Contain("SELECT DISTINCT TOP 25 Name, Email FROM");
        _ = result.Should().EndWith("ORDER BY Name ASC");

        // Verify final result
        _ = result.Should().Be("SELECT DISTINCT TOP 25 Name, Email FROM Users WHERE Active = 1 ORDER BY Name ASC");
    }

    #endregion

    #region Null Handling in Combinations

    [Fact]
    public void CombinedConversions_HandleNullGracefully()
    {
        string? sql = null;

        var countSql = SqlInterrogator.ConvertSelectStatementToSelectCount(sql!);
        _ = countSql.Should().BeNull();

        var topSql = SqlInterrogator.ConvertSelectStatementToSelectTop(sql!, 10);
        _ = topSql.Should().BeNull();

        var distinctSql = SqlInterrogator.ConvertSelectStatementToSelectDistinct(sql!);
        _ = distinctSql.Should().BeNull();

        var orderedSql = SqlInterrogator.ConvertSelectStatementToSelectOrderBy(sql!, "Name ASC");
        _ = orderedSql.Should().BeNull();
    }

    [Fact]
    public void CombinedConversions_PropagateNullCorrectly()
    {
        var sql = "UPDATE Users SET Active = 1"; // Invalid for SELECT conversions

        var topSql = SqlInterrogator.ConvertSelectStatementToSelectTop(sql, 10);
        _ = topSql.Should().BeNull();

        // Null should propagate through chain
        var distinctSql = SqlInterrogator.ConvertSelectStatementToSelectDistinct(topSql!);
        _ = distinctSql.Should().BeNull();

        var countSql = SqlInterrogator.ConvertSelectStatementToSelectCount(distinctSql!);
        _ = countSql.Should().BeNull();
    }

    #endregion

    #region Real-World Scenarios

    [Fact]
    public void RealWorld_PaginationWithDynamicSorting()
    {
        var baseQuery = "SELECT u.Name, u.Email, u.Status FROM Users u WHERE u.Active = 1";

        // User selects different sort column
        var sorted = SqlInterrogator.ConvertSelectStatementToSelectOrderBy(baseQuery, "u.Name ASC")!;

        // Apply pagination
        sorted = $"{sorted} OFFSET 0 ROWS FETCH NEXT 10 ROWS ONLY";

        // User changes sort again (pagination should be preserved)
        var resorted = SqlInterrogator.ConvertSelectStatementToSelectOrderBy(sorted, "u.Email DESC")!;
        
        _ = resorted.Should().Contain("ORDER BY u.Email DESC");
        _ = resorted.Should().Contain("OFFSET 0 ROWS");
        _ = resorted.Should().Contain("FETCH NEXT 10 ROWS ONLY");
        _ = resorted.Should().NotContain("u.Name ASC");
    }

    [Fact]
    public void RealWorld_GetCountForDistinctTopQuery()
    {
        var sql = "SELECT DISTINCT u.Department, u.Role FROM Users u WHERE u.Active = 1";

        // Limit to top 50 distinct combinations
        var topSql = SqlInterrogator.ConvertSelectStatementToSelectTop(sql, 50)!;
        _ = topSql.Should().Be("SELECT DISTINCT TOP 50 u.Department, u.Role FROM Users u WHERE u.Active = 1");

        // Get count of those top 50 distinct combinations
        var countSql = SqlInterrogator.ConvertSelectStatementToSelectCount(topSql)!;
        _ = countSql.Should().Contain("SELECT COUNT(*) FROM (SELECT DISTINCT TOP 50 u.Department, u.Role FROM Users u");
        _ = countSql.Should().EndWith(") AS DistinctCount");
    }

    [Fact]
    public void RealWorld_DataSamplingWorkflow()
    {
        var sql = "SELECT ProductName, Category, Price FROM Products WHERE Discontinued = 0";

        // Get unique products (remove duplicates)
        var distinctSql = SqlInterrogator.ConvertSelectStatementToSelectDistinct(sql)!;

        // Sort by category
        var sortedSql = SqlInterrogator.ConvertSelectStatementToSelectOrderBy(distinctSql, "Category ASC, Price DESC")!;

        // Take sample of 100
        var sampleSql = SqlInterrogator.ConvertSelectStatementToSelectTop(sortedSql, 100)!;

        // Final verification
        _ = sampleSql.Should().Contain("SELECT DISTINCT TOP 100 ProductName, Category, Price FROM");
        _ = sampleSql.Should().Contain("WHERE Discontinued = 0");
        _ = sampleSql.Should().Contain("ORDER BY Category ASC, Price DESC");
    }

    [Fact]
    public void RealWorld_ReportWithMultipleTransformations()
    {
        var sql = @"SELECT c.Country, c.City, COUNT(*) AS CustomerCount
                    FROM Customers c
                    WHERE c.Active = 1
                    GROUP BY c.Country, c.City
                    HAVING COUNT(*) >= 5";

        // Sort by customer count
        var sorted = SqlInterrogator.ConvertSelectStatementToSelectOrderBy(sql, "CustomerCount DESC")!;

        // Get top 20 cities
        var top20 = SqlInterrogator.ConvertSelectStatementToSelectTop(sorted, 20)!;

        // Add DISTINCT (though redundant with GROUP BY, user might want it)
        var distinct = SqlInterrogator.ConvertSelectStatementToSelectDistinct(top20)!;

        // Get total count for report
        var count = SqlInterrogator.ConvertSelectStatementToSelectCount(distinct)!;

        _ = count.Should().Contain("SELECT COUNT(*) FROM (SELECT DISTINCT TOP 20 c.Country, c.City, COUNT(*) AS CustomerCount FROM");
        _ = count.Should().Contain("GROUP BY c.Country, c.City");
        _ = count.Should().Contain("HAVING COUNT(*) >= 5");
        _ = count.Should().Contain("ORDER BY CustomerCount DESC");
    }

    #endregion

    #region Performance and Optimization Scenarios

    [Fact]
    public void Optimization_TopBeforeDistinct_VsDistinctBeforeTop()
    {
        var sql = "SELECT Name, Email FROM Users WHERE Active = 1";

        // Scenario A: TOP then DISTINCT
        var topFirst = SqlInterrogator.ConvertSelectStatementToSelectTop(sql, 100)!;
        var topThenDistinct = SqlInterrogator.ConvertSelectStatementToSelectDistinct(topFirst)!;

        // Scenario B: DISTINCT then TOP
        var distinctFirst = SqlInterrogator.ConvertSelectStatementToSelectDistinct(sql)!;
        var distinctThenTop = SqlInterrogator.ConvertSelectStatementToSelectTop(distinctFirst, 100)!;

        // Both should produce: SELECT DISTINCT TOP 100 Name, Email
        _ = topThenDistinct.Should().Contain("SELECT DISTINCT TOP 100 Name, Email FROM");
        _ = distinctThenTop.Should().Contain("SELECT DISTINCT TOP 100 Name, Email FROM");
        
        // Results should be identical
        _ = topThenDistinct.Should().Be(distinctThenTop);
    }

    [Fact]
    public void Optimization_CountQuery_RemovesUnnecessaryClauses()
    {
        var sql = @"SELECT u.Name, u.Email, COUNT(o.Id) AS OrderCount
                    FROM Users u
                    LEFT JOIN Orders o ON u.Id = o.UserId
                    WHERE u.Active = 1
                    GROUP BY u.Name, u.Email
                    HAVING COUNT(o.Id) > 0
                    ORDER BY COUNT(o.Id) DESC";

        // Convert to count (ORDER BY becomes irrelevant but is preserved)
        var countSql = SqlInterrogator.ConvertSelectStatementToSelectCount(sql)!;

        _ = countSql.Should().StartWith("SELECT COUNT(*)");
        _ = countSql.Should().Contain("GROUP BY u.Name, u.Email");
        _ = countSql.Should().Contain("HAVING COUNT(o.Id) > 0");
        
        // ORDER BY is preserved even though it has no effect on COUNT
        _ = countSql.Should().Contain("ORDER BY COUNT(o.Id) DESC");
    }

    #endregion
}
