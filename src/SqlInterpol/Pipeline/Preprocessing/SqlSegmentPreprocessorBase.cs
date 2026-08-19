using SqlInterpol.Configuration;
using SqlInterpol.Segments;

namespace SqlInterpol.Pipeline;

/// <summary>
/// A base class that provides safe SQL manipulation utilities to custom preprocessors.
/// </summary>
public abstract class SqlSegmentPreprocessorBase : ISqlSegmentPreprocessor
{
    /// <inheritdoc />
    public abstract IReadOnlyList<SqlSegment> Process(IReadOnlyList<SqlSegment> segments, ISqlContext context);

    /// <summary>
    /// Safely replaces a keyword in a raw SQL string, respecting word boundaries 
    /// and ignoring occurrences inside string literals or comments.
    /// </summary>
    protected string ReplaceKeyword(string sql, string target, string replacement)
    {
        return SqlLexicalHelper.ReplaceKeyword(sql, target, replacement);
    }
}