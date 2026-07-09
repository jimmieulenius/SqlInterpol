namespace SqlInterpol.Generators;

public record SqlExtractedQueryInfo(
    string MethodName,
    string SqlTemplate, 
    EquatableArray<SqlQueryParameterInfo> Parameters,
    string FilePath,
    int Line,
    int Character) : IEquatable<SqlExtractedQueryInfo>;