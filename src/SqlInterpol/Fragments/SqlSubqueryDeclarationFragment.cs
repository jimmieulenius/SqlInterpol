namespace SqlInterpol;

internal class SqlSubqueryDeclarationFragment : ISqlFragment
{
    private readonly ISqlQuery _query;

    public SqlSubqueryDeclarationFragment(ISqlQuery query)
    {
        _query = query;
    }

    public string ToSql(ISqlContext context, SqlRenderMode mode = SqlRenderMode.Default)
    {
        // 1. Extract the pristine inner SELECT statement layout
        string innerSql = ((ISqlFragment)_query).ToSql(context, mode);
        
        // 2. Discover the baseline indent of the current line by looking backward 
        // at the trailing text of the preceding literal segment relative to the active rendering cursor.
        string baseIndent = "";
        if (context is SqlBuilder builder)
        {
            // Read the tracking cursor position directly from the builder. 
            // The old parsing state interface is completely gone!
            int currentIndex = builder.CurrentRenderIndex;
            
            if (currentIndex > 0 && currentIndex - 1 < builder.Segments.Count)
            {
                var lastSeg = builder.Segments[currentIndex - 1];
                
                // Safely ensure we are only reading from a Text Literal
                if (lastSeg.Type == SqlSegmentType.Literal && lastSeg.Value is string lastLiteral)
                {
                    int lastNewLine = lastLiteral.LastIndexOf('\n');
                    string currentLinePrefix = lastNewLine >= 0 ? lastLiteral[(lastNewLine + 1)..] : lastLiteral;
                    
                    // Capture all preceding spaces/tabs on the current active line
                    baseIndent = new string(currentLinePrefix.TakeWhile(char.IsWhiteSpace).ToArray());
                }
            }
        }

        // 3. Apply the golden layout rule: Inner Level Indent = Current Baseline + Configured Indent Size
        string extraIndent = new string(' ', context.Options.IndentSize);
        string totalBodyIndent = baseIndent + extraIndent;

        // 4. Shift all lines of the inner body cleanly into the calculated indentation track
        string[] lines = innerSql.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);
        string formattedInnerSql = string.Join("\n", lines.Select(l => string.IsNullOrWhiteSpace(l) ? l : totalBodyIndent + l));
        
        // 5. Resolve and cleanly quote the auto-captured C# variable name as the SQL alias
        var entityRef = ((ISqlEntityBase)_query).Reference;
        string alias = entityRef.Alias ?? entityRef.FallbackAlias ?? "stats";
        string quotedAlias = context.Dialect.QuoteIdentifier(alias);
        
        // 6. Package it up, aligning the closing parenthesis perfectly back with the parent line's baseline!
        return $"(\n{formattedInnerSql}\n{baseIndent}) AS {quotedAlias}";
    }
}