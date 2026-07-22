using SqlInterpol.Configuration;
using SqlInterpol.Dialects.SqlServer;
using SqlInterpol.Schema;
using SqlInterpol.Segments;

namespace SqlInterpol.Dialects;

/// <summary>
/// The SQL Server dialect: bracket identifiers, OFFSET/FETCH paging, WITH (UPDLOCK) row locking,
/// OUTPUT-based RETURNING emulation, and MERGE-based upsert transpilation.
/// </summary>
[SqlDialect(OpenQuote = _openQuote, CloseQuote = _closeQuote)]
public class SqlServerDialect : SqlDialectBase
{
    private const string _openQuote = "[";
    private const string _closeQuote = "]";

    /// <inheritdoc />
    public override SqlDialectKind Kind => SqlDialectKind.SqlServer;
    
    /// <inheritdoc />
    public override string OpenQuote => _openQuote;
    
    /// <inheritdoc />
    public override string CloseQuote => _closeQuote;
    
    /// <inheritdoc />
    public override string ParameterPrefix => "@p";
    
    /// <inheritdoc />
    public override IReadOnlySet<SqlFeature> SupportedFeatures { get; } =
        new HashSet<SqlFeature>
        {
            SqlFeature.ForUpdate,
            SqlFeature.ForShare,
            SqlFeature.Returning,
            SqlFeature.OnConflict,
            SqlFeature.SelectInto,
            SqlFeature.MultiTableDelete,
            SqlFeature.MultiTableUpdate,
            SqlFeature.DeleteAs,
            SqlFeature.UpdateAs
        };
    
    /// <inheritdoc />
    public override int QueryParametersMaxCount => 2099;

    /// <summary>
    /// Injects the SQL Server-specific syntax rewriter into the segment processing pipeline.
    /// The underlying <see cref="SqlRewriterCollection"/> automatically guarantees it is only added once.
    /// </summary>
    public override SqlInterpolOptions GetDefaultOptions()
    {
        var options = base.GetDefaultOptions();
        options.Rewriters.Add(new SqlServerSyntaxRewriter());
        return options;
    }

    /// <inheritdoc />
    public override string RenderFragment(ISqlFragment fragment, ISqlContext context)
    {
        return fragment switch
        {
            SqlLockFragment { Mode: SqlLockMode.Update } => " WITH (UPDLOCK)",
            SqlLockFragment { Mode: SqlLockMode.Share }  => " WITH (ROWLOCK, HOLDLOCK)",
            SqlLockFragment { Mode: SqlLockMode.NoLock } => " WITH (NOLOCK)",
            SqlPagingFragment p => $"OFFSET {p.Offset} ROWS FETCH NEXT {p.Limit} ROWS ONLY",
            _ => base.RenderFragment(fragment, context)
        };
    }

    /// <inheritdoc />
    protected override string RenderMultiTableDelete(SqlMultiTableDeleteFragment delete, ISqlContext context)
    {
        string targetSql = delete.Target.ToSql(context).Trim();

        if (delete.Target is SqlSegmentCollectionFragment coll)
        {
            foreach (var seg in coll.Segments)
            {
                if (seg.Type == SqlSegmentType.Reference && seg.Value is ISqlEntityBase e)
                {
                    targetSql = !string.IsNullOrEmpty(e.Reference.Alias) 
                        ? QuoteIdentifier(e.Reference.Alias)
                        : e.ToSql(context, SqlRenderMode.BaseName);
                    break;
                }
            }
        }

        var sql = $"DELETE FROM {targetSql}";
        if (delete.FromClause != null) sql += $"{Environment.NewLine}FROM {delete.FromClause.ToSql(context).Trim()}";
        if (delete.WhereClause != null) sql += $"{Environment.NewLine}WHERE {delete.WhereClause.ToSql(context).Trim()}";
            
        return sql;
    }

    /// <inheritdoc />
    protected override string RenderDeleteAs(SqlDeleteAsFragment fragment, ISqlContext context)
    {
        string alias = QuoteIdentifier(fragment.Target.Reference.Alias ?? "tgt");
        string baseName = fragment.Target.ToSql(context, SqlRenderMode.BaseName);
        
        // FIX: Appended ' AS {alias}' to correctly alias the table in the FROM clause
        return $"DELETE {alias} FROM {baseName} AS {alias}";
    }
}