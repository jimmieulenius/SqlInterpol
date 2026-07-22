namespace SqlInterpol.Schema;

/// <summary>
/// Defines the structural database type of a mapped entity.
/// </summary>
public enum SqlEntityType
{
    /// <summary>A standard database table.</summary>
    Table,
    
    /// <summary>A database view.</summary>
    View
}