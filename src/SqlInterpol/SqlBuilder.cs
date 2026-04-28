using SqlInterpol.Config;
using SqlInterpol.Dialects;
using SqlInterpol.Parsing;

namespace SqlInterpol;

public class SqlBuilder
{
    private readonly SqlContext _context;

    // Quick start constructor
    public SqlBuilder(SqlInterpolOptions options)
    {
        // Explicit resolution
        var dialect = SqlDialectFactory.Create(options.Dialect);
        _context = new SqlContext(dialect, options);
    }

    public SqlBuilder(SqlDialectKind kind = SqlDialectKind.SqlServer) 
        : this(new SqlInterpolOptions { Dialect = kind }) 
    { 
    }

    public SqlContext Context => _context;

    internal SqlQueryResult ExecuteHandler(SqlQueryInterpolatedStringHandler handler)
    {
        return new SqlQueryResult(handler.GetBuiltSql(), _context.Parameters);
    }
}