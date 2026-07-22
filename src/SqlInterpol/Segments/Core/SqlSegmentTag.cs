namespace SqlInterpol.Segments;

/// <summary>
/// Well-known string constants used to annotate <see cref="SqlSegment"/> instances
/// with semantic meaning during query building and dialect rewriting.
/// </summary>
public static class SqlSegmentTag
{
    /// <summary>Identifies a segment responsible for query pagination (e.g., LIMIT, OFFSET, FETCH).</summary>
    public const string Paging = "Paging";

    /// <summary>Identifies the target entity or table in an INSERT statement.</summary>
    public const string InsertTarget = "InsertTarget";

    /// <summary>Identifies the target entity or table in an UPDATE statement.</summary>
    public const string UpdateTarget = "UpdateTarget";

    /// <summary>Identifies a 'CREATE' keyword.</summary>
    public const string CreateKeyword = "CreateKeyword";

    /// <summary>Identifies a 'DROP' keyword.</summary>
    public const string DropKeyword = "DropKeyword";

    /// <summary>Identifies an 'ALTER' keyword.</summary>
    public const string AlterKeyword = "AlterKeyword";

    /// <summary>Identifies a 'TRUNCATE' keyword.</summary>
    public const string TruncateKeyword = "TruncateKeyword";

    /// <summary>Identifies a 'DELETE' keyword.</summary>
    public const string DeleteKeyword = "DeleteKeyword";

    /// <summary>Identifies an 'INSERT' keyword.</summary>
    public const string InsertKeyword = "InsertKeyword";

    /// <summary>Identifies a 'VALUES' keyword within an INSERT statement.</summary>
    public const string InsertValuesKeyword = "InsertValuesKeyword";

    /// <summary>Identifies a 'RETURNING' or 'OUTPUT' keyword used for retrieving modified rows.</summary>
    public const string ReturningKeyword = "ReturningKeyword";

    /// <summary>Identifies an 'ON CONFLICT' or similar keyword used for upsert operations.</summary>
    public const string OnConflictKeyword = "OnConflictKeyword";

    /// <summary>Identifies a 'DO UPDATE SET' or similar keyword used in upsert conflict resolution.</summary>
    public const string DoUpdateSetKeyword = "DoUpdateSetKeyword";

    /// <summary>Identifies a 'FOR UPDATE' row-level locking keyword.</summary>
    public const string ForUpdateKeyword = "ForUpdateKeyword";

    /// <summary>Identifies a 'FOR SHARE' row-level locking keyword.</summary>
    public const string ForShareKeyword = "ForShareKeyword";

    /// <summary>Identifies a 'SELECT' keyword.</summary>
    public const string SelectKeyword = "SelectKeyword";

    /// <summary>Identifies a 'SELECT DISTINCT' keyword.</summary>
    public const string SelectDistinctKeyword = "SelectDistinctKeyword";

    /// <summary>Identifies an 'INTERSECT' keyword.</summary>
    public const string IntersectKeyword = "IntersectKeyword";

    /// <summary>Identifies an 'EXCEPT' keyword.</summary>
    public const string ExceptKeyword = "ExceptKeyword";

    /// <summary>Identifies a 'UNION' keyword.</summary>
    public const string UnionKeyword = "UnionKeyword";

    /// <summary>Identifies a 'UNION ALL' keyword.</summary>
    public const string UnionAllKeyword = "UnionAllKeyword";

    /// <summary>Identifies an 'UPDATE' keyword.</summary>
    public const string UpdateKeyword = "UpdateKeyword";

    /// <summary>Identifies a 'SET' keyword.</summary>
    public const string SetKeyword = "SetKeyword";

    /// <summary>Identifies a 'FROM' keyword.</summary>
    public const string FromKeyword = "FromKeyword";

    /// <summary>Identifies a 'WHERE' keyword.</summary>
    public const string WhereKeyword = "WhereKeyword";

    /// <summary>Identifies an 'INTO' keyword specifically used in a SELECT INTO statement.</summary>
    public const string SelectIntoKeyword = "SelectIntoKeyword";

    /// <summary>Identifies an 'INTO' keyword generally used in INSERT statements.</summary>
    public const string IntoKeyword = "IntoKeyword";

    /// <summary>Identifies an 'AS' keyword used for general aliasing.</summary>
    public const string AsKeyword = "AsKeyword";

    /// <summary>Identifies an 'AS' keyword specifically tied to a DELETE alias statement.</summary>
    public const string DeleteAsKeyword = "DeleteAsKeyword";

    /// <summary>Identifies an 'AS' keyword specifically tied to an UPDATE alias statement.</summary>
    public const string UpdateAsKeyword = "UpdateAsKeyword";

    /// <summary>
    /// Identifies an 'AS' keyword specifically used for table aliasing in FROM or JOIN clauses.
    /// Used by dialect rewriters (like Oracle) to filter out illegal syntax.
    /// </summary>
    public const string TableAliasAsKeyword = "TableAliasAsKeyword";

    /// <summary>Identifies a 'JOIN' keyword.</summary>
    public const string JoinKeyword = "JoinKeyword";

    /// <summary>Identifies an ANSI standard 'TRUE' keyword.</summary>
    public const string TrueKeyword = "TrueKeyword";

    /// <summary>Identifies an ANSI standard 'FALSE' keyword.</summary>
    public const string FalseKeyword = "FalseKeyword";

    /// <summary>Identifies an ANSI standard string concatenation operator '||'.</summary>
    public const string ConcatOperator = "ConcatOperator";
}