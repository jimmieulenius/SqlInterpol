using System;
using System.Collections.Generic;
using System.Linq;
using SqlInterpol.Parsing;

namespace SqlInterpol.Dialects.PostgreSql;

/// <summary>
/// A structural rewriter that safely repositions deferred locks for PostgreSQL 
/// and normalizes ON CONFLICT clauses.
/// </summary>
public class PostgreSqlSyntaxRewriter : ISqlSegmentRewriter
{
    /// <inheritdoc />
    public bool IsApplicable(ISqlCompilationState state) => true;

    /// <inheritdoc />
    public IReadOnlyList<SqlSegment> Rewrite(IReadOnlyList<SqlSegment> segments, ISqlContext context)
    {
        var rewritten = new List<SqlSegment>(segments.Count);
        SqlLockMode? deferredLock = null;

        for (int i = 0; i < segments.Count; i++)
        {
            var segment = segments[i];

            if (segment.Type == SqlSegmentType.Raw && segment.Value is SqlLockFragment lockFrag)
            {
                deferredLock = lockFrag.Mode;
                continue; 
            }

            bool isOnConflict = segment.HasTag(SqlSegmentTag.OnConflictKeyword) || 
                               (segment.Type == SqlSegmentType.Literal && segment.Value is string s1 && SqlRewriterHelpers.ContainsKeyword(s1, SqlKeyword.OnConflict.Value));

            if (isOnConflict)
            {
                var conflictCols = new List<ISqlProjection>();
                int doIdx = -1;
                int lookahead = 1;

                while (i + lookahead < segments.Count)
                {
                    var next = segments[i + lookahead];
                    
                    bool isDo = next.HasTag(SqlSegmentTag.DoUpdateSetKeyword) || 
                                (next.Type == SqlSegmentType.Literal && next.Value is string s2 && SqlRewriterHelpers.ContainsKeyword(s2, SqlKeyword.Do.Value));

                    if (isDo)
                    {
                        doIdx = i + lookahead;
                        break;
                    }

                    if (next.Value is SqlColumnReferenceBase colRefBase)
                    {
                        conflictCols.Add(new SqlUnqualifiedColumn(colRefBase.ColumnName));
                    }
                    else if (next.Value is ISqlProjection p)
                    {
                        conflictCols.Add(p);
                    }
                    
                    lookahead++;
                }

                if (doIdx > -1 && conflictCols.Count > 0)
                {
                    if (segment.Value is string text)
                    {
                        int idx = text.LastIndexOf(SqlKeyword.OnConflict.Value, StringComparison.OrdinalIgnoreCase);
                        if (idx > 0)
                        {
                            var preceding = text[..idx].TrimEnd();
                            if (preceding.Length > 0) rewritten.Add(new SqlSegment(SqlSegmentType.Literal, preceding));
                        }
                    }

                    rewritten.Add(new SqlSegment(SqlSegmentType.Literal, "\nON CONFLICT "));
                    rewritten.Add(new SqlSegment(SqlSegmentType.Raw, new PostgreSqlConflictTargetFragment(conflictCols)));

                    i = doIdx - 1;
                    continue;
                }
            }

            rewritten.Add(segment);
        }

        if (deferredLock == SqlLockMode.Update)
            rewritten.Add(new SqlSegment(SqlSegmentType.Literal, "\nFOR UPDATE"));
        else if (deferredLock == SqlLockMode.Share)
            rewritten.Add(new SqlSegment(SqlSegmentType.Literal, "\nFOR SHARE"));

        return rewritten;
    }

    private class SqlUnqualifiedColumn : ISqlProjection
    {
        private readonly string _columnName;
        public ISqlReference Reference => null!; 
        public string PropertyName => _columnName;
        public SqlUnqualifiedColumn(string columnName) => _columnName = columnName;
        public string ToSql(ISqlContext context, SqlRenderMode mode = SqlRenderMode.Default) => context.Dialect.QuoteIdentifier(_columnName);
    }

    private class PostgreSqlConflictTargetFragment : ISqlFragment
    {
        private readonly IReadOnlyList<ISqlProjection> _columns;
        public PostgreSqlConflictTargetFragment(IReadOnlyList<ISqlProjection> columns) => _columns = columns;

        public string ToSql(ISqlContext context, SqlRenderMode mode = SqlRenderMode.Default)
        {
            var cols = _columns.Select(c => c.ToSql(context, SqlRenderMode.BaseName));
            return $"({string.Join(", ", cols)})";
        }
    }
}