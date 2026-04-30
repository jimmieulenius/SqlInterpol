using SqlInterpol.Config;
using SqlInterpol.Dialects;

namespace SqlInterpol.Test.Dialects;

// 1. SqlDialectKind
public static partial class SqlDialectKindExtensions
{
    extension (SqlDialectKind)
    {
        public static SqlDialectKind CustomDb => new("Custom");
    }
}

// 2. SqlDialect
public class CustomDbSqlDialect : SqlDialectBase
{
    public override string ParameterPrefix => "!!"; // Unique prefix for testing
    public override string OpenQuote => "<<";       // Unique quotes for testing
    public override string CloseQuote => ">>";
    public override SqlDialectKind Kind => SqlDialectKind.CustomDb;

    public override SqlInterpolOptions GetDefaultOptions() => new() { ParameterIndexStart = 100 };
}

// 3. SqlBuilder
public static partial class SqlBuilderExtensions
{
    private static readonly CustomDbSqlDialect _customDb = new();

    extension (SqlBuilder _)
    {
        public static SqlBuilder CustomDb(SqlInterpolOptions? opt = null) 
            => new(_customDb, opt);
    }
}