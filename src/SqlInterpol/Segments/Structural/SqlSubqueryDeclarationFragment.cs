using SqlInterpol.Configuration;
using SqlInterpol.Schema;

namespace SqlInterpol.Segments;

/// <summary>
/// Represents a subquery declaration fragment, typically used in FROM or JOIN clauses,
/// formatted with contextual indentation and aliasing.
/// </summary>
/// <param name="query">The nested query to declare.</param>
public class SqlSubqueryDeclarationFragment(ISqlQuery query) : ISqlFragment
{
    /// <summary>Gets the nested query.</summary>
    public ISqlQuery Query { get; } = query;

    /// <inheritdoc />
    public string ToSql(ISqlContext context, SqlRenderMode mode = SqlRenderMode.Default)
    {
        bool originalExcludeState = Query.ExcludeParentheses;
        Query.ExcludeParentheses = true;
        
        string innerSql;
        try
        {
             innerSql = ((ISqlFragment)Query).ToSql(context, mode);
        }
        finally
        {
             Query.ExcludeParentheses = originalExcludeState;
        }
        
        string baseIndent = "";
        if (context is SqlBuilder builder)
        {
            int currentIndex = builder.CurrentRenderIndex;
            
            if (currentIndex > 0 && currentIndex - 1 < builder.Segments.Count)
            {
                var lastSeg = builder.Segments[currentIndex - 1];
                
                if (lastSeg.Type == SqlSegmentType.Literal && lastSeg.Value is string lastLiteral)
                {
                    int lastNewLine = lastLiteral.LastIndexOf('\n');
                    string currentLinePrefix = lastNewLine >= 0 ? lastLiteral[(lastNewLine + 1)..] : lastLiteral;
                    baseIndent = new string(currentLinePrefix.TakeWhile(char.IsWhiteSpace).ToArray());
                }
            }
        }

        string extraIndent = new string(' ', context.Options.IndentSize);
        string totalBodyIndent = baseIndent + extraIndent;

        string[] lines = innerSql.Split(["\r\n", "\r", "\n"], StringSplitOptions.None);
        
        int innerBaseIndentLen = 0;
        if (lines.Any(l => !string.IsNullOrWhiteSpace(l)))
        {
            innerBaseIndentLen = lines.Where(l => !string.IsNullOrWhiteSpace(l)).Min(l => l.TakeWhile(char.IsWhiteSpace).Count());
        }

        string formattedInnerSql = string.Join("\n", lines.Select(l => 
        {
            if (string.IsNullOrWhiteSpace(l)) return string.Empty;
            return totalBodyIndent + l.Substring(innerBaseIndentLen);
        }));
        
        var entityRef = ((ISqlEntityBase)Query).Reference;
        string alias = (entityRef.Alias ?? entityRef.FallbackAlias ?? "stats").Trim();
        string quotedAlias = context.Dialect.QuoteIdentifier(alias);
        
        string declarationBody = $"(\n{formattedInnerSql}\n{baseIndent})";
        
        // Safely delegate back to the Dialect abstraction!
        return context.Dialect.ApplyAlias(declarationBody, quotedAlias.TrimEnd());
    }
}