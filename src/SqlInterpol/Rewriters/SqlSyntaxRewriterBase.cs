using SqlInterpol.Parsing;

namespace SqlInterpol.Rewriters;

/// <summary>
/// An abstract base class that centralizes AST iteration, literal modification, and clause hunting
/// to keep dialect-specific structural rewriters perfectly DRY.
/// </summary>
public abstract class SqlSyntaxRewriterBase : ISqlSegmentRewriter
{
    public virtual bool IsApplicable(ISqlCompilationState state) => true;

    public virtual IReadOnlyList<SqlSegment> Rewrite(IReadOnlyList<SqlSegment> segments, ISqlContext context)
    {
        var rewritten = new List<SqlSegment>(segments.Count);
        bool droppedAs = false; // Tracks dropped 'AS' keywords to clean up trailing spaces

        for (int i = 0; i < segments.Count; i++)
        {
            var segment = segments[i];

            // 1. Recursive Pass
            segment = ProcessRecursiveSegments(segment, context);

            // 2. Table Alias 'AS' Stripping
            if (DropTableAliasAsKeyword && segment.HasTag(SqlSegmentTag.TableAliasAsKeyword))
            {
                droppedAs = true;
                continue; 
            }

            // 3. Literal Text Modifications
            if (segment.Type == SqlSegmentType.Literal && segment.Value is string literalValue)
            {
                var newValue = literalValue;
                if (droppedAs)
                {
                    if (newValue.StartsWith(" ")) newValue = newValue[1..];
                    droppedAs = false;
                }

                // ==============================================================
                // FAST TOKEN SWAPS (Handled seamlessly by the Dialects!)
                // ==============================================================
                if (segment.HasTag(SqlSegmentTag.TrueKeyword)) newValue = TranspileTrueKeyword(newValue);
                else if (segment.HasTag(SqlSegmentTag.FalseKeyword)) newValue = TranspileFalseKeyword(newValue);
                else if (segment.HasTag(SqlSegmentTag.ConcatOperator)) newValue = TranspileConcatOperator(newValue);

                newValue = ProcessLiteral(newValue);

                if (!ReferenceEquals(newValue, literalValue))
                {
                    segment = new SqlSegment(SqlSegmentType.Literal, newValue, segment.RenderMode, segment.Tags);
                }
            }
            else if (droppedAs)
            {
                droppedAs = false;
            }

            // 4. Locking Hints
            if (segment.Type == SqlSegmentType.Raw && segment.Value is SqlLockFragment lockFrag)
            {
                if (TryRewriteLock(lockFrag, segments, rewritten, ref i)) continue;
            }

            // 5. Upsert (ON CONFLICT) Check
            if (TryRewriteUpsert(segment, segments, rewritten, ref i)) continue;

            // 6. Returning Check
            if (TryRewriteReturning(segment, segments, rewritten, ref i)) continue;

            // 7. Paging (LIMIT/OFFSET) Check
            if (TryRewritePaging(segment, segments, rewritten, ref i)) continue;

            // 8. Pass-Through
            rewritten.Add(segment);
        }

        ApplyDeferredTransforms(rewritten, context);

        return rewritten;
    }

    // =====================================================================
    // VIRTUAL DIALECT STRATEGY HOOKS
    // =====================================================================
    
    // Fast Token Hooks
    protected virtual string TranspileTrueKeyword(string value) => value;
    protected virtual string TranspileFalseKeyword(string value) => value;
    protected virtual string TranspileConcatOperator(string value) => value;
    
    protected virtual bool DropTableAliasAsKeyword => false;
    protected virtual SqlSegment ProcessRecursiveSegments(SqlSegment segment, ISqlContext context) => segment;
    protected virtual string ProcessLiteral(string literal) => literal;
    protected virtual bool TryRewriteLock(SqlLockFragment lockFrag, IReadOnlyList<SqlSegment> segments, List<SqlSegment> rewritten, ref int i) => false;
    protected virtual bool TryRewriteUpsert(SqlSegment segment, IReadOnlyList<SqlSegment> segments, List<SqlSegment> rewritten, ref int i) => false;
    protected virtual bool TryRewriteReturning(SqlSegment segment, IReadOnlyList<SqlSegment> segments, List<SqlSegment> rewritten, ref int i) => false;
    protected virtual bool TryRewritePaging(SqlSegment segment, IReadOnlyList<SqlSegment> segments, List<SqlSegment> rewritten, ref int i) => false;
    
