namespace SqlInterpol.Generators;

internal sealed class SqlKeyword
{
    public string Value { get; }
    public bool IsClause { get; }
    public string ClauseGroup { get; }

    public SqlKeyword(string value, bool isClause = true, string? clauseGroup = null)
    {
        Value = value;
        IsClause = isClause;
        ClauseGroup = clauseGroup ?? value; // Defaults to itself if not specified
    }

    // Structural Clauses
    public static readonly SqlKeyword Select = new("SELECT");
    public static readonly SqlKeyword SelectDistinct = new("SELECT DISTINCT", clauseGroup: "SELECT"); // Maps to SELECT family!
    public static readonly SqlKeyword From = new("FROM");
    public static readonly SqlKeyword Join = new("JOIN");
    public static readonly SqlKeyword Insert = new("INSERT");
    public static readonly SqlKeyword Update = new("UPDATE");
    public static readonly SqlKeyword Delete = new("DELETE");
    public static readonly SqlKeyword Set = new("SET");
    public static readonly SqlKeyword Values = new("VALUES");
    public static readonly SqlKeyword Where = new("WHERE");
    public static readonly SqlKeyword OrderBy = new("ORDER BY");
    public static readonly SqlKeyword GroupBy = new("GROUP BY");
    public static readonly SqlKeyword Having = new("HAVING");
    public static readonly SqlKeyword Returning = new("RETURNING");
    public static readonly SqlKeyword On = new("ON");

    // Syntax Operators & Modifiers (Non-Clauses)
    public static readonly SqlKeyword Into = new("INTO", isClause: false);
    public static readonly SqlKeyword As = new("AS", isClause: false);
    public static readonly SqlKeyword Intersect = new("INTERSECT", isClause: false);
    public static readonly SqlKeyword Union = new("UNION", isClause: false);
    public static readonly SqlKeyword Except = new("EXCEPT", isClause: false);
    public static readonly SqlKeyword Over = new("OVER", isClause: false);
    public static readonly SqlKeyword OnConflict = new("ON CONFLICT", isClause: false);
    public static readonly SqlKeyword OnDuplicate = new("ON DUPLICATE", isClause: false);
    public static readonly SqlKeyword Merge = new("MERGE", isClause: false);

    /// <summary>
    /// Keywords sorted by length descending to optimize scanning algorithms.
    /// </summary>
    public static readonly SqlKeyword[] AllOrdered = new[]
    {
        SelectDistinct, OrderBy, GroupBy, OnConflict, OnDuplicate,
        Select, From, Join, Insert, Update, Delete, Set, Values, Into, 
        Where, Having, Returning, On, As, Intersect, Union, Except, Over, Merge
    };
}