namespace SqlInterpol;

/// <summary>
/// Identifies the SQL entity kind, used to select the correct concrete class and rendering
/// behavior in <see cref="SqlMetadataRegistry"/>.
/// </summary>
public enum SqlEntityType
{
    /// <summary>A physical database table.</summary>
    Table,

    /// <summary>A database view.</summary>
    View,

    /// <summary>An inline subquery used as a derived table.</summary>
    Subquery
}