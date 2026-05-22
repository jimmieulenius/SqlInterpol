
namespace SqlInterpol;

/// <summary>
/// A fragment that emits a pre-formed SQL string verbatim, without quoting or parameterization.
/// </summary>
/// <param name="Value">The raw SQL string to emit.</param>
public readonly record struct SqlRawFragment(string Value) : ISqlFragment
{
    /// <inheritdoc />
    public string ToSql(ISqlContext context, SqlRenderMode mode = SqlRenderMode.Default) => Value;
}