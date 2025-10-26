namespace SqlInterrogatorServiceTest;

using FluentAssertions;
using SqlInterrogatorService;

public class ExtractWhereClausesFromSql_Should_New
{
    [Fact]
    public void ReturnEmptyList_WhenSqlIsNull()
    {
        var result = SqlInterrogator.ExtractWhereClausesFromSql(null!);

        _ = result.Should().BeEmpty();
    }

    [Fact]
    public void ReturnEmptyList_WhenSqlIsEmpty()
    {
        var result = SqlInterrogator.ExtractWhereClausesFromSql("");

        _ = result.Should().BeEmpty();
    }

    [Fact]
    public void ReturnEmptyList_WhenSqlIsWhitespace()
    {
        var result = SqlInterrogator.ExtractWhereClausesFromSql("   ");

        _ = result.Should().BeEmpty();
    }

    [Fact]
    public void ReturnEmptyList_WhenNoWhereClause()
    {
        var sql = "SELECT * FROM Users";

        var result = SqlInterrogator.ExtractWhereClausesFromSql(sql);

        _ = result.Should().BeEmpty();
    }

    [Fact]
    public void ExtractSingleCondition_WhenSimpleEquality()
    {
        var sql = "SELECT * FROM Users WHERE Id = 1";

        var result = SqlInterrogator.ExtractWhereClausesFromSql(sql);

        _ = result.Should().ContainSingle();
        _ = result[0].Column.ColumnName.Should().Be("Id");
        _ = result[0].Column.Alias.Should().BeNull();
        _ = result[0].Operator.Should().Be("=");
        _ = result[0].Value.Should().Be("1");
    }

    [Fact]
    public void ExtractSingleCondition_WhenStringValue()
    {
        var sql = "SELECT * FROM Users WHERE Name = 'John'";

        var result = SqlInterrogator.ExtractWhereClausesFromSql(sql);

        _ = result.Should().ContainSingle();
        _ = result[0].Column.ColumnName.Should().Be("Name");
        _ = result[0].Operator.Should().Be("=");
        _ = result[0].Value.Should().Be("'John'");
    }

    [Fact]
    public void ExtractMultipleConditions_WhenAndOperator()
    {
        var sql = "SELECT * FROM Users WHERE Id = 1 AND Active = 1";

        var result = SqlInterrogator.ExtractWhereClausesFromSql(sql);

        _ = result.Should().HaveCount(2);
        _ = result[0].Column.ColumnName.Should().Be("Id");
        _ = result[0].Operator.Should().Be("=");
        _ = result[0].Value.Should().Be("1");
        _ = result[1].Column.ColumnName.Should().Be("Active");
        _ = result[1].Operator.Should().Be("=");
        _ = result[1].Value.Should().Be("1");
    }

    [Fact]
    public void ExtractMultipleConditions_WhenOrOperator()
    {
        var sql = "SELECT * FROM Users WHERE Status = 1 OR Status = 2";

        var result = SqlInterrogator.ExtractWhereClausesFromSql(sql);

        _ = result.Should().HaveCount(2);
        _ = result[0].Column.ColumnName.Should().Be("Status");
        _ = result[0].Operator.Should().Be("=");
        _ = result[0].Value.Should().Be("1");
        _ = result[1].Column.ColumnName.Should().Be("Status");
        _ = result[1].Operator.Should().Be("=");
        _ = result[1].Value.Should().Be("2");
    }

    [Fact]
    public void ExtractCondition_WhenQualifiedColumnName()
    {
        var sql = "SELECT * FROM Users u WHERE u.Active = 1";

        var result = SqlInterrogator.ExtractWhereClausesFromSql(sql);

        _ = result.Should().ContainSingle();
        _ = result[0].Column.ColumnName.Should().Be("u.Active");
        _ = result[0].Operator.Should().Be("=");
        _ = result[0].Value.Should().Be("1");
    }

    [Fact]
    public void ExtractCondition_WhenBracketedColumnName()
    {
        var sql = "SELECT * FROM Users WHERE [User Name] = 'John'";

        var result = SqlInterrogator.ExtractWhereClausesFromSql(sql);

        _ = result.Should().ContainSingle();
        _ = result[0].Column.ColumnName.Should().Be("Name");
        _ = result[0].Operator.Should().Be("=");
        _ = result[0].Value.Should().Be("'John'");
    }

