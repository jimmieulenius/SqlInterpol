namespace SqlInterpol.Parsing;

public static class SqlSegmentTag
{
    public const string Paging = "Paging";
    public const string InsertTarget = "InsertTarget";
    public const string UpdateTarget = "UpdateTarget";
    public const string InsertValuesKeyword = "InsertValuesKeyword";
    public const string UpdateSetKeyword = "UpdateSetKeyword";
    public const string ReturningKeyword = "ReturningKeyword";
    public const string OnConflictKeyword = "OnConflictKeyword";
    public const string DoUpdateSetKeyword = "DoUpdateSetKeyword";
    public const string ForUpdateKeyword = "ForUpdateKeyword";
    public const string ForShareKeyword = "ForShareKeyword";
    public const string SelectKeyword = "SelectKeyword";
    public const string SelectDistinctKeyword = "SelectDistinctKeyword";
}