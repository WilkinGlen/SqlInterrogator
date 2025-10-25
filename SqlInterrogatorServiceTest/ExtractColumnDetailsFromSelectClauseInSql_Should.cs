namespace SqlInterrogatorServiceTest;

using FluentAssertions;

public class ExtractColumnDetailsFromSelectClauseInSql_Should
{
    [Fact]
    public void ReturnStar_WhenSelectStar()
    {
        var sql = "SELECT * FROM Users";
        var result = SqlInterrogatorService.SqlInterrogator.ExtractColumnDetailsFromSelectClauseInSql(sql);

        _ = result.Should().ContainSingle();
        _ = result[0].ColumnName.Should().Be("*");
        _ = result[0].DatabaseName.Should().BeNull();
        _ = result[0].TableName.Should().BeNull();
    }

    [Fact]
    public void ReturnSimpleColumnName_WhenUnqualified()
    {
        var sql = "SELECT Name FROM Users";
        var result = SqlInterrogatorService.SqlInterrogator.ExtractColumnDetailsFromSelectClauseInSql(sql);

        _ = result.Should().ContainSingle();
        _ = result[0].ColumnName.Should().Be("Name");
        _ = result[0].DatabaseName.Should().BeNull();
        _ = result[0].TableName.Should().BeNull();
    }

    [Fact]
    public void ReturnMultipleColumns_WhenCommaSeparated()
    {
        var sql = "SELECT Name, Email, Age FROM Users";
        var result = SqlInterrogatorService.SqlInterrogator.ExtractColumnDetailsFromSelectClauseInSql(sql);

        _ = result.Should().HaveCount(3);
        _ = result[0].ColumnName.Should().Be("Name");
        _ = result[1].ColumnName.Should().Be("Email");
        _ = result[2].ColumnName.Should().Be("Age");
    }

    [Fact]
    public void ReturnTableQualifiedColumn_WhenTwoPartIdentifier()
    {
        var sql = "SELECT Users.Name FROM Users";
        var result = SqlInterrogatorService.SqlInterrogator.ExtractColumnDetailsFromSelectClauseInSql(sql);

        _ = result.Should().ContainSingle();
        _ = result[0].TableName.Should().Be("Users");
        _ = result[0].ColumnName.Should().Be("Name");
        _ = result[0].DatabaseName.Should().BeNull();
    }

    [Fact]
    public void ReturnBracketedTableQualifiedColumn()
    {
        var sql = "SELECT [Users].[Name] FROM [Users]";
        var result = SqlInterrogatorService.SqlInterrogator.ExtractColumnDetailsFromSelectClauseInSql(sql);

        _ = result.Should().ContainSingle();
        _ = result[0].TableName.Should().Be("Users");
        _ = result[0].ColumnName.Should().Be("Name");
    }

    [Fact]
    public void ReturnDatabaseAndTableQualifiedColumn_WhenThreePartIdentifier()
    {
        var sql = "SELECT MyDB.Users.Name FROM MyDB.Users";
        var result = SqlInterrogatorService.SqlInterrogator.ExtractColumnDetailsFromSelectClauseInSql(sql);

        _ = result.Should().ContainSingle();
        _ = result[0].DatabaseName.Should().Be("MyDB");
        _ = result[0].TableName.Should().Be("Users");
        _ = result[0].ColumnName.Should().Be("Name");
    }

    [Fact]
    public void ReturnFullyQualifiedColumn_WhenFourPartIdentifier()
    {
        var sql = "SELECT [Server1].[MyDB].[dbo].[Users].[Name] FROM [Server1].[MyDB].[dbo].[Users]";
        var result = SqlInterrogatorService.SqlInterrogator.ExtractColumnDetailsFromSelectClauseInSql(sql);

        _ = result.Should().ContainSingle();
        _ = result[0].DatabaseName.Should().Be("Server1");
        _ = result[0].TableName.Should().Be("Users");
        _ = result[0].ColumnName.Should().Be("Name");
    }

    [Fact]
    public void ReturnAliasName_WhenColumnHasAlias()
    {
        var sql = "SELECT Name AS FullName FROM Users";
        var result = SqlInterrogatorService.SqlInterrogator.ExtractColumnDetailsFromSelectClauseInSql(sql);

        _ = result.Should().ContainSingle();
        _ = result[0].ColumnName.Should().Be("FullName");
    }

    [Fact]
    public void ReturnAliasName_WhenColumnHasBracketedAlias()
    {
        var sql = "SELECT Name AS [Full Name] FROM Users";
        var result = SqlInterrogatorService.SqlInterrogator.ExtractColumnDetailsFromSelectClauseInSql(sql);

        _ = result.Should().ContainSingle();
        _ = result[0].ColumnName.Should().Be("Full Name");
    }

