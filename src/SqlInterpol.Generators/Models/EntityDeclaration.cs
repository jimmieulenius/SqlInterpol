namespace SqlInterpol.Generators;

/// <summary>
/// Represents a tracked entity from `db.Entity<T>(out var name, "alias")`
/// </summary>
public record EntityDeclaration(
    string VariableName, 
    string TypeName, 
    string MappedTableName,    
    string? MappedSchemaName,  
    string? ExplicitAlias, 
    bool WasAutoAliased,
    List<ColumnMap> Columns // Replaced Dictionary to preserve order!
);

public record ColumnMap(string PropertyName, string ColumnName);