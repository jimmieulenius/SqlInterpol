namespace SqlInterpol.Generators;

public record SqlQueryParameterInfo(
    int Index, 
    string TypeName, 
    string OriginalExpression) : IEquatable<SqlQueryParameterInfo>;