    [Fact]
    public void ReturnAliasName_WhenImplicitAlias()
    {
        var sql = "SELECT u.Name UserName FROM Users u";
        var result = SqlInterrogatorService.SqlInterrogator.ExtractColumnDetailsFromSelectClauseInSql(sql);

        _ = result.Should().ContainSingle();
        _ = result[0].ColumnName.Should().Be("UserName");
        _ = result[0].TableName.Should().Be("u");
    }

    [Fact]
    public void ReturnFunctionName_WhenUsingAggregateFunction()
    {
        var sql = "SELECT COUNT(*) FROM Users";
        var result = SqlInterrogatorService.SqlInterrogator.ExtractColumnDetailsFromSelectClauseInSql(sql);

        _ = result.Should().ContainSingle();
        _ = result[0].ColumnName.Should().Be("COUNT");
    }

    [Fact]
    public void ReturnAliasForFunction_WhenFunctionHasAlias()
    {
        var sql = "SELECT COUNT(*) AS TotalUsers FROM Users";
        var result = SqlInterrogatorService.SqlInterrogator.ExtractColumnDetailsFromSelectClauseInSql(sql);

        _ = result.Should().ContainSingle();
        _ = result[0].ColumnName.Should().Be("TotalUsers");
    }

    [Fact]
    public void ReturnMultipleFunctions()
    {
        var sql = "SELECT COUNT(*) AS Total, MAX(Age) AS MaxAge, MIN(Age) AS MinAge FROM Users";
        var result = SqlInterrogatorService.SqlInterrogator.ExtractColumnDetailsFromSelectClauseInSql(sql);

        _ = result.Should().HaveCount(3);
        _ = result[0].ColumnName.Should().Be("Total");
        _ = result[1].ColumnName.Should().Be("MaxAge");
        _ = result[2].ColumnName.Should().Be("MinAge");
    }

    [Fact]
    public void HandleMixedColumnsAndFunctions()
    {
        var sql = "SELECT u.Name, u.Email, COUNT(o.OrderId) AS OrderCount FROM Users u JOIN Orders o ON u.Id = o.UserId";
        var result = SqlInterrogatorService.SqlInterrogator.ExtractColumnDetailsFromSelectClauseInSql(sql);

        _ = result.Should().HaveCount(3);
        _ = result[0].ColumnName.Should().Be("Name");
        _ = result[0].TableName.Should().Be("u");
        _ = result[1].ColumnName.Should().Be("Email");
        _ = result[1].TableName.Should().Be("u");
        _ = result[2].ColumnName.Should().Be("OrderCount");
    }

    [Fact]
    public void HandleComplexFunctionWithMultipleParameters()
    {
        var sql = "SELECT CONCAT(FirstName, ' ', LastName) AS FullName FROM Users";
        var result = SqlInterrogatorService.SqlInterrogator.ExtractColumnDetailsFromSelectClauseInSql(sql);

        _ = result.Should().ContainSingle();
        _ = result[0].ColumnName.Should().Be("FullName");
    }

    [Fact]
    public void ReturnEmptyList_WhenNoSelectClause()
    {
        var sql = "UPDATE Users SET Active = 1";
        var result = SqlInterrogatorService.SqlInterrogator.ExtractColumnDetailsFromSelectClauseInSql(sql);

        _ = result.Should().BeEmpty();
    }

    [Fact]
    public void ReturnEmptyList_WhenNullSql()
    {
        var result = SqlInterrogatorService.SqlInterrogator.ExtractColumnDetailsFromSelectClauseInSql(null!);

        _ = result.Should().BeEmpty();
    }

    [Fact]
    public void ReturnEmptyList_WhenEmptySql()
    {
        var result = SqlInterrogatorService.SqlInterrogator.ExtractColumnDetailsFromSelectClauseInSql("");

        _ = result.Should().BeEmpty();
    }

    [Fact]
    public void HandleMultilineQuery()
    {
        var sql = @"
                    SELECT 
                        u.Name,
                        u.Email,
                        u.Age
                    FROM 
                        Users u";

        var result = SqlInterrogatorService.SqlInterrogator.ExtractColumnDetailsFromSelectClauseInSql(sql);

        _ = result.Should().HaveCount(3);
        _ = result[0].ColumnName.Should().Be("Name");
        _ = result[1].ColumnName.Should().Be("Email");
        _ = result[2].ColumnName.Should().Be("Age");
    }

    [Fact]
    public void IgnoreCommentsInSelectClause()
    {
        var sql = @"
            SELECT 
                Name, -- This is the user name
                Email /* User email address */
            FROM Users";

        var result = SqlInterrogatorService.SqlInterrogator.ExtractColumnDetailsFromSelectClauseInSql(sql);

        _ = result.Should().HaveCount(2);
        _ = result[0].ColumnName.Should().Be("Name");
        _ = result[1].ColumnName.Should().Be("Email");
    }

