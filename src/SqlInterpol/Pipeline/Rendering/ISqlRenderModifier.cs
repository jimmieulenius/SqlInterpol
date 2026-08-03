using SqlInterpol.Segments;

namespace SqlInterpol;

/// <summary>
/// Represents a value wrapped with a specific SQL rendering mode directive.
/// Used by the interpolation handler to dynamically adjust how segments are formatted.
/// </summary>
public interface ISqlRenderModifier
{
    object? Value { get; }
    SqlRenderMode Mode { get; }
    string? OriginalExpression { get; }
}