
namespace SqlInterpol.Parsing;

/// <summary>
/// Extends <see cref="ISqlContext"/> with access to the active <see cref="SqlBuilder"/> and mutable
/// parser state for use during an interpolated string parsing pass.
/// </summary>
public interface ISqlParserContext : ISqlContext
{
    /// <summary>Gets the active <see cref="SqlBuilder"/> that is accumulating this query's segments.</summary>
    SqlBuilder Builder { get; }
    /// <summary>Gets the mutable parser state for the current interpolation pass.</summary>
    ISqlParserState ParserState { get; }
}