    [Fact]
    public void ExtractCondition_WhenDoubleQuotedColumnName()
    {
        var sql = "SELECT * FROM Users WHERE \"Email\" = 'test@example.com'";

        var result = SqlInterrogator.ExtractWhereClausesFromSql(sql);

        _ = result.Should().ContainSingle();
        _ = result[0].Column.ColumnName.Should().Be("Email");
        _ = result[0].Operator.Should().Be("=");
        _ = result[0].Value.Should().Be("'test@example.com'");
    }

    [Fact]
    public void ExtractCondition_WhenGreaterThanOperator()
    {
        var sql = "SELECT * FROM Users WHERE Age > 18";

        var result = SqlInterrogator.ExtractWhereClausesFromSql(sql);

        _ = result.Should().ContainSingle();
        _ = result[0].Column.ColumnName.Should().Be("Age");
        _ = result[0].Operator.Should().Be(">");
        _ = result[0].Value.Should().Be("18");
    }

    [Fact]
    public void ExtractCondition_WhenLessThanOperator()
    {
        var sql = "SELECT * FROM Users WHERE Age < 65";

        var result = SqlInterrogator.ExtractWhereClausesFromSql(sql);

        _ = result.Should().ContainSingle();
        _ = result[0].Column.ColumnName.Should().Be("Age");
        _ = result[0].Operator.Should().Be("<");
        _ = result[0].Value.Should().Be("65");
    }

    [Fact]
    public void ExtractCondition_WhenGreaterThanOrEqualOperator()
    {
        var sql = "SELECT * FROM Users WHERE Age >= 18";

        var result = SqlInterrogator.ExtractWhereClausesFromSql(sql);

        _ = result.Should().ContainSingle();
        _ = result[0].Column.ColumnName.Should().Be("Age");
        _ = result[0].Operator.Should().Be(">=");
        _ = result[0].Value.Should().Be("18");
    }

    [Fact]
    public void ExtractCondition_WhenLessThanOrEqualOperator()
    {
        var sql = "SELECT * FROM Users WHERE Age <= 65";

        var result = SqlInterrogator.ExtractWhereClausesFromSql(sql);

        _ = result.Should().ContainSingle();
        _ = result[0].Column.ColumnName.Should().Be("Age");
        _ = result[0].Operator.Should().Be("<=");
        _ = result[0].Value.Should().Be("65");
    }

    [Fact]
    public void ExtractCondition_WhenNotEqualOperatorExclamation()
    {
        var sql = "SELECT * FROM Users WHERE Status != 0";

        var result = SqlInterrogator.ExtractWhereClausesFromSql(sql);

        _ = result.Should().ContainSingle();
        _ = result[0].Column.ColumnName.Should().Be("Status");
        _ = result[0].Operator.Should().Be("!=");
        _ = result[0].Value.Should().Be("0");
    }

    [Fact]
    public void ExtractCondition_WhenNotEqualOperatorAngleBrackets()
    {
        var sql = "SELECT * FROM Users WHERE Status <> 0";

        var result = SqlInterrogator.ExtractWhereClausesFromSql(sql);

        _ = result.Should().ContainSingle();
        _ = result[0].Column.ColumnName.Should().Be("Status");
        _ = result[0].Operator.Should().Be("<>");
        _ = result[0].Value.Should().Be("0");
    }

    [Fact]
    public void ExtractCondition_WhenLikeOperator()
    {
        var sql = "SELECT * FROM Users WHERE Email LIKE '%@example.com'";

        var result = SqlInterrogator.ExtractWhereClausesFromSql(sql);

        _ = result.Should().ContainSingle();
        _ = result[0].Column.ColumnName.Should().Be("Email");
        _ = result[0].Operator.Should().Be("LIKE");
        _ = result[0].Value.Should().Be("'%@example.com'");
    }

    [Fact]
    public void ExtractCondition_WhenNotLikeOperator()
    {
        var sql = "SELECT * FROM Users WHERE Email NOT LIKE '%@spam.com'";

        var result = SqlInterrogator.ExtractWhereClausesFromSql(sql);

        _ = result.Should().ContainSingle();
        _ = result[0].Column.ColumnName.Should().Be("Email");
        _ = result[0].Operator.Should().Be("NOT LIKE");
        _ = result[0].Value.Should().Be("'%@spam.com'");
    }

