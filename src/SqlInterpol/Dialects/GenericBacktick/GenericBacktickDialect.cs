using SqlInterpol.Configuration;

namespace SqlInterpol.Dialects;

/// <summary>
/// A generic SQL dialect that uses backticks for identifier quoting.
/// Used as a fallback or baseline for MySQL/MariaDB-like environments.
/// </summary>
[SqlDialect(OpenQuote = _openQuote, CloseQuote = _closeQuote)]
public sealed class GenericBacktickDialect : SqlDialectBase
{
    private const string _openQuote = "`";
    private const string _closeQuote = "`";

    public override SqlDialectKind Kind => SqlDialectKind.GenericBacktick;
    public override string OpenQuote => _openQuote;
    public override string CloseQuote => _closeQuote;
    public override string ParameterPrefix => "@"; 
}