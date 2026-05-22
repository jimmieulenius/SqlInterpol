namespace SqlInterpol.Parsing;

/// <summary>
/// Well-known string constants used to annotate <see cref="SqlSegment"/> instances
/// with semantic meaning during query building and dialect rewriting.
/// </summary>
public static class SqlSegmentTag
{
    public const string Paging = "Paging";
    public const string InsertTarget = "InsertTarget";
    public const string UpdateTarget = "UpdateTarget";

    public const string CreateKeyword = "CreateKeyword";
    public const string DropKeyword = "DropKeyword";
    public const string AlterKeyword = "AlterKeyword";
    public const string TruncateKeyword = "TruncateKeyword";
    public const string DeleteKeyword = "DeleteKeyword";
    public const string InsertKeyword = "InsertKeyword";

    public const string InsertValuesKeyword = "InsertValuesKeyword";
    public const string ReturningKeyword = "ReturningKeyword";
    public const string OnConflictKeyword = "OnConflictKeyword";
    public const string DoUpdateSetKeyword = "DoUpdateSetKeyword";
    public const string ForUpdateKeyword = "ForUpdateKeyword";
    public const string ForShareKeyword = "ForShareKeyword";
    public const string SelectKeyword = "SelectKeyword";
    public const string SelectDistinctKeyword = "SelectDistinctKeyword";
    public const string IntersectKeyword = "IntersectKeyword";
    public const string ExceptKeyword = "ExceptKeyword";
    public const string UnionKeyword = "UnionKeyword";
    public const string UnionAllKeyword = "UnionAllKeyword";
    public const string UpdateKeyword = "UpdateKeyword";
    public const string SetKeyword = "SetKeyword";
    public const string FromKeyword = "FromKeyword";
    public const string WhereKeyword = "WhereKeyword";
    public const string SelectIntoKeyword = "SelectIntoKeyword";
    public const string IntoKeyword = "IntoKeyword";
}