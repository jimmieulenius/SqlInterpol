namespace SqlInterpol;

/// <summary>
/// Represents a known SQL keyword used by the interpolation engine for clause detection,
/// dialect validation, and segment tagging.
/// </summary>
/// <remarks>
/// Instances are singletons exposed as <see langword="static readonly"/> fields.
/// They support implicit conversion to <see langword="string"/> so they can be used
/// directly inside interpolated SQL strings:
/// <code>
/// db.Append($"{SqlKeyword.Where} {p[x => x.IsActive]} = {true}");
/// </code>
/// </remarks>
public sealed class SqlKeyword
{
    /// <summary>Gets the SQL text of this keyword (e.g. <c>SELECT</c>, <c>INNER JOIN</c>).</summary>
    public string Value { get; }
    /// <summary>Gets whether this keyword begins a new SQL clause and triggers clause-boundary detection in the parser.</summary>
    public bool IsClauseInitiator { get; }
    /// <summary>Gets whether the clause introduced by this keyword expects a table or subquery declaration immediately after it.</summary>
    public bool ExpectsDeclaration { get; }
    /// <summary>
    /// Gets whether the clause introduced by this keyword expects the bare base name of an entity (DDL context).
    /// <see langword="null"/> means the keyword has no opinion on base-name rendering.
    /// </summary>
    public bool? ExpectsBaseName { get; }

    private SqlKeyword(string value, bool isClauseInitiator = false, bool expectsDeclaration = false, bool? expectsBaseName = null)
    {
        Value = value;
        IsClauseInitiator = isClauseInitiator;
        ExpectsDeclaration = expectsDeclaration;
        ExpectsBaseName = expectsBaseName;
    }

    /// <summary>Represents the <c>CREATE</c> DDL keyword.</summary>
    public static readonly SqlKeyword Create = new("CREATE", true, false, expectsBaseName: true);
    /// <summary>Represents the <c>DROP</c> DDL keyword.</summary>
    public static readonly SqlKeyword Drop = new("DROP", true, false, expectsBaseName: true);
    /// <summary>Represents the <c>ALTER</c> DDL keyword.</summary>
    public static readonly SqlKeyword Alter = new("ALTER", true, false, expectsBaseName: true);
    /// <summary>Represents the <c>TRUNCATE</c> DDL keyword.</summary>
    public static readonly SqlKeyword Truncate = new("TRUNCATE", true, false, expectsBaseName: true);

    /// <summary>Represents the <c>SELECT</c> keyword.</summary>
    public static readonly SqlKeyword Select = new("SELECT", true, false, expectsBaseName: false);
    /// <summary>Represents the <c>SELECT DISTINCT</c> keyword combination.</summary>
    public static readonly SqlKeyword SelectDistinct = new("SELECT DISTINCT", true, false, expectsBaseName: false);
    /// <summary>Represents the <c>FROM</c> keyword.</summary>
    public static readonly SqlKeyword From = new("FROM", true, true);
    /// <summary>Represents the <c>INSERT</c> keyword.</summary>
    public static readonly SqlKeyword Insert = new("INSERT", true, true, expectsBaseName: true);
    /// <summary>Represents the <c>UPDATE</c> keyword.</summary>
    public static readonly SqlKeyword Update = new("UPDATE", true, true, expectsBaseName: false);
    /// <summary>Represents the <c>DELETE</c> keyword.</summary>
    public static readonly SqlKeyword Delete = new("DELETE", true, true, expectsBaseName: false);
    /// <summary>Represents the <c>SET</c> keyword.</summary>
    public static readonly SqlKeyword Set = new("SET", true, false);
    /// <summary>Represents the <c>VALUES</c> keyword.</summary>
    public static readonly SqlKeyword Values = new("VALUES", true, false, expectsBaseName: false);
    /// <summary>Represents the <c>WITH</c> keyword (Common Table Expressions).</summary>
    public static readonly SqlKeyword With = new("WITH", true, true);
    /// <summary>Represents the <c>INTO</c> keyword.</summary>
    public static readonly SqlKeyword Into = new("INTO", false, false, expectsBaseName: false);
    /// <summary>Represents the <c>RETURNING</c> keyword (PostgreSQL / Firebird).</summary>
    public static readonly SqlKeyword Returning = new("RETURNING", true, false, expectsBaseName: true);
    /// <summary>Represents the <c>ON CONFLICT</c> keyword (PostgreSQL upsert).</summary>
    public static readonly SqlKeyword OnConflict = new("ON CONFLICT", true, false);
    /// <summary>Represents the <c>DO UPDATE SET</c> keyword combination (PostgreSQL upsert action).</summary>
    public static readonly SqlKeyword DoUpdateSet = new("DO UPDATE SET", true, false);
    /// <summary>Represents the <c>DO</c> keyword.</summary>
    public static readonly SqlKeyword Do = new("DO");
    /// <summary>Represents the <c>FOR UPDATE</c> row-locking keyword.</summary>
    public static readonly SqlKeyword ForUpdate = new("FOR UPDATE");
    /// <summary>Represents the <c>FOR SHARE</c> row-locking keyword.</summary>
    public static readonly SqlKeyword ForShare = new("FOR SHARE");

