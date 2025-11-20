namespace SqlUrlParameteriserService;

/// <summary>
/// Provides functionality to parameterize SQL queries using URL query string parameters.
/// </summary>
public static class SqlUrlParameteriser
{
    /// <summary>
    /// Replaces SQL parameter placeholders with their corresponding values from a URL query string.
    /// </summary>
    /// <param name="sql">The SQL query containing parameter placeholders (e.g., @userId, [@status]).</param>
    /// <param name="url">The URL containing parameters in the format: ?parameters=[name]=[value];[name]=[value]</param>
    /// <returns>
    /// The SQL query with parameter placeholders replaced by their values from the URL parameters.
    /// Returns null if sql or url is null or empty. Parameter names are matched case-insensitively.
    /// </returns>
    /// <remarks>
    /// <para>This method expects URL parameters in a specific format:</para>
    /// <list type="bullet">
    /// <item>URL format: ?parameters=[paramName]=[value];[paramName]=[value]</item>
    /// <item>Example: ?parameters=userId=123;status=active;minPrice=100</item>
    /// </list>
    /// <para>SQL parameters are supported in two formats:</para>
    /// <list type="bullet">
    /// <item>Standard format: @parameterName</item>
    /// <item>Bracketed format: [@parameterName]</item>
    /// </list>
    /// <para>Parameter name matching is case-insensitive. Both @userId and [@userId] will match "userId" from the URL.</para>
    /// <para>String values are automatically wrapped in single quotes for SQL compatibility.
    /// Numeric values are inserted as-is.</para>
    /// <para>If a SQL parameter has no matching URL parameter, it remains unchanged in the output.</para>
    /// </remarks>
    /// <example>
    /// <para><strong>Basic Usage:</strong></para>
    /// <code>
    /// var sql = "SELECT * FROM Users WHERE Id = @userId AND Status = @status";
    /// var url = "https://api.example.com/users?parameters=userId=123;status=active";
    /// var result = SqlUrlParameteriser.ParameteriseSqlFromUrl(sql, url);
    /// // Result: "SELECT * FROM Users WHERE Id = 123 AND Status = 'active'"
    /// </code>
    /// 
    /// <para><strong>Bracketed Parameters:</strong></para>
    /// <code>
    /// var sql = "SELECT * FROM Users WHERE Id = [@userId] AND Status = [@status]";
    /// var url = "https://api.example.com/users?parameters=userId=456;status=pending";
    /// var result = SqlUrlParameteriser.ParameteriseSqlFromUrl(sql, url);
    /// // Result: "SELECT * FROM Users WHERE Id = 456 AND Status = 'pending'"
    /// </code>
    /// 
    /// <para><strong>Mixed Format:</strong></para>
    /// <code>
    /// var sql = "SELECT * FROM Orders WHERE UserId = @userId AND Status = [@status]";
    /// var url = "https://api.example.com/orders?parameters=userId=789;status=completed";
    /// var result = SqlUrlParameteriser.ParameteriseSqlFromUrl(sql, url);
    /// // Result: "SELECT * FROM Orders WHERE UserId = 789 AND Status = 'completed'"
    /// </code>
    /// 
    /// <para><strong>Numeric and String Values:</strong></para>
    /// <code>
    /// var sql = "SELECT * FROM Products WHERE CategoryId = @categoryId AND Name = @productName AND Price > @minPrice";
    /// var url = "https://api.example.com/products?parameters=categoryId=5;productName=Laptop;minPrice=500.99";
    /// var result = SqlUrlParameteriser.ParameteriseSqlFromUrl(sql, url);
    /// // Result: "SELECT * FROM Products WHERE CategoryId = 5 AND Name = 'Laptop' AND Price > 500.99"
    /// </code>
    /// 
    /// <para><strong>Missing Parameters:</strong></para>
    /// <code>
    /// var sql = "SELECT * FROM Users WHERE Id = @userId AND Status = @status AND Role = @role";
    /// var url = "https://api.example.com/users?parameters=userId=123;status=active";
    /// var result = SqlUrlParameteriser.ParameteriseSqlFromUrl(sql, url);
    /// // Result: "SELECT * FROM Users WHERE Id = 123 AND Status = 'active' AND Role = @role"
    /// // (role parameter remains unchanged)
    /// </code>
    /// </example>
    public static string? ParameteriseSqlFromUrl(string? sql, string? url)
    {
        if (string.IsNullOrWhiteSpace(sql) || string.IsNullOrWhiteSpace(url))
        {
            return null;
        }

        // Extract parameters from URL
        var queryParams = ExtractParametersFromUrl(url);
        if (queryParams.Count == 0)
        {
            return sql;
        }

        var result = sql;

        // Replace each URL parameter in the SQL
        foreach (var (paramName, paramValue) in queryParams)
        {
            // Handle bracketed format [@paramName]
            var bracketedPattern = $"[@{paramName}]";
            if (result.Contains(bracketedPattern, StringComparison.OrdinalIgnoreCase))
            {
                var replacement = IsNumeric(paramValue) ? paramValue : $"'{paramValue}'";
                result = result.Replace(bracketedPattern, replacement, StringComparison.OrdinalIgnoreCase);
            }

            // Handle standard format @paramName
            var standardPattern = $"@{paramName}";
            result = ReplaceParameter(result, standardPattern, paramValue);
        }

        return result;
    }

