using SqlInterpol.Config;

namespace SqlInterpol;

public class SqlAssignmentFragment(ISqlReference reference, object? value) 
    : ISqlAssignmentFragment, ISqlParameterGenerator
{
    public ISqlReference Reference => reference;
    public object? Value => value;
    private string? _parameterName;

    public void GenerateParameters(ISqlContext context)
    {
        // Reserve the parameter name during the parsing phase
        _parameterName = context.AddParameter(Value);
    }

    public string ToSql(ISqlContext context, SqlRenderMode mode = SqlRenderMode.Default)
    {
        // Use the pre-generated name, or generate one if called outside the builder
        _parameterName ??= context.AddParameter(Value);
        
        string targetSql = Reference.ToSql(context, SqlRenderMode.BaseName);

        return $"{targetSql} = {_parameterName}";
    }
}