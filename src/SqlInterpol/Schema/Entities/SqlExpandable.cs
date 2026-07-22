using SqlInterpol.Configuration;
using SqlInterpol.Segments;

namespace SqlInterpol.Schema;

/// <summary>
/// A macro fragment used to represent a C# DTO that should be expanded into structural 
/// SQL sequences (like columns, values, or assignments) by the pipeline's semantic rewriters.
/// </summary>
/// <typeparam name="TDto">The data transfer object type to expand.</typeparam>
/// <param name="keys">Optional primary key property names to exclude during SET expansions.</param>
public class SqlExpandable<TDto>(string[] keys) : ISqlFragment
{
    /// <summary>Gets the array of key property names.</summary>
    public string[] Keys { get; } = keys;

    /// <summary>
    /// Throws a <see cref="NotSupportedException"/>, as this macro must be structurally rewritten 
    /// by the pipeline before the rendering phase.
    /// </summary>
    public string ToSql(ISqlContext context, SqlRenderMode mode = SqlRenderMode.Default)
    {
        throw new NotSupportedException("SqlExpandable is a macro fragment and must be structurally rewritten by the pipeline before rendering.");
    }
}