    [Fact]
    public void ExtractCondition_WhenInOperator()
    {
        var sql = "SELECT * FROM Users WHERE Status IN (1,2,3)";

        var result = SqlInterrogator.ExtractWhereClausesFromSql(sql);

        _ = result.Should().ContainSingle();
        _ = result[0].Column.ColumnName.Should().Be("Status");
        _ = result[0].Operator.Should().Be("IN");
        _ = result[0].Value.Should().Be("(1,2,3)");
    }

    [Fact]
    public void ExtractCondition_WhenNotInOperator()
    {
        var sql = "SELECT * FROM Users WHERE Status NOT IN (0,9)";

        var result = SqlInterrogator.ExtractWhereClausesFromSql(sql);

        _ = result.Should().ContainSingle();
        _ = result[0].Column.ColumnName.Should().Be("Status");
        _ = result[0].Operator.Should().Be("NOT IN");
        _ = result[0].Value.Should().Be("(0,9)");
    }

    [Fact]
    public void ExtractCondition_WhenIsNullOperator()
    {
        var sql = "SELECT * FROM Users WHERE DeletedDate IS NULL";

        var result = SqlInterrogator.ExtractWhereClausesFromSql(sql);

        _ = result.Should().ContainSingle();
        _ = result[0].Column.ColumnName.Should().Be("DeletedDate");
        _ = result[0].Operator.Should().Be("IS");
        _ = result[0].Value.Should().Be("NULL");
    }

    [Fact]
    public void ExtractCondition_WhenIsNotNullOperator()
    {
        var sql = "SELECT * FROM Users WHERE CreatedDate IS NOT NULL";

        var result = SqlInterrogator.ExtractWhereClausesFromSql(sql);

        _ = result.Should().ContainSingle();
        _ = result[0].Column.ColumnName.Should().Be("CreatedDate");
        _ = result[0].Operator.Should().Be("IS NOT");
        _ = result[0].Value.Should().Be("NULL");
    }

    [Fact]
    public void ExtractConditions_WhenDateComparison()
    {
        var sql = "SELECT * FROM Users WHERE CreatedDate > '2024-01-01'";

        var result = SqlInterrogator.ExtractWhereClausesFromSql(sql);

        _ = result.Should().ContainSingle();
        _ = result[0].Column.ColumnName.Should().Be("CreatedDate");
        _ = result[0].Operator.Should().Be(">");
        _ = result[0].Value.Should().Be("'2024-01-01'");
    }

    [Fact]
    public void ExtractConditions_WhenStopsAtOrderBy()
    {
        var sql = "SELECT * FROM Users WHERE Active = 1 ORDER BY Name";

        var result = SqlInterrogator.ExtractWhereClausesFromSql(sql);

        _ = result.Should().ContainSingle();
        _ = result[0].Column.ColumnName.Should().Be("Active");
        _ = result[0].Operator.Should().Be("=");
        _ = result[0].Value.Should().Be("1");
    }

    [Fact]
    public void ExtractConditions_WhenStopsAtGroupBy()
    {
        var sql = "SELECT * FROM Users WHERE Active = 1 GROUP BY Department";

        var result = SqlInterrogator.ExtractWhereClausesFromSql(sql);

        _ = result.Should().ContainSingle();
        _ = result[0].Column.ColumnName.Should().Be("Active");
        _ = result[0].Operator.Should().Be("=");
        _ = result[0].Value.Should().Be("1");
    }

    [Fact]
    public void ExtractConditions_WhenStopsAtHaving()
    {
        var sql = "SELECT COUNT(*) FROM Users WHERE Active = 1 GROUP BY Department HAVING COUNT(*) > 5";

        var result = SqlInterrogator.ExtractWhereClausesFromSql(sql);

        _ = result.Should().ContainSingle();
        _ = result[0].Column.ColumnName.Should().Be("Active");
        _ = result[0].Operator.Should().Be("=");
        _ = result[0].Value.Should().Be("1");
    }

