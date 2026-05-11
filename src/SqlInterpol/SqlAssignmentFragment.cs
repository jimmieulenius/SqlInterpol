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
        // Reserve the parameter name now so the index is sequential!
        _parameterName = context.AddParameter(Value);
    }

    public string ToSql(ISqlContext context, SqlRenderMode mode = SqlRenderMode.Default)
    {
        // If it wasn't generated yet (e.g. called outside builder), do it now.
        _parameterName ??= context.AddParameter(Value);
        
        string targetSql = Reference.ToSql(context, SqlRenderMode.BaseName);
        
        return $"{targetSql} = {_parameterName}";
    }
}