namespace SqlInterpol.Parsing;

/// <summary>Describes the role of an entity within a SQL query.</summary>
public enum SqlEntityRole
{
    /// <summary>A concrete table or view referenced directly in the query.</summary>
    Table,
    /// <summary>A common table expression (CTE) defined in a WITH clause.</summary>
    Cte
}