    [Fact]
    public void ExtractConditions_WhenStopsAtUnion()
    {
        var sql = "SELECT * FROM Users WHERE Active = 1 UNION SELECT * FROM ArchivedUsers";

        var result = SqlInterrogator.ExtractWhereClausesFromSql(sql);

        _ = result.Should().ContainSingle();
        _ = result[0].Column.ColumnName.Should().Be("Active");
        _ = result[0].Operator.Should().Be("=");
        _ = result[0].Value.Should().Be("1");
    }

    [Fact]
    public void ExtractConditions_WhenStopsAtSemicolon()
    {
        var sql = "SELECT * FROM Users WHERE Active = 1; SELECT * FROM Orders";

        var result = SqlInterrogator.ExtractWhereClausesFromSql(sql);

        _ = result.Should().ContainSingle();
        _ = result[0].Column.ColumnName.Should().Be("Active");
        _ = result[0].Operator.Should().Be("=");
        _ = result[0].Value.Should().Be("1");
    }

    [Fact]
    public void ExtractConditions_WhenComplexAndOrMixture()
    {
        var sql = "SELECT * FROM Users WHERE Id = 1 AND Active = 1 OR Status = 2";

        var result = SqlInterrogator.ExtractWhereClausesFromSql(sql);

        _ = result.Should().HaveCount(3);
        _ = result[0].Column.ColumnName.Should().Be("Id");
        _ = result[0].Operator.Should().Be("=");
        _ = result[0].Value.Should().Be("1");
        _ = result[1].Column.ColumnName.Should().Be("Active");
        _ = result[1].Operator.Should().Be("=");
        _ = result[1].Value.Should().Be("1");
        _ = result[2].Column.ColumnName.Should().Be("Status");
        _ = result[2].Operator.Should().Be("=");
        _ = result[2].Value.Should().Be("2");
    }

    [Fact]
    public void IgnoreCommentsInWhereClause()
    {
        var sql = @"
            SELECT * FROM Users 
            WHERE -- This is a comment
                Active = 1 -- Another comment
            AND Id > 0 /* Multi-line
                comment */";

        var result = SqlInterrogator.ExtractWhereClausesFromSql(sql);

        _ = result.Should().HaveCount(2);
        _ = result[0].Column.ColumnName.Should().Be("Active");
        _ = result[0].Operator.Should().Be("=");
        _ = result[0].Value.Should().Be("1");
        _ = result[1].Column.ColumnName.Should().Be("Id");
        _ = result[1].Operator.Should().Be(">");
        _ = result[1].Value.Should().Be("0");
    }

    [Fact]
    public void ExtractConditions_WhenMultilineQuery()
    {
        var sql = @"
            SELECT * 
            FROM Users 
            WHERE 
                Active = 1 
            AND Status IN (1,2,3)
            AND Email LIKE '%@example.com'";

        var result = SqlInterrogator.ExtractWhereClausesFromSql(sql);

        _ = result.Should().HaveCount(3);
        _ = result[0].Column.ColumnName.Should().Be("Active");
        _ = result[0].Operator.Should().Be("=");
        _ = result[0].Value.Should().Be("1");
        _ = result[1].Column.ColumnName.Should().Be("Status");
        _ = result[1].Operator.Should().Be("IN");
        _ = result[1].Value.Should().Be("(1,2,3)");
        _ = result[2].Column.ColumnName.Should().Be("Email");
        _ = result[2].Operator.Should().Be("LIKE");
        _ = result[2].Value.Should().Be("'%@example.com'");
    }

    [Fact]
    public void ExtractConditions_WhenFullyQualifiedColumnName()
    {
        var sql = "SELECT * FROM MyDB.dbo.Users u WHERE u.Active = 1 AND u.Status = 2";

        var result = SqlInterrogator.ExtractWhereClausesFromSql(sql);

        _ = result.Should().HaveCount(2);
        _ = result[0].Column.ColumnName.Should().Be("u.Active");
        _ = result[1].Column.ColumnName.Should().Be("u.Status");
    }

    [Fact]
    public void ExtractConditions_WhenBracketedQualifiedColumn()
    {
        var sql = "SELECT * FROM Users WHERE [dbo].[Users].[Active] = 1";

        var result = SqlInterrogator.ExtractWhereClausesFromSql(sql);

        _ = result.Should().ContainSingle();
        _ = result[0].Column.ColumnName.Should().Be("dbo].[Users].[Active");
    }

