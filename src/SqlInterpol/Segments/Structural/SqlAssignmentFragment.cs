using SqlInterpol.Configuration;
using SqlInterpol.Infrastructure;
using SqlInterpol.Schema;

namespace SqlInterpol.Segments;

/// <summary>
/// Represents a column = value assignment in SET or INSERT clauses, generating
/// a named parameter and rendering the <c>column = @pN</c> expression.
/// </summary>
/// <param name="reference">The column reference to assign to.</param>
/// <param name="value">The value to assign; becomes a named SQL parameter.</param>
public class SqlAssignmentFragment(ISqlReference reference, object? value) 
    : ISqlAssignmentFragment, ISqlParameterGenerator
{
    private string? _parameterName;

    /// <summary>Gets the target column reference.</summary>
    public ISqlReference Reference { get; } = reference;

    /// <summary>Gets the raw value that will be bound as a parameter.</summary>
    public object? Value { get; } = value;

    /// <inheritdoc />
    public void GenerateParameters(ISqlContext context)
    {
        _parameterName = context.AddParameter(Value);
    }

    /// <inheritdoc />
    public string ToSql(ISqlContext context, SqlRenderMode mode = SqlRenderMode.Default)
    {
        _parameterName ??= context.AddParameter(Value);
        
        string targetSql = Reference.ToSql(context, SqlRenderMode.BaseName);
        return $"{targetSql} = {_parameterName}";
    }
}