    /// <summary>
    /// Extracts parameters from a URL in the format ?parameters=[name]=[value];[name]=[value].
    /// </summary>
    /// <param name="url">The URL containing parameters.</param>
    /// <returns>A dictionary of parameter names and values.</returns>
    private static Dictionary<string, string> ExtractParametersFromUrl(string url)
    {
        var parameters = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        // Find the query string start
        var queryIndex = url.IndexOf('?');
        if (queryIndex < 0)
        {
            return parameters;
        }

        // Extract query string (excluding fragment #)
        var queryStart = queryIndex + 1;
        var fragmentIndex = url.IndexOf('#', queryStart);
        var queryString = fragmentIndex >= 0
              ? url[queryStart..fragmentIndex]
                 : url[queryStart..];

        // Parse query string to find "parameters=" key
        var pairs = queryString.Split('&', StringSplitOptions.RemoveEmptyEntries);

        foreach (var pair in pairs)
        {
            var keyValue = pair.Split('=', 2);
            if (keyValue.Length == 2 &&
         keyValue[0].Equals("parameters", StringComparison.OrdinalIgnoreCase))
            {
                // Found the parameters key, now parse its value
                var parametersValue = Uri.UnescapeDataString(keyValue[1]);
                ParseParametersList(parametersValue, parameters);
                break; // Only process the first "parameters=" found
            }
        }

        return parameters;
    }

    /// <summary>
    /// Parses a semicolon-separated list of key-value pairs in the format [name]=[value];[name]=[value].
    /// </summary>
    /// <param name="parametersList">The semicolon-separated parameter list.</param>
    /// <param name="parameters">The dictionary to populate with parsed parameters.</param>
    private static void ParseParametersList(string parametersList, Dictionary<string, string> parameters)
    {
        if (string.IsNullOrWhiteSpace(parametersList))
        {
            return;
        }

        var entries = parametersList.Split(';', StringSplitOptions.RemoveEmptyEntries);

        foreach (var entry in entries)
        {
            var parts = entry.Split('=', 2);
            if (parts.Length == 2)
            {
                var name = parts[0].Trim();
                var value = parts[1].Trim();

                if (!string.IsNullOrEmpty(name))
                {
                    parameters[name] = value;
                }
            }
        }
    }

    /// <summary>
    /// Determines if a string value represents a numeric value.
    /// </summary>
    /// <param name="value">The value to check.</param>
    /// <returns>True if the value is numeric; otherwise, false.</returns>
    private static bool IsNumeric(string value)
    {
        return double.TryParse(value, out _);
    }

    /// <summary>
    /// Replaces a SQL parameter with its value, ensuring word boundaries are respected.
    /// </summary>
    /// <param name="sql">The SQL string.</param>
    /// <param name="parameter">The parameter name (e.g., @userId).</param>
    /// <param name="value">The parameter value.</param>
    /// <returns>The SQL with the parameter replaced.</returns>
    private static string ReplaceParameter(string sql, string parameter, string value)
    {
        var replacement = IsNumeric(value) ? value : $"'{value}'";
        var result = sql;
        var searchIndex = 0;

        while (searchIndex < result.Length)
        {
            var index = result.IndexOf(parameter, searchIndex, StringComparison.OrdinalIgnoreCase);
            if (index < 0)
            {
                break;
            }

            // Check if this is a complete parameter (not part of a longer name)
            var endIndex = index + parameter.Length;
            var isCompleteParameter = endIndex >= result.Length || (!char.IsLetterOrDigit(result[endIndex]) && result[endIndex] != '_');

            if (isCompleteParameter)
            {
                result = result[..index] + replacement + result[endIndex..];
                searchIndex = index + replacement.Length;
            }
            else
            {
                searchIndex = endIndex;
            }
        }

        return result;
    }
}
