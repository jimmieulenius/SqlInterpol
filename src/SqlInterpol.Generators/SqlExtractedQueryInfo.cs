namespace SqlInterpol.Generators;

public record SqlExtractedQueryInfo(
    string MethodName,
    string SqlTemplate, 
    EquatableArray<SqlQueryParameterInfo> Parameters) : IEquatable<SqlExtractedQueryInfo>;