    /// <summary>Represents the <c>WHERE</c> keyword.</summary>
    public static readonly SqlKeyword Where = new("WHERE", true, false);
    /// <summary>Represents the <c>GROUP BY</c> keyword.</summary>
    public static readonly SqlKeyword GroupBy = new("GROUP BY", true, false);
    /// <summary>Represents the <c>HAVING</c> keyword.</summary>
    public static readonly SqlKeyword Having = new("HAVING", true, false);
    /// <summary>Represents the <c>ORDER BY</c> keyword.</summary>
    public static readonly SqlKeyword OrderBy = new("ORDER BY", true, false);

    /// <summary>Represents the <c>TOP</c> row-count modifier (SQL Server / MS Access). Not a clause initiator.</summary>
    public static readonly SqlKeyword Top = new("TOP");
    /// <summary>Represents the <c>LIMIT</c> keyword.</summary>
    public static readonly SqlKeyword Limit = new("LIMIT", true, false);
    /// <summary>Represents the <c>OFFSET</c> keyword.</summary>
    public static readonly SqlKeyword Offset = new("OFFSET", true, false);
    /// <summary>Represents the <c>FETCH</c> keyword (SQL Server / Oracle paging).</summary>
    public static readonly SqlKeyword Fetch = new("FETCH", true, false);

    /// <summary>Represents the <c>INNER JOIN</c> keyword.</summary>
    public static readonly SqlKeyword InnerJoin = new("INNER JOIN", true, true);
    /// <summary>Represents the <c>LEFT JOIN</c> keyword.</summary>
    public static readonly SqlKeyword LeftJoin = new("LEFT JOIN", true, true);
    /// <summary>Represents the <c>RIGHT JOIN</c> keyword.</summary>
    public static readonly SqlKeyword RightJoin = new("RIGHT JOIN", true, true);
    /// <summary>Represents the <c>FULL OUTER JOIN</c> keyword.</summary>
    public static readonly SqlKeyword FullOuterJoin = new("FULL OUTER JOIN", true, true);
    /// <summary>Represents the <c>CROSS JOIN</c> keyword.</summary>
    public static readonly SqlKeyword CrossJoin = new("CROSS JOIN", true, true);
    /// <summary>Represents the bare <c>JOIN</c> keyword (treated as <c>INNER JOIN</c> by most databases).</summary>
    public static readonly SqlKeyword Join = new("JOIN", true, true);

    /// <summary>Represents the <c>UNION</c> set operator.</summary>
    public static readonly SqlKeyword Union = new("UNION", true, false);
    /// <summary>Represents the <c>UNION ALL</c> set operator.</summary>
    public static readonly SqlKeyword UnionAll = new("UNION ALL", true, false);
    /// <summary>Represents the <c>INTERSECT</c> set operator.</summary>
    public static readonly SqlKeyword Intersect = new("INTERSECT", true, false);
    /// <summary>Represents the <c>EXCEPT</c> set operator.</summary>
    public static readonly SqlKeyword Except = new("EXCEPT", true, false);

