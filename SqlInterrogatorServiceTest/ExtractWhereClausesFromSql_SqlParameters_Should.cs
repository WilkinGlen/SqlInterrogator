using FluentAssertions;
using SqlInterrogatorService;

namespace SqlInterrogatorServiceTest;

public class ExtractWhereClausesFromSql_SqlParameters_Should
{
    [Fact]
    public void ExtractCondition_WhenSingleParameter()
    {
        var sql = "SELECT * FROM Users WHERE Id = @userId";

        var result = SqlInterrogator.ExtractWhereClausesFromSql(sql);

        _ = result.Should().ContainSingle();
        _ = result[0].Column.ColumnName.Should().Be("Id");
        _ = result[0].Operator.Should().Be("=");
        _ = result[0].Value.Should().Be("@userId");
    }

    [Fact]
    public void ExtractMultipleConditions_WhenMultipleParameters()
    {
        var sql = "SELECT * FROM Users WHERE Id = @userId AND Status = @userStatus";

        var result = SqlInterrogator.ExtractWhereClausesFromSql(sql);

        _ = result.Should().HaveCount(2);
        _ = result[0].Column.ColumnName.Should().Be("Id");
    _ = result[0].Operator.Should().Be("=");
        _ = result[0].Value.Should().Be("@userId");
        _ = result[1].Column.ColumnName.Should().Be("Status");
        _ = result[1].Operator.Should().Be("=");
    _ = result[1].Value.Should().Be("@userStatus");
    }

    [Fact]
    public void ExtractCondition_WhenParameterWithGreaterThan()
    {
      var sql = "SELECT * FROM Users WHERE Age > @minAge";

     var result = SqlInterrogator.ExtractWhereClausesFromSql(sql);

        _ = result.Should().ContainSingle();
        _ = result[0].Column.ColumnName.Should().Be("Age");
        _ = result[0].Operator.Should().Be(">");
        _ = result[0].Value.Should().Be("@minAge");
 }

    [Fact]
    public void ExtractCondition_WhenParameterWithLessThan()
    {
   var sql = "SELECT * FROM Users WHERE Age < @maxAge";

        var result = SqlInterrogator.ExtractWhereClausesFromSql(sql);

        _ = result.Should().ContainSingle();
        _ = result[0].Column.ColumnName.Should().Be("Age");
        _ = result[0].Operator.Should().Be("<");
        _ = result[0].Value.Should().Be("@maxAge");
    }

    [Fact]
    public void ExtractCondition_WhenParameterWithLike()
    {
        var sql = "SELECT * FROM Users WHERE Email LIKE @searchPattern";

        var result = SqlInterrogator.ExtractWhereClausesFromSql(sql);

        _ = result.Should().ContainSingle();
        _ = result[0].Column.ColumnName.Should().Be("Email");
   _ = result[0].Operator.Should().Be("LIKE");
        _ = result[0].Value.Should().Be("@searchPattern");
  }

    [Fact]
    public void ExtractCondition_WhenParameterWithQualifiedColumn()
  {
    var sql = "SELECT * FROM Users u WHERE u.Id = @userId";

        var result = SqlInterrogator.ExtractWhereClausesFromSql(sql);

        _ = result.Should().ContainSingle();
        _ = result[0].Column.ColumnName.Should().Be("u.Id");
        _ = result[0].Operator.Should().Be("=");
        _ = result[0].Value.Should().Be("@userId");
    }

    [Fact]
 public void ExtractConditions_WhenMixedParametersAndLiterals()
    {
        var sql = "SELECT * FROM Users WHERE Id = @userId AND Active = 1";

        var result = SqlInterrogator.ExtractWhereClausesFromSql(sql);

        _ = result.Should().HaveCount(2);
        _ = result[0].Column.ColumnName.Should().Be("Id");
  _ = result[0].Operator.Should().Be("=");
        _ = result[0].Value.Should().Be("@userId");
        _ = result[1].Column.ColumnName.Should().Be("Active");
    _ = result[1].Operator.Should().Be("=");
        _ = result[1].Value.Should().Be("1");
    }

  [Fact]
    public void ExtractConditions_WhenMixedParametersAndStrings()
    {
        var sql = "SELECT * FROM Users WHERE Name = @userName AND Status = 'Active'";

        var result = SqlInterrogator.ExtractWhereClausesFromSql(sql);

     _ = result.Should().HaveCount(2);
_ = result[0].Column.ColumnName.Should().Be("Name");
    _ = result[0].Operator.Should().Be("=");
    _ = result[0].Value.Should().Be("@userName");
  _ = result[1].Column.ColumnName.Should().Be("Status");
        _ = result[1].Operator.Should().Be("=");
        _ = result[1].Value.Should().Be("'Active'");
    }

    [Fact]
    public void ExtractCondition_WhenParameterWithUnderscores()
    {
        var sql = "SELECT * FROM Users WHERE Id = @user_id";

        var result = SqlInterrogator.ExtractWhereClausesFromSql(sql);

        _ = result.Should().ContainSingle();
        _ = result[0].Column.ColumnName.Should().Be("Id");
        _ = result[0].Operator.Should().Be("=");
        _ = result[0].Value.Should().Be("@user_id");
    }

    [Fact]
    public void ExtractCondition_WhenParameterWithNumbers()
    {
        var sql = "SELECT * FROM Users WHERE Id = @param1";

        var result = SqlInterrogator.ExtractWhereClausesFromSql(sql);

        _ = result.Should().ContainSingle();
        _ = result[0].Column.ColumnName.Should().Be("Id");
        _ = result[0].Operator.Should().Be("=");
        _ = result[0].Value.Should().Be("@param1");
    }