    [Fact]
    public void HandleDoubleQuotedColumns()
    {
        var sql = "SELECT \"Name\", \"Email\" FROM \"Users\"";
        var result = SqlInterrogatorService.SqlInterrogator.ExtractColumnDetailsFromSelectClauseInSql(sql);

        _ = result.Should().HaveCount(2);
        _ = result[0].ColumnName.Should().Be("Name");
        _ = result[1].ColumnName.Should().Be("Email");
    }

    [Fact]
    public void HandleColumnNamesWithUnderscores()
    {
        var sql = "SELECT First_Name, Last_Name FROM Users";
        var result = SqlInterrogatorService.SqlInterrogator.ExtractColumnDetailsFromSelectClauseInSql(sql);

        _ = result.Should().HaveCount(2);
        _ = result[0].ColumnName.Should().Be("First_Name");
        _ = result[1].ColumnName.Should().Be("Last_Name");
    }

    [Fact]
    public void HandleUseStatement()
    {
        var sql = @"USE MyDatabase;
                    SELECT Name, Email FROM Users";

        var result = SqlInterrogatorService.SqlInterrogator.ExtractColumnDetailsFromSelectClauseInSql(sql);

        _ = result.Should().HaveCount(2);
        _ = result[0].ColumnName.Should().Be("Name");
        _ = result[1].ColumnName.Should().Be("Email");
    }

    [Fact]
    public void HandleCTEQuery()
    {
        var sql = @"
                    WITH UserCTE AS (
                    SELECT Id, Name FROM AllUsers
                    )
                    SELECT Name, Email FROM Users";

        var result = SqlInterrogatorService.SqlInterrogator.ExtractColumnDetailsFromSelectClauseInSql(sql);

        _ = result.Should().HaveCount(2);
        _ = result[0].ColumnName.Should().Be("Name");
        _ = result[1].ColumnName.Should().Be("Email");
    }

    [Fact]
    public void HandleDistinct()
    {
        var sql = "SELECT DISTINCT Name, Email FROM Users";
        var result = SqlInterrogatorService.SqlInterrogator.ExtractColumnDetailsFromSelectClauseInSql(sql);

        _ = result.Should().HaveCount(2);
        _ = result[0].ColumnName.Should().Be("Name");
        _ = result[1].ColumnName.Should().Be("Email");
    }

    [Fact]
    public void HandleRowNumberFunction()
    {
        var sql = @"SELECT 
                    ROW_NUMBER() OVER(ORDER BY u.CreatedDate) AS RowNum,
                    u.Name
                    FROM Users u";

        var result = SqlInterrogatorService.SqlInterrogator.ExtractColumnDetailsFromSelectClauseInSql(sql);

        _ = result.Should().HaveCount(2);
        _ = result[0].ColumnName.Should().Be("RowNum");
        _ = result[1].ColumnName.Should().Be("Name");
    }

    [Fact]
    public void HandleLeadLagFunctions()
    {
        var sql = @"SELECT 
               LEAD(u.Salary) OVER(ORDER BY u.HireDate) AS NextSalary,
               LAG(u.Salary) OVER(ORDER BY u.HireDate) AS PrevSalary,
               u.Name
               FROM Users u";

        var result = SqlInterrogatorService.SqlInterrogator.ExtractColumnDetailsFromSelectClauseInSql(sql);

        _ = result.Should().HaveCount(3);
        _ = result[0].ColumnName.Should().Be("NextSalary");
        _ = result[1].ColumnName.Should().Be("PrevSalary");
        _ = result[2].ColumnName.Should().Be("Name");
    }

    [Fact]
    public void HandleFirstValueLastValue()
    {
        var sql = @"SELECT 
                    FIRST_VALUE(u.Name) OVER(PARTITION BY u.Department ORDER BY u.Salary DESC) AS HighestPaid,
                    LAST_VALUE(u.Name) OVER(PARTITION BY u.Department ORDER BY u.Salary DESC) AS LowestPaid
                    FROM Users u";

        var result = SqlInterrogatorService.SqlInterrogator.ExtractColumnDetailsFromSelectClauseInSql(sql);

        _ = result.Should().HaveCount(2);
        _ = result[0].ColumnName.Should().Be("HighestPaid");
        _ = result[1].ColumnName.Should().Be("LowestPaid");
    }

    [Fact]
    public void HandleJapaneseColumnNames()
    {
        var sql = "SELECT u.[??] AS Name, u.[???] FROM [????] u";

        var result = SqlInterrogatorService.SqlInterrogator.ExtractColumnDetailsFromSelectClauseInSql(sql);

        _ = result.Should().HaveCount(2);
        _ = result[0].ColumnName.Should().Be("Name");
        _ = result[1].ColumnName.Should().Be("???");
    }
}