    [Fact]
    public void ExtractConditions_WhenUseStatementPresent()
    {
        var sql = @"
            USE MyDatabase;
            SELECT * FROM Users WHERE Active = 1";

        var result = SqlInterrogator.ExtractWhereClausesFromSql(sql);

        _ = result.Should().ContainSingle();
        _ = result[0].Column.ColumnName.Should().Be("Active");
        _ = result[0].Operator.Should().Be("=");
        _ = result[0].Value.Should().Be("1");
    }

    [Fact]
    public void ExtractConditions_WhenCTEPresent()
    {
        var sql = @"
            WITH ActiveUsers AS (
                SELECT * FROM AllUsers WHERE Status = 1
            )
            SELECT * FROM Users WHERE Active = 1";

        var result = SqlInterrogator.ExtractWhereClausesFromSql(sql);

        _ = result.Should().ContainSingle();
        _ = result[0].Column.ColumnName.Should().Be("Active");
        _ = result[0].Operator.Should().Be("=");
        _ = result[0].Value.Should().Be("1");
    }

    [Fact]
    public void ExtractConditions_WhenInClauseWithSpaces()
    {
        var sql = "SELECT * FROM Users WHERE Status IN (1, 2, 3)";

        var result = SqlInterrogator.ExtractWhereClausesFromSql(sql);

        _ = result.Should().ContainSingle();
        _ = result[0].Column.ColumnName.Should().Be("Status");
        _ = result[0].Operator.Should().Be("IN");
        _ = result[0].Value.Should().Contain("(1,");
    }

    [Fact]
    public void ExtractConditions_WhenLikeWithWildcardsAtBothEnds()
    {
        var sql = "SELECT * FROM Users WHERE Name LIKE '%John%'";

        var result = SqlInterrogator.ExtractWhereClausesFromSql(sql);

        _ = result.Should().ContainSingle();
        _ = result[0].Column.ColumnName.Should().Be("Name");
        _ = result[0].Operator.Should().Be("LIKE");
        _ = result[0].Value.Should().Be("'%John%'");
    }

    [Fact]
    public void ExtractConditions_WhenMixedCaseOperators()
    {
        var sql = "SELECT * FROM Users WHERE Name like '%test%' AnD Active = 1";

        var result = SqlInterrogator.ExtractWhereClausesFromSql(sql);

        _ = result.Should().HaveCount(2);
        _ = result[0].Column.ColumnName.Should().Be("Name");
        _ = result[1].Column.ColumnName.Should().Be("Active");
    }

    [Fact]
    public void ExtractConditions_WhenColumnNameWithUnderscores()
    {
        var sql = "SELECT * FROM Users WHERE First_Name = 'John' AND Last_Name = 'Doe'";

        var result = SqlInterrogator.ExtractWhereClausesFromSql(sql);

        _ = result.Should().HaveCount(2);
        _ = result[0].Column.ColumnName.Should().Be("First_Name");
        _ = result[0].Operator.Should().Be("=");
        _ = result[0].Value.Should().Be("'John'");
        _ = result[1].Column.ColumnName.Should().Be("Last_Name");
        _ = result[1].Operator.Should().Be("=");
        _ = result[1].Value.Should().Be("'Doe'");
    }

    [Fact]
    public void ExtractConditions_WhenNumericComparisons()
    {
        var sql = "SELECT * FROM Products WHERE Price > 100.50 AND Quantity < 1000";

        var result = SqlInterrogator.ExtractWhereClausesFromSql(sql);

        _ = result.Should().HaveCount(2);
        _ = result[0].Column.ColumnName.Should().Be("Price");
        _ = result[0].Operator.Should().Be(">");
        _ = result[0].Value.Should().Be("100.50");
        _ = result[1].Column.ColumnName.Should().Be("Quantity");
        _ = result[1].Operator.Should().Be("<");
        _ = result[1].Value.Should().Be("1000");
    }

