using SqlInterpol.Enums;

namespace SqlInterpol.Models;

public class SqlQuery
{
    public string Sql { get; }
    public Dictionary<string, object?> Parameters { get; }

    internal SqlQueryOptions Options { get; }

    // Set by As() / As<T>() so that column references on this instance can read it lazily
    internal string? _registeredAlias;

    public SqlQuery(string sql, Dictionary<string, object?> parameters, SqlQueryOptions? options = null)
    {
        Sql = sql;
        Parameters = parameters;
        Options = options ?? SqlQueryOptions.ForDatabase(SqlDatabaseType.SqlServer);
    }

    public override string ToString() => Sql;

    public SqlSubqueryTable As(string alias)
    {
        _registeredAlias = alias;
        return new SqlSubqueryTable(this, alias);
    }

    public SqlQueryProject<T> Project<T>() => new(this);

    public static implicit operator string(SqlQuery result) => result.Sql;
}