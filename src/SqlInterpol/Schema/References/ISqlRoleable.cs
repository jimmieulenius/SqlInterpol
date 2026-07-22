namespace SqlInterpol.Schema;

/// <summary>
/// Defines a SQL fragment or entity that can have its execution role dynamically assigned.
/// </summary>
public interface ISqlRoleable
{
    /// <summary>
    /// Gets or sets the assigned execution role (e.g., Table or CTE) for this entity.
    /// </summary>
    SqlEntityRole Role { get; set; }
}