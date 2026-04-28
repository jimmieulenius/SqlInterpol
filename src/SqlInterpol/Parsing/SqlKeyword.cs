namespace SqlInterpol.Parsing;

public sealed class SqlKeyword
{
    public string Value { get; }
    public bool IsClauseInitiator { get; }
    public bool ExpectsDeclaration { get; }

    private SqlKeyword(string value, bool isClauseInitiator = false, bool expectsDeclaration = false)
    {
        Value = value;
        IsClauseInitiator = isClauseInitiator;
        ExpectsDeclaration = expectsDeclaration;
    }

    // --- CRUD & Main Clauses ---
    public static readonly SqlKeyword Select = new("SELECT", true, false);
    public static readonly SqlKeyword From = new("FROM", true, true);
    public static readonly SqlKeyword Insert = new("INSERT INTO", true, true);
    public static readonly SqlKeyword Update = new("UPDATE", true, true);
    public static readonly SqlKeyword Delete = new("DELETE", true, true);
    public static readonly SqlKeyword Set = new("SET", true, false);
    public static readonly SqlKeyword Values = new("VALUES", true, false);
    public static readonly SqlKeyword With = new("WITH", true, true);

    // --- Filtering & Grouping ---
    public static readonly SqlKeyword Where = new("WHERE", true, false);
    public static readonly SqlKeyword GroupBy = new("GROUP BY", true, false);
    public static readonly SqlKeyword Having = new("HAVING", true, false);
    public static readonly SqlKeyword OrderBy = new("ORDER BY", true, false);
    
    // --- Paging & Limits ---
    public static readonly SqlKeyword Top = new("TOP"); // Modifier, not an initiator
    public static readonly SqlKeyword Limit = new("LIMIT", true, false);
    public static readonly SqlKeyword Offset = new("OFFSET", true, false);
    public static readonly SqlKeyword Fetch = new("FETCH", true, false);

    // --- Joins ---
    public static readonly SqlKeyword InnerJoin = new("INNER JOIN", true, true);
    public static readonly SqlKeyword LeftJoin = new("LEFT JOIN", true, true);
    public static readonly SqlKeyword RightJoin = new("RIGHT JOIN", true, true);
    public static readonly SqlKeyword FullOuterJoin = new("FULL OUTER JOIN", true, true);
    public static readonly SqlKeyword CrossJoin = new("CROSS JOIN", true, true);
    public static readonly SqlKeyword Join = new("JOIN", true, true);

    // --- Set Operators ---
    public static readonly SqlKeyword Union = new("UNION", true, false);
    public static readonly SqlKeyword UnionAll = new("UNION ALL", true, false);
    public static readonly SqlKeyword Intersect = new("INTERSECT", true, false);
    public static readonly SqlKeyword Except = new("EXCEPT", true, false);

    // --- Structural & Logical ---
    public static readonly SqlKeyword As = new("AS");
    public static readonly SqlKeyword On = new("ON");
    public static readonly SqlKeyword And = new("AND");
    public static readonly SqlKeyword Or = new("OR");
    public static readonly SqlKeyword Not = new("NOT");
    public static readonly SqlKeyword In = new("IN");
    public static readonly SqlKeyword Exists = new("EXISTS");
    public static readonly SqlKeyword Is = new("IS");
    public static readonly SqlKeyword Null = new("NULL");
    public static readonly SqlKeyword Distinct = new("DISTINCT");

    // Registry for the Handler
    public static readonly SqlKeyword[] AllKeywords = 
    [ 
        Select, From, Insert, Update, Delete, Set, Values, With,
        Where, GroupBy, Having, OrderBy, Limit, Offset, Fetch,
        InnerJoin, LeftJoin, RightJoin, FullOuterJoin, CrossJoin, Join,
        Union, UnionAll, Intersect, Except, Top, Distinct, As, On, And, Or,
        Not, In, Exists, Is, Null
    ];

    public static readonly SqlKeyword[] AllInitiatorsOrdered = [.. AllKeywords
        .Where(k => k.IsClauseInitiator)
        .OrderByDescending(k => k.Value.Length)];

    public static implicit operator string(SqlKeyword keyword) => keyword.Value;
    public override string ToString() => Value;
}