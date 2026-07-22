using SqlInterpol.Configuration;
using SqlInterpol.Segments;

namespace SqlInterpol.Dialects.MySql;

/// <summary>
/// A helper fragment that strips the leading <c>SET</c> keyword from a <see cref="SqlSetFragment"/>
/// for use in MySQL's ON DUPLICATE KEY UPDATE clause.
/// </summary>
public class MySqlUpdateFragment : ISqlFragment
{
    private readonly SqlSetFragment _original;

    /// <summary>
    /// Initializes a new instance of the <see cref="MySqlUpdateFragment"/> class.
    /// </summary>
    /// <param name="original">The SET fragment to wrap.</param>
    public MySqlUpdateFragment(SqlSetFragment original)
    {
        _original = original;
    }

    /// <inheritdoc />
    public string ToSql(ISqlContext context, SqlRenderMode mode = SqlRenderMode.Default)
    {
        var sql = _original.ToSql(context, mode);
        if (sql.StartsWith("SET", StringComparison.OrdinalIgnoreCase)) return " " + sql[3..].TrimStart();
        return " " + sql;
    }
}