    /// <summary>Represents the <c>AS</c> alias keyword.</summary>
    public static readonly SqlKeyword As = new("AS");
    /// <summary>Represents the <c>ON</c> join-condition keyword.</summary>
    public static readonly SqlKeyword On = new("ON");
    /// <summary>Represents the <c>FOR</c> keyword.</summary>
    public static readonly SqlKeyword For = new("FOR");
    /// <summary>Represents the <c>AND</c> logical operator.</summary>
    public static readonly SqlKeyword And = new("AND");
    /// <summary>Represents the <c>OR</c> logical operator.</summary>
    public static readonly SqlKeyword Or = new("OR");
    /// <summary>Represents the <c>NOT</c> logical operator.</summary>
    public static readonly SqlKeyword Not = new("NOT");
    /// <summary>Represents the <c>IN</c> set-membership operator.</summary>
    public static readonly SqlKeyword In = new("IN");
    /// <summary>Represents the <c>EXISTS</c> subquery predicate.</summary>
    public static readonly SqlKeyword Exists = new("EXISTS");
    /// <summary>Represents the <c>IS</c> comparison keyword (e.g. <c>IS NULL</c>).</summary>
    public static readonly SqlKeyword Is = new("IS");
    /// <summary>Represents the <c>NULL</c> keyword.</summary>
    public static readonly SqlKeyword Null = new("NULL");
    /// <summary>Represents the <c>DISTINCT</c> modifier keyword.</summary>
    public static readonly SqlKeyword Distinct = new("DISTINCT");
    /// <summary>Represents the <c>ANY</c> quantifier keyword.</summary>
    public static readonly SqlKeyword Any = new("ANY");
    /// <summary>Represents the <c>ALL</c> quantifier keyword.</summary>
    public static readonly SqlKeyword All = new("ALL");
    /// <summary>Represents the <c>SOME</c> quantifier keyword (equivalent to <c>ANY</c> in most dialects).</summary>
    public static readonly SqlKeyword Some = new("SOME");

    /// <summary>Represents the <c>ASC</c> sort-direction keyword.</summary>
    public static readonly SqlKeyword Asc = new("ASC");
    /// <summary>Represents the <c>DESC</c> sort-direction keyword.</summary>
    public static readonly SqlKeyword Desc = new("DESC");

    /// <summary>
    /// All registered <see cref="SqlKeyword"/> instances, used by the interpolation parser
    /// for clause-boundary detection and dialect validation.
    /// </summary>
    public static readonly SqlKeyword[] AllKeywords = 
    [ 
        Create,
        Drop,
        Alter,
        Truncate,
        Select,
        SelectDistinct,
        From,
        Insert,
        Update,
        Delete,
        Set,
        Values,
        With,
        Into,
        Returning,
        OnConflict,
        DoUpdateSet,
        Do,
        ForUpdate,
        ForShare,
        Where,
        GroupBy,
        Having,
        OrderBy,
        Limit,
        Offset,
        Fetch,
        InnerJoin,
        LeftJoin,
        RightJoin,
        FullOuterJoin,
        CrossJoin,
        Join,
        Union,
        UnionAll,
        Intersect,
        Except,
        Top,
        Distinct,
        As,
        On,
        For,
        And,
        Or,
        Not,
        In,
        Exists,
        Is,
        Null,
        Any,
        All,
        Some,
        Asc,
        Desc
    ];

    /// <summary>
    /// All clause-initiating keywords from <see cref="AllKeywords"/>, sorted longest-first so that
    /// multi-word keywords (e.g. <c>SELECT DISTINCT</c>) are matched before shorter ones (e.g. <c>SELECT</c>).
    /// </summary>
    public static readonly SqlKeyword[] AllInitiatorsOrdered = [.. AllKeywords
        .Where(k => k.IsClauseInitiator)
        .OrderByDescending(k => k.Value.Length)];

    /// <summary>Implicitly converts a <see cref="SqlKeyword"/> to its SQL text string.</summary>
    public static implicit operator string(SqlKeyword keyword) => keyword.Value;
    /// <inheritdoc cref="Value"/>
    public override string ToString() => Value;
}