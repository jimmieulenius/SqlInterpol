namespace SqlInterpol.Parsing;

/// <summary>
/// A foundational rewriter that applies core syntax transformations in a single linear pass. 
/// It handles base-name phase switching, set operations (UNION, EXCEPT), lock hints (FOR UPDATE), 
/// absorbs target alias bindings (DELETE AS, UPDATE AS), and sanitizes auto-aliases for standard DML.
/// </summary>
public class SqlCoreSyntaxRewriter : ISqlSegmentRewriter
{
    /// <inheritdoc />
    public bool IsApplicable(ISqlCompilationState state) => true;

    private static void SetAlias(object target, string? alias)
    {
        if (target is ISqlAliasable aliasable)
        {
            aliasable.Alias = alias;
        }
    }

    /// <inheritdoc />
    public IReadOnlyList<SqlSegment> Rewrite(IReadOnlyList<SqlSegment> segments, ISqlContext context)
    {
        var rewritten = new List<SqlSegment>(segments.Count);
        bool forceBaseNamePhase = false;

        bool hasDelete = false, hasUpdate = false, hasDeleteAs = false, hasUpdateAs = false, hasFrom = false;
        foreach(var s in segments)
        {
            if (s.HasTag(SqlSegmentTag.DeleteKeyword)) hasDelete = true;
            if (s.HasTag(SqlSegmentTag.UpdateKeyword)) hasUpdate = true;
            if (s.HasTag(SqlSegmentTag.DeleteAsKeyword)) hasDeleteAs = true;
            if (s.HasTag(SqlSegmentTag.UpdateAsKeyword)) hasUpdateAs = true;
            if (s.HasTag(SqlSegmentTag.FromKeyword)) hasFrom = true;
        }

        if ((hasDelete && !hasDeleteAs) || (hasUpdate && !hasUpdateAs && !hasFrom))
        {
            foreach (var s in segments)
            {
                if (s?.Type == SqlSegmentType.Reference && s.Value is ISqlEntityBase ent && ent is not ISqlQueryFragment)
                {
                    if (ent.Reference != null)
                    {
                        SetAlias(ent.Reference, null);
                    }
                    break; 
                }
            }
        }

        bool TryRewriteKeywordFragment<T>(string keyword, SqlSegment segment, int index, out int newIndex) where T : ISqlFragment
        {
            newIndex = index;
            int lookahead = index + 1;
            while (lookahead < segments.Count && segments[lookahead]?.Type == SqlSegmentType.Literal && string.IsNullOrWhiteSpace(segments[lookahead]?.Value as string)) lookahead++;
            if (lookahead < segments.Count && segments[lookahead]?.Value is T)
            {
                if (segment?.Value is string text)
                {
                    int keywordIndex = text.LastIndexOf(keyword, StringComparison.OrdinalIgnoreCase);
                    if (keywordIndex > -1)
                    {
                        rewritten.Add(new SqlSegment(SqlSegmentType.Literal, text[..keywordIndex]));
                        newIndex = lookahead - 1;
                        return true;
                    }
                }
                newIndex = lookahead - 1;
                return true; 
            }
            return false;
        }

        for (int i = 0; i < segments.Count; i++)
        {
            var segment = segments[i];
            if (segment == null) continue;

            if (segment.Type == SqlSegmentType.Literal && segment.Value is string litText && litText.Contains(';')) forceBaseNamePhase = false;
            
            if (segment.Tags != null)
            {
                foreach (var tag in segment.Tags)
                {
                    if (tag == SqlSegmentTag.FromKeyword || tag == SqlSegmentTag.UpdateKeyword || tag == SqlSegmentTag.IntoKeyword || tag == SqlSegmentTag.DeleteKeyword || tag == SqlSegmentTag.JoinKeyword)
                        forceBaseNamePhase = true;
                    else if (tag == SqlSegmentTag.SelectKeyword || tag == SqlSegmentTag.SelectDistinctKeyword || tag == SqlSegmentTag.SetKeyword || tag == SqlSegmentTag.WhereKeyword || tag == SqlSegmentTag.InsertValuesKeyword)
                        forceBaseNamePhase = false;
                }
            }
            
            if (segment.HasTag(SqlSegmentTag.OnConflictKeyword) || (segment.Type == SqlSegmentType.Literal && segment.Value is string s1 && s1.Contains("ON CONFLICT", StringComparison.OrdinalIgnoreCase)))
            {
                forceBaseNamePhase = true;
                if (segment.Value is string text)
                {
                    string clean = segment.HasTag(SqlSegmentTag.OnConflictKeyword) ? (text.EndsWith(" ") ? text[..^1] : text) : text;
                    if (segment.HasTag(SqlSegmentTag.OnConflictKeyword) && !clean.EndsWith('(')) clean += " (";
                    rewritten.Add(new SqlSegment(SqlSegmentType.Literal, clean));
                    continue;
                }
            }
            else if (segment.HasTag(SqlSegmentTag.DoUpdateSetKeyword) || (segment.Type == SqlSegmentType.Literal && segment.Value is string s2 && s2.Contains("DO UPDATE SET", StringComparison.OrdinalIgnoreCase)))
            {
                forceBaseNamePhase = false; 
                if (segment.Value is string text)
                {
                    string clean = segment.HasTag(SqlSegmentTag.DoUpdateSetKeyword) && !text.TrimStart().StartsWith(')') ? ")\n" + text.TrimStart() : text;
                    rewritten.Add(new SqlSegment(SqlSegmentType.Literal, clean));
                    continue;
                }
            }
            else if (segment.HasTag(SqlSegmentTag.ForUpdateKeyword) && segment.Value is string updateText)
            {
                int idx = updateText.IndexOf("FOR UPDATE", StringComparison.OrdinalIgnoreCase);
                if (idx > -1)
                {
                    rewritten.Add(new SqlSegment(SqlSegmentType.Literal, updateText[..idx].TrimEnd(' ', '\t')));
                    rewritten.Add(new SqlSegment(SqlSegmentType.Raw, new SqlLockFragment(SqlLockMode.Update)));
                    rewritten.Add(new SqlSegment(SqlSegmentType.Literal, updateText[(idx + 10)..]));
                    continue;
                }
            }
            else if (segment.HasTag(SqlSegmentTag.ForShareKeyword) && segment.Value is string shareText)
            {
                int idx = shareText.IndexOf("FOR SHARE", StringComparison.OrdinalIgnoreCase);
                if (idx > -1)
                {
                    rewritten.Add(new SqlSegment(SqlSegmentType.Literal, shareText[..idx].TrimEnd(' ', '\t')));
                    rewritten.Add(new SqlSegment(SqlSegmentType.Raw, new SqlLockFragment(SqlLockMode.Share)));
                    rewritten.Add(new SqlSegment(SqlSegmentType.Literal, shareText[(idx + 9)..]));
                    continue;
                }
            }
            else if (segment.HasTag(SqlSegmentTag.ExceptKeyword) || segment.HasTag(SqlSegmentTag.IntersectKeyword) || segment.HasTag(SqlSegmentTag.UnionKeyword) || segment.HasTag(SqlSegmentTag.UnionAllKeyword))
            {
                var op = SqlSetOperator.Union;
                if (segment.HasTag(SqlSegmentTag.ExceptKeyword)) op = SqlSetOperator.Except;
                else if (segment.HasTag(SqlSegmentTag.IntersectKeyword)) op = SqlSetOperator.Intersect;
                else if (segment.HasTag(SqlSegmentTag.UnionAllKeyword)) op = SqlSetOperator.UnionAll;

                if (rewritten.Count > 0 && rewritten[^1]?.Value is ISqlFragment left && i + 1 < segments.Count && segments[i + 1]?.Value is ISqlFragment right)
                {
                    rewritten[^1] = new SqlSegment(SqlSegmentType.Raw, new SqlSetOperationFragment(left, right, op));
                    i++; continue; 
                }
            }

            if (segment.HasTag(SqlSegmentTag.DeleteAsKeyword))
            {
                int delIdx = -1, targetIdx = -1;
                for (int j = rewritten.Count - 1; j >= 0; j--)
                {
                    if (delIdx == -1 && rewritten[j].HasTag(SqlSegmentTag.DeleteKeyword)) delIdx = j;
                    if (targetIdx == -1 && rewritten[j].Type == SqlSegmentType.Reference && rewritten[j].Value is ISqlEntityBase) targetIdx = j;
                    if (delIdx != -1 && targetIdx != -1) break;
                }

                if (delIdx > -1 && targetIdx > delIdx)
                {
                    var targetEntity = (ISqlEntityBase)rewritten[targetIdx].Value!;

                    if (targetEntity is not ISqlQueryFragment && !context.Dialect.SupportedFeatures.Contains(SqlFeature.DeleteAs)) 
                        throw new SqlDialectException($"'DELETE' with a target table alias is not supported by {context.Dialect.Kind}.");

                    string delText = rewritten[delIdx].Value?.ToString() ?? "";
                    int deleteKeywordIdx = delText.LastIndexOf("DELETE", StringComparison.OrdinalIgnoreCase);
                    string prefix = deleteKeywordIdx > 0 ? delText[..deleteKeywordIdx] : "";

                    rewritten.RemoveRange(delIdx, rewritten.Count - delIdx);
                    if (!string.IsNullOrEmpty(prefix)) rewritten.Add(new SqlSegment(SqlSegmentType.Literal, prefix));
                    rewritten.Add(new SqlSegment(SqlSegmentType.Raw, new SqlDeleteAsFragment(targetEntity)));
                    
                    int lookahead = i + 1;
                    while (lookahead < segments.Count && segments[lookahead]?.Type == SqlSegmentType.Literal && string.IsNullOrWhiteSpace(segments[lookahead]?.Value as string)) lookahead++;
                    if (lookahead < segments.Count) lookahead++;
                    i = lookahead - 1;
                    continue;
                }
                else if (!context.Dialect.SupportedFeatures.Contains(SqlFeature.DeleteAs))
                {
                    throw new SqlDialectException($"'DELETE' with a target table alias is not supported by {context.Dialect.Kind}.");
                }
            }
            else if (segment.HasTag(SqlSegmentTag.UpdateAsKeyword))
            {
                int localUpdateIdx = -1, targetIdx = -1;
                for (int j = rewritten.Count - 1; j >= 0; j--)
                {
                    if (localUpdateIdx == -1 && rewritten[j].HasTag(SqlSegmentTag.UpdateKeyword)) localUpdateIdx = j;
                    if (targetIdx == -1 && rewritten[j].Type == SqlSegmentType.Reference && rewritten[j].Value is ISqlEntityBase) targetIdx = j;
                    if (localUpdateIdx != -1 && targetIdx != -1) break;
                }

                if (localUpdateIdx > -1 && targetIdx > localUpdateIdx)
                {
                    var targetEntity = (ISqlEntityBase)rewritten[targetIdx].Value!;

                    if (targetEntity is not ISqlQueryFragment && !context.Dialect.SupportedFeatures.Contains(SqlFeature.UpdateAs)) 
                        throw new SqlDialectException($"'UPDATE' with a target table alias is not supported by {context.Dialect.Kind}.");

                    string updateText = rewritten[localUpdateIdx].Value?.ToString() ?? "";
                    int updateKeywordIdx = updateText.LastIndexOf("UPDATE", StringComparison.OrdinalIgnoreCase);
                    string prefix = updateKeywordIdx > 0 ? updateText[..updateKeywordIdx] : "";

                    rewritten.RemoveRange(localUpdateIdx, rewritten.Count - localUpdateIdx);
                    if (!string.IsNullOrEmpty(prefix)) rewritten.Add(new SqlSegment(SqlSegmentType.Literal, prefix));
                    rewritten.Add(new SqlSegment(SqlSegmentType.Raw, new SqlUpdateAsFragment(targetEntity)));
                    
                    int lookahead = i + 1;
                    while (lookahead < segments.Count && segments[lookahead]?.Type == SqlSegmentType.Literal && string.IsNullOrWhiteSpace(segments[lookahead]?.Value as string)) lookahead++;
                    if (lookahead < segments.Count) lookahead++; 
                    i = lookahead - 1;
                    continue;
                }
                else if (!context.Dialect.SupportedFeatures.Contains(SqlFeature.UpdateAs))
                {
                    throw new SqlDialectException($"'UPDATE' with a target table alias is not supported by {context.Dialect.Kind}.");
                }
            }

            if (forceBaseNamePhase)
            {
                // FIX: Respect explicitly supplied render modes (like 'Declaration' or 'AliasOnly') so AutoAliasing survives!
                if ((segment.Type == SqlSegmentType.Projection || segment.Type == SqlSegmentType.Raw) && segment.Value is ISqlProjection proj) 
                { 
                    rewritten.Add(new SqlSegment(segment.Type, proj, segment.RenderMode ?? SqlRenderMode.BaseName, segment.Tags)); 
                    continue; 
                }
                if ((segment.Type == SqlSegmentType.Reference || segment.Type == SqlSegmentType.Raw) && segment.Value is ISqlEntityBase entity) 
                { 
                    rewritten.Add(new SqlSegment(segment.Type, entity, segment.RenderMode ?? SqlRenderMode.BaseName, segment.Tags)); 
                    continue; 
                }
            }

            bool rewrittenKeyword = false;
            if (segment.Tags != null)
            {
                foreach (var tag in segment.Tags)
                {
                    if (tag == SqlSegmentTag.InsertValuesKeyword) { rewrittenKeyword = TryRewriteKeywordFragment<SqlInsertValuesFragment>("VALUES", segment, i, out i); break; }
                    if (tag == SqlSegmentTag.SetKeyword) { rewrittenKeyword = TryRewriteKeywordFragment<SqlSetFragment>("SET", segment, i, out i); break; }
                    if (tag == SqlSegmentTag.SelectKeyword) { rewrittenKeyword = TryRewriteKeywordFragment<SqlSelectFragment>("SELECT", segment, i, out i); break; }
                    if (tag == SqlSegmentTag.SelectDistinctKeyword) { rewrittenKeyword = TryRewriteKeywordFragment<SqlSelectFragment>("SELECT DISTINCT", segment, i, out i); break; }
                }
            }
            
            if (rewrittenKeyword) continue;
            rewritten.Add(segment);
        }

        return rewritten;
    }
}