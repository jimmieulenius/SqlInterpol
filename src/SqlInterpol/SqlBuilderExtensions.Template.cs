namespace SqlInterpol;

public static partial class SqlBuilderExtensions
{
    /// <summary>
    /// Appends a pre-compiled SQL template to the builder, rendering it immediately 
    /// and binding its arguments natively into the global parameter dictionary.
    /// </summary>
    public static SqlBuilder Append(this SqlBuilder builder, ISqlTemplate template, object? arguments = null)
    {
        string renderedText = template.Render(builder.Context, arguments);
        
        // Bypasses AST compilation! Drops the template output directly into the stream as a block literal.
        builder.AppendSegment(new SqlSegment(SqlSegmentType.Literal, renderedText));
        
        return builder;
    }

    /// <summary>
    /// Appends a pre-compiled SQL template followed by a newline.
    /// </summary>
    public static SqlBuilder AppendLine(this SqlBuilder builder, ISqlTemplate template, object? arguments = null)
    {
        builder.Append(template, arguments);
        return builder.AppendLine();
    }
}