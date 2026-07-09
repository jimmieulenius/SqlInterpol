namespace SqlInterpol;

/// <summary>
/// Public factory for generating standardized AST node fragments.
/// Methods are based on canonical PostgreSQL syntax.
/// </summary>
public static partial class Sql
{
    /// <summary>
    /// Concatenates multiple strings or arguments. (PostgreSQL: CONCAT)
    /// </summary>
    public static ISqlFragment Concat(params object[] args) => new SqlFunctionNodeFragment("CONCAT", args);

    /// <summary>
    /// Returns the first non-null argument. (PostgreSQL: COALESCE)
    /// </summary>
    public static ISqlFragment Coalesce(params object[] args) => new SqlFunctionNodeFragment("COALESCE", args);

    /// <summary>
    /// Aggregates string values with a delimiter. (PostgreSQL: STRING_AGG)
    /// </summary>
    public static ISqlFragment StringAgg(object expression, string delimiter) => new SqlFunctionNodeFragment("STRING_AGG", new[] { expression, delimiter });

    /// <summary>
    /// Truncates a timestamp or interval to a specified level of precision. (PostgreSQL: DATE_TRUNC)
    /// </summary>
    public static ISqlFragment DateTrunc(string field, object source) => new SqlFunctionNodeFragment("DATE_TRUNC", new[] { field, source });

    /// <summary>
    /// Performs a regular expression match. (PostgreSQL: ~ operator)
    /// </summary>
    public static ISqlFragment RegexMatch(object source, string pattern) => new SqlOperatorNode("~", source, pattern);
}