    protected virtual void ApplyDeferredTransforms(List<SqlSegment> rewritten, ISqlContext context)
    {
        int upAsIdx = rewritten.FindIndex(s => s.Type == SqlSegmentType.Raw && s.Value is SqlUpdateAsFragment);
        bool isAlreadyElevated = rewritten.Any(s => s.Type == SqlSegmentType.Raw && (s.Value is SqlUpdateCteFragment || s.Value is SqlUpdateSubqueryFragment));

        if (upAsIdx > -1 && !isAlreadyElevated)
        {
            var upAsFrag = (SqlUpdateAsFragment)rewritten[upAsIdx].Value!;
            int setFragIdx = rewritten.FindIndex(upAsIdx + 1, s => s.Value is SqlSetFragment);
            int whereKeywordIdx = rewritten.FindIndex(upAsIdx + 1, s => s.HasTag(SqlSegmentTag.WhereKeyword));

            if (setFragIdx > -1)
            {
                var setFrag = (SqlSetFragment)rewritten[setFragIdx].Value!;
                var multiTableUpdate = CreateMultiTableUpdate(upAsFrag, setFrag, rewritten, whereKeywordIdx, context);
                
                if (multiTableUpdate != null)
                {
                    rewritten.RemoveRange(upAsIdx, rewritten.Count - upAsIdx);
                    rewritten.Add(new SqlSegment(SqlSegmentType.Raw, multiTableUpdate));
                }
            }
        }
    }

    protected virtual SqlMultiTableUpdateFragment? CreateMultiTableUpdate(SqlUpdateAsFragment upAsFrag, SqlSetFragment setFrag, List<SqlSegment> rewritten, int whereKeywordIdx, ISqlContext context) 
        => null;

    // Shared Rewriter Utilities
    protected bool TryRewriteStandardOnConflict(SqlSegment segment, IReadOnlyList<SqlSegment> segments, List<SqlSegment> rewritten, ref int i)
    {
        bool isOnConflict = segment.HasTag(SqlSegmentTag.OnConflictKeyword) || 
            (segment.Type == SqlSegmentType.Literal && segment.Value is string s1 && SqlRewriterHelpers.ContainsKeyword(s1, SqlKeyword.OnConflict.Value));

        if (!isOnConflict) return false;

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

            if (next.Value is SqlColumnReferenceBase colRefBase) conflictCols.Add(new SqlUnqualifiedColumn(colRefBase.ColumnName));
            else if (next.Value is ISqlProjection p) conflictCols.Add(p);
            
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
            rewritten.Add(new SqlSegment(SqlSegmentType.Raw, new SqlConflictTargetFragment(conflictCols)));

            i = doIdx - 1;
            return true;
        }
        return false;
    }

    protected class SqlUnqualifiedColumn : ISqlProjection
    {
        private readonly string _columnName;
        public ISqlReference Reference => null!; 
        public string PropertyName => _columnName;
        public SqlUnqualifiedColumn(string columnName) => _columnName = columnName;
        public string ToSql(ISqlContext context, SqlRenderMode mode = SqlRenderMode.Default) => context.Dialect.QuoteIdentifier(_columnName);
    }

    protected class SqlConflictTargetFragment : ISqlFragment
    {
        private readonly IReadOnlyList<ISqlProjection> _columns;
        public SqlConflictTargetFragment(IReadOnlyList<ISqlProjection> columns) => _columns = columns;
        public string ToSql(ISqlContext context, SqlRenderMode mode = SqlRenderMode.Default)
        {
            var cols = _columns.Select(c => c.ToSql(context, SqlRenderMode.BaseName));
            return $"({string.Join(", ", cols)})";
        }
    }
}