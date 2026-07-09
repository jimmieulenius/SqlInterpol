using System.ComponentModel;

namespace SqlInterpol;

/// <summary>
/// Internal-facing builder interface containing the raw mutator methods.
/// Hidden from IntelliSense to prevent accidental SQL injection or invalid AST states.
/// </summary>
[EditorBrowsable(EditorBrowsableState.Never)]
public interface ISqlGeneratorBuilder
{
    /// <summary>
    /// Appends raw text to the query (dangerous, internal use only).
    /// </summary>
    void AppendRaw(string text);

    /// <summary>
    /// Appends a structured AST node to the query tree.
    /// </summary>
    void AppendNode(ISqlFragment node);

    /// <summary>
    /// Appends a pre-compiled or cached template structure.
    /// </summary>
    void AppendTemplate(ISqlTemplate template);

    /// <summary>
    /// Appends a fully resolved Tier 1 segment directly from the interpolated string handler.
    /// </summary>
    void AppendSegment(SqlSegment segment);
}