    [Fact]
    public void ExtractCondition_WhenDateParameterComparison()
    {
        var sql = "SELECT * FROM Users WHERE CreatedDate > @startDate AND CreatedDate < @endDate";

  var result = SqlInterrogator.ExtractWhereClausesFromSql(sql);

        _ = result.Should().HaveCount(2);
        _ = result[0].Column.ColumnName.Should().Be("CreatedDate");
        _ = result[0].Operator.Should().Be(">");
        _ = result[0].Value.Should().Be("@startDate");
        _ = result[1].Column.ColumnName.Should().Be("CreatedDate");
        _ = result[1].Operator.Should().Be("<");
  _ = result[1].Value.Should().Be("@endDate");
    }

    [Fact]
    public void ExtractCondition_WhenParameterWithNotEqual()
    {
        var sql = "SELECT * FROM Users WHERE Status != @excludedStatus";

var result = SqlInterrogator.ExtractWhereClausesFromSql(sql);

        _ = result.Should().ContainSingle();
        _ = result[0].Column.ColumnName.Should().Be("Status");
        _ = result[0].Operator.Should().Be("!=");
        _ = result[0].Value.Should().Be("@excludedStatus");
    }

    [Fact]
    public void ExtractCondition_WhenParameterWithGreaterThanOrEqual()
  {
        var sql = "SELECT * FROM Products WHERE Price >= @minPrice";

   var result = SqlInterrogator.ExtractWhereClausesFromSql(sql);

        _ = result.Should().ContainSingle();
        _ = result[0].Column.ColumnName.Should().Be("Price");
    _ = result[0].Operator.Should().Be(">=");
        _ = result[0].Value.Should().Be("@minPrice");
    }

    [Fact]
    public void ExtractCondition_WhenParameterWithLessThanOrEqual()
    {
    var sql = "SELECT * FROM Products WHERE Price <= @maxPrice";

    var result = SqlInterrogator.ExtractWhereClausesFromSql(sql);

        _ = result.Should().ContainSingle();
  _ = result[0].Column.ColumnName.Should().Be("Price");
    _ = result[0].Operator.Should().Be("<=");
        _ = result[0].Value.Should().Be("@maxPrice");
    }

    [Fact]
    public void ExtractConditions_WhenComplexQueryWithParameters()
    {
     var sql = @"
    SELECT u.Name, u.Email 
 FROM Users u 
 WHERE u.Id = @userId 
      AND u.Active = 1 
      AND u.CreatedDate > @startDate 
            ORDER BY u.Name";

        var result = SqlInterrogator.ExtractWhereClausesFromSql(sql);

        _ = result.Should().HaveCount(3);
        _ = result[0].Column.ColumnName.Should().Be("u.Id");
        _ = result[0].Operator.Should().Be("=");
  _ = result[0].Value.Should().Be("@userId");
        _ = result[1].Column.ColumnName.Should().Be("u.Active");
     _ = result[1].Operator.Should().Be("=");
        _ = result[1].Value.Should().Be("1");
        _ = result[2].Column.ColumnName.Should().Be("u.CreatedDate");
        _ = result[2].Operator.Should().Be(">");
        _ = result[2].Value.Should().Be("@startDate");
    }

    [Fact]
    public void ExtractCondition_WhenParameterInJoinedTable()
    {
        var sql = "SELECT * FROM Orders o INNER JOIN Users u ON o.UserId = u.Id WHERE o.Status = @orderStatus";

var result = SqlInterrogator.ExtractWhereClausesFromSql(sql);

        _ = result.Should().ContainSingle();
   _ = result[0].Column.ColumnName.Should().Be("o.Status");
        _ = result[0].Operator.Should().Be("=");
        _ = result[0].Value.Should().Be("@orderStatus");
    }

    [Fact]
    public void ExtractConditions_WhenMultipleParametersWithOrOperator()
    {
        var sql = "SELECT * FROM Users WHERE Status = @status1 OR Status = @status2";

        var result = SqlInterrogator.ExtractWhereClausesFromSql(sql);

     _ = result.Should().HaveCount(2);
 _ = result[0].Column.ColumnName.Should().Be("Status");
      _ = result[0].Operator.Should().Be("=");
 _ = result[0].Value.Should().Be("@status1");
        _ = result[1].Column.ColumnName.Should().Be("Status");
        _ = result[1].Operator.Should().Be("=");
        _ = result[1].Value.Should().Be("@status2");
    }

    [Fact]
    public void ExtractCondition_WhenBracketedColumnWithParameter()
    {
        var sql = "SELECT * FROM Users WHERE [User Name] = @userName";

        var result = SqlInterrogator.ExtractWhereClausesFromSql(sql);

        _ = result.Should().ContainSingle();
        _ = result[0].Column.ColumnName.Should().Be("User Name");
        _ = result[0].Operator.Should().Be("=");
   _ = result[0].Value.Should().Be("@userName");
    }

    [Fact]
    public void ExtractCondition_WhenFullyQualifiedColumnWithParameter()
    {
        var sql = "SELECT * FROM MyDB.dbo.Users WHERE MyDB.dbo.Users.Status = @userStatus";

    var result = SqlInterrogator.ExtractWhereClausesFromSql(sql);

    _ = result.Should().ContainSingle();
        _ = result[0].Column.ColumnName.Should().Be("MyDB.dbo.Users.Status");
        _ = result[0].Operator.Should().Be("=");
        _ = result[0].Value.Should().Be("@userStatus");
}
}
