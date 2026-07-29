using SqlInterpol.Segments;

namespace SqlInterpol;

/// <summary>
/// A lightweight struct capturing a value and its intended render mode without allocations.
/// </summary>
public readonly struct SqlRenderModifier<T> : ISqlRenderModifier
{
    public T Value { get; }
    public SqlRenderMode Mode { get; }
    public string? OriginalExpression { get; }

    object? ISqlRenderModifier.Value => Value;

    public SqlRenderModifier(T value, SqlRenderMode mode, string? originalExpression)
    {
        Value = value;
        Mode = mode;
        OriginalExpression = originalExpression;
    }
}