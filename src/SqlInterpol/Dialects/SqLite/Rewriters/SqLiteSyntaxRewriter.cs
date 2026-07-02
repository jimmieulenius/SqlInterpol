using System;
using System.Collections.Generic;
using System.Linq;
using SqlInterpol.Parsing;

namespace SqlInterpol.Dialects.SqLite;

/// <summary>
/// A structural rewriter that normalizes ON CONFLICT clauses for SQLite.
/// </summary>
public class SqLiteSyntaxRewriter : ISqlSegmentRewriter
{
    /// <inheritdoc />
    public bool IsApplicable(ISqlCompilationState state) => true;

    /// <inheritdoc />
    public IReadOnlyList<SqlSegment> Rewrite(IReadOnlyList<SqlSegment> segments, ISqlContext context)
    {
        var rewritten = new List<SqlSegment>(segments.Count);

        for (int i = 0; i < segments.Count; i++)
        {
            var segment = segments[i];

            bool isOnConflict = segment.HasTag(SqlSegmentTag.OnConflictKeyword) || 
                               (segment.Type == SqlSegmentType.Literal && segment.Value is string s1 && s1.Contains("ON CONFLICT", StringComparison.OrdinalIgnoreCase));

            if (isOnConflict)
            {
                var conflictCols = new List<ISqlProjection>();
                int doIdx = -1;
                int lookahead = 1;

                // =====================================================================
                // ROBUST AST HUNTING: Find the conflict columns and the DO keyword
                // =====================================================================
                while (i + lookahead < segments.Count)
                {
                    var next = segments[i + lookahead];
                    
                    bool isDo = next.HasTag(SqlSegmentTag.DoUpdateSetKeyword) || 
                                (next.Type == SqlSegmentType.Literal && next.Value is string s2 && s2.Contains("DO ", StringComparison.OrdinalIgnoreCase));

                    if (isDo)
                    {
                        doIdx = i + lookahead;
                        break;
                    }

                    // Unwrap the column references so they render strictly as unqualified names!
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
                        int idx = text.LastIndexOf("ON CONFLICT", StringComparison.OrdinalIgnoreCase);
                        if (idx > 0)
                        {
                            var preceding = text[..idx].TrimEnd();
                            if (preceding.Length > 0) rewritten.Add(new SqlSegment(SqlSegmentType.Literal, preceding));
                        }
                    }

                    // Inject the normalized ON CONFLICT target wrapped perfectly in parentheses!
                    rewritten.Add(new SqlSegment(SqlSegmentType.Literal, "\nON CONFLICT "));
                    rewritten.Add(new SqlSegment(SqlSegmentType.Raw, new SqLiteConflictTargetFragment(conflictCols)));

                    // Jump the loop pointer forward to just before the DO keyword
                    i = doIdx - 1;
                    continue;
                }
            }

            rewritten.Add(segment);
        }

        return rewritten;
    }

    // =========================================================================
    // HELPERS
    // =========================================================================
    private class SqlUnqualifiedColumn : ISqlProjection
    {
        private readonly string _columnName;
        public ISqlReference Reference => null!; 
        public string PropertyName => _columnName;
        public SqlUnqualifiedColumn(string columnName) => _columnName = columnName;
        public string ToSql(ISqlContext context, SqlRenderMode mode = SqlRenderMode.Default) => context.Dialect.QuoteIdentifier(_columnName);
    }

    private class SqLiteConflictTargetFragment : ISqlFragment
    {
        private readonly IReadOnlyList<ISqlProjection> _columns;
        public SqLiteConflictTargetFragment(IReadOnlyList<ISqlProjection> columns) => _columns = columns;

        public string ToSql(ISqlContext context, SqlRenderMode mode = SqlRenderMode.Default)
        {
            var cols = _columns.Select(c => c.ToSql(context, SqlRenderMode.BaseName));
            return $"({string.Join(", ", cols)})";
        }
    }
}