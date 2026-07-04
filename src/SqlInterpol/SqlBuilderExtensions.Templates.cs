using System;

namespace SqlInterpol;

/// <summary>
/// Provides extension methods for appending pre-compiled SQL templates to a <see cref="SqlBuilder"/>.
/// </summary>
public static partial class SqlBuilderExtensions
{
    /// <summary>
    /// Appends a pre-compiled SQL template to the builder, rendering it immediately 
    /// and binding its arguments natively into the global parameter dictionary.
    /// </summary>
    public static SqlBuilder Append(this SqlBuilder builder, ISqlTemplate template, object? arguments = null)
    {
        string renderedText = template.Render(builder.Context, arguments);
        
        // CRITICAL FIX: Use SqlSegmentType.Raw + SqlRawFragment to completely bypass the Lexer.
        // Using 'Literal' accidentally feeds the rendered string back into the preprocessor!
        builder.AppendSegment(new SqlSegment(SqlSegmentType.Raw, new SqlRawFragment(renderedText)));
        
        return builder;
    }

    /// <summary>
    /// Appends a pre-compiled SQL template followed by a newline.
    /// </summary>
    public static SqlBuilder AppendLine(this SqlBuilder builder, ISqlTemplate template, object? arguments = null)
    {
        builder.Append(template, arguments);
        
        // Append line using Raw to maintain Lexer-bypass integrity
        builder.AppendSegment(new SqlSegment(SqlSegmentType.Raw, new SqlRawFragment(Environment.NewLine)));
        
        return builder;
    }
}