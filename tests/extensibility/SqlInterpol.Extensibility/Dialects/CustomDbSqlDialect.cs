using SqlInterpol.Configuration;
using SqlInterpol.Dialects;

namespace SqlInterpol.Extensibility.Dialects;

[SqlDialect(OpenQuote = _openQuote, CloseQuote = _closeQuote)]
public class CustomDbSqlDialect : SqlDialectBase
{
    private const string _openQuote = "<<";
    private const string _closeQuote = ">>";

    public override string ParameterPrefix => "!!"; 
    public override string OpenQuote => _openQuote; 
    public override string CloseQuote => _closeQuote;
    
    public override SqlInterpol.Dialects.SqlDialectKind Kind => 
#if CSHARP14_EXTENSION_TYPES
        SqlDialectKind.CustomDb;
#else
        Dialects.SqlDialectKind.CustomDb;
#endif

    public override IReadOnlySet<SqlFeature> SupportedFeatures { get; } = new HashSet<SqlFeature>
    {
        SqlFeature.MultiTableDelete,
        SqlFeature.UpdatableInlineViews
    };

    public override SqlInterpolOptions GetDefaultOptions() => new() { ParameterIndexStart = 100 };
}