    [Fact]
    public void ExtractConditions_WhenBooleanColumn()
    {
        var sql = "SELECT * FROM Users WHERE IsActive = 1 AND IsDeleted = 0";

        var result = SqlInterrogator.ExtractWhereClausesFromSql(sql);

        _ = result.Should().HaveCount(2);
        _ = result[0].Column.ColumnName.Should().Be("IsActive");
        _ = result[0].Operator.Should().Be("=");
        _ = result[0].Value.Should().Be("1");
        _ = result[1].Column.ColumnName.Should().Be("IsDeleted");
        _ = result[1].Operator.Should().Be("=");
        _ = result[1].Value.Should().Be("0");
    }

    [Fact]
    public void ExtractConditions_WhenStringWithSpecialCharacters()
    {
        var sql = "SELECT * FROM Users WHERE Email = 'test@example.com'";

        var result = SqlInterrogator.ExtractWhereClausesFromSql(sql);

        _ = result.Should().ContainSingle();
        _ = result[0].Column.ColumnName.Should().Be("Email");
        _ = result[0].Operator.Should().Be("=");
        _ = result[0].Value.Should().Be("'test@example.com'");
    }

    [Fact]
    public void ExtractCondition_WhenValueIsAnotherColumn()
    {
        var sql = "SELECT * FROM Orders o INNER JOIN Users u ON o.UserId = u.Id WHERE o.Status = u.DefaultStatus";

        var result = SqlInterrogator.ExtractWhereClausesFromSql(sql);

        _ = result.Should().ContainSingle();
        _ = result[0].Column.ColumnName.Should().Be("o.Status");
        _ = result[0].Operator.Should().Be("=");
        _ = result[0].Value.Should().Be("u.DefaultStatus");
    }

    [Fact]
    public void ExtractCondition_WhenValueIsQualifiedColumn()
    {
        var sql = "SELECT * FROM Orders WHERE Orders.UserId = Users.Id";

        var result = SqlInterrogator.ExtractWhereClausesFromSql(sql);

        _ = result.Should().ContainSingle();
        _ = result[0].Column.ColumnName.Should().Be("Orders.UserId");
        _ = result[0].Operator.Should().Be("=");
        _ = result[0].Value.Should().Be("Users.Id");
    }

    [Fact]
    public void ExtractCondition_WhenValueIsBracketedColumn()
    {
        var sql = "SELECT * FROM Orders WHERE OrderDate > [LastModifiedDate]";

        var result = SqlInterrogator.ExtractWhereClausesFromSql(sql);

        _ = result.Should().ContainSingle();
        _ = result[0].Column.ColumnName.Should().Be("OrderDate");
        _ = result[0].Operator.Should().Be(">");
        _ = result[0].Value.Should().Be("[LastModifiedDate]");
    }

    [Fact]
    public void ExtractCondition_WhenValueIsFullyQualifiedColumn()
    {
        var sql = "SELECT * FROM Orders WHERE [dbo].[Orders].[CreatedDate] < [dbo].[Users].[RegisteredDate]";

        var result = SqlInterrogator.ExtractWhereClausesFromSql(sql);

        _ = result.Should().ContainSingle();
        _ = result[0].Column.ColumnName.Should().Be("dbo].[Orders].[CreatedDate");
        _ = result[0].Operator.Should().Be("<");
        _ = result[0].Value.Should().Be("[dbo].[Users].[RegisteredDate]");
    }

    [Fact]
    public void ExtractMultipleConditions_WhenMixedLiteralsAndColumns()
    {
        var sql = "SELECT * FROM Orders o WHERE o.Status = 1 AND o.UserId = u.Id AND o.Total > 100";

        var result = SqlInterrogator.ExtractWhereClausesFromSql(sql);

        _ = result.Should().HaveCount(3);
        _ = result[0].Column.ColumnName.Should().Be("o.Status");
        _ = result[0].Operator.Should().Be("=");
        _ = result[0].Value.Should().Be("1");
        _ = result[1].Column.ColumnName.Should().Be("o.UserId");
        _ = result[1].Operator.Should().Be("=");
        _ = result[1].Value.Should().Be("u.Id");
        _ = result[2].Column.ColumnName.Should().Be("o.Total");
        _ = result[2].Operator.Should().Be(">");
        _ = result[2].Value.Should().Be("100");
    }
}
