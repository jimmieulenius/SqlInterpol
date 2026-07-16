using SqlInterpol.Execution;
using SqlInterpol.Segments;

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
    /// <param name="builder">The builder instance.</param>
    /// <param name="template">The pre-compiled template to execute.</param>
    /// <param name="arguments">An optional payload containing argument values required by the template.</param>
    /// <returns>The current builder instance for method chaining.</returns>
    public static SqlBuilder Append(this SqlBuilder builder, ISqlTemplate template, object? arguments = null)
    {
        string renderedText = template.Render(builder.Context, arguments);
        
        builder.AppendSegment(new SqlSegment(SqlSegmentType.Raw, new SqlRawFragment(renderedText)));
        
        return builder;
    }

    /// <summary>
    /// Appends a pre-compiled SQL template followed by a newline.
    /// </summary>
    /// <param name="builder">The builder instance.</param>
    /// <param name="template">The pre-compiled template to execute.</param>
    /// <param name="arguments">An optional payload containing argument values required by the template.</param>
    /// <returns>The current builder instance for method chaining.</returns>
    public static SqlBuilder AppendLine(this SqlBuilder builder, ISqlTemplate template, object? arguments = null)
    {
        builder.Append(template, arguments);
        
        builder.AppendSegment(new SqlSegment(SqlSegmentType.Raw, new SqlRawFragment(Environment.NewLine)));
        
        return builder;
    }
}