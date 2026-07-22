using SqlInterpol.Configuration;
using SqlInterpol.Segments;

namespace SqlInterpol.Pipeline;

/// <summary>
/// Defines a lexical and semantic preprocessor that transforms, resolves, 
/// and tags a raw stream of SQL segments before it undergoes dialect-specific rewriting.
/// </summary>
public interface ISqlSegmentPreprocessor
{
    /// <summary>
    /// Processes a flat list of collected segments, evaluating unresolved values, 
    /// generating database parameters, and isolating structural DML keywords based on context.
    /// </summary>
    /// <param name="segments">The raw text and unresolved object segments collected by the builder.</param>
    /// <param name="context">The active query context tracking parameters and dialect capabilities.</param>
    /// <returns>A fully resolved, structurally tagged, and validated read-only list of segments.</returns>
    IReadOnlyList<SqlSegment> Process(IReadOnlyList<SqlSegment> segments, ISqlContext context);
}