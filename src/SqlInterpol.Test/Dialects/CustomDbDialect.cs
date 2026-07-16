using SqlInterpol.Dialects;

namespace SqlInterpol.Test.Dialects;

// 1. SqlDialectKind
public static partial class SqlDialectKindExtensions
{
    extension (SqlDialectKind)
    {
        public static SqlDialectKind CustomDb => new("CustomDb");
    }
}

// 2. SqlDialect
[SqlDialect(OpenQuote = _openQuote, CloseQuote = _closeQuote)]
public class CustomDbSqlDialect : SqlDialectBase
{
    private const string _openQuote = "<<";
    private const string _closeQuote = ">>";

    public override string ParameterPrefix => "!!"; // Unique prefix for testing
    public override string OpenQuote => _openQuote;       // Unique quotes for testing
    public override string CloseQuote => _closeQuote;
    public override SqlDialectKind Kind => SqlDialectKind.CustomDb;

    public override IReadOnlySet<SqlFeature> SupportedFeatures { get; } = new HashSet<SqlFeature>
    {
        SqlFeature.MultiTableDelete,
        SqlFeature.UpdatableInlineViews
    };

    public override SqlInterpolOptions GetDefaultOptions() => new() { ParameterIndexStart = 100 };
}

// 3. SqlBuilder
public static partial class SqlBuilderExtensions
{
    extension (SqlBuilder _)
    {
        public static SqlBuilder CustomDb(SqlInterpolOptions? opt = null) 
            => new(SqlDialectCache<CustomDbSqlDialect>.Instance, opt);
    }
}