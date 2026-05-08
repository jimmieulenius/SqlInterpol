using SqlInterpol.Config;

namespace SqlInterpol.Dialects;

public class PostgreSqlSqlDialect : SqlDialectBase
{
    public override SqlDialectKind Kind => SqlDialectKind.PostgreSql;
    public override string OpenQuote => "\"";
    public override string CloseQuote => "\"";
    public override string ParameterPrefix => "$";
    private static readonly string[] PostgresSymbols = ["->>", "->", "@>", "<@"];

    public override bool IsExpressionContext(string textBeforeParen)
    {
        // 1. Check Postgres-specific symbols first
        foreach (var symbol in PostgresSymbols)
        {
            if (textBeforeParen.EndsWith(symbol)) return true;
        }

        // 2. Fall back to the core engine's ANSI checks
        return base.IsExpressionContext(textBeforeParen);
    }

    public override SqlInterpolOptions GetDefaultOptions() => new() 
    { 
        ParameterIndexStart = 1 
    };
}