namespace SqlInterpol.Parsing;

public partial class SqlSegmentPreprocessor
{
    private void ProcessTextLiteral(SqlSegment segment, IReadOnlyList<SqlSegment> segments, int i, SqlPreprocessorState state)
    {
        string text = (string)segment.Value!;
        if (text.Contains(';')) state.ForceBaseNamePhase = false;

        bool isEligibleForAlias = state.Refined.Count > 0 && 
            (state.Refined[^1].Type == SqlSegmentType.Reference || 
             state.Refined[^1].Type == SqlSegmentType.Raw ||
             state.Refined[^1].Value is ISqlProjection ||
             state.Refined[^1].Value is ISqlFragment);

        if (isEligibleForAlias)
        {
            TryExtractInlineAlias(ref text, segment, state);
        }

        bool xvSql = state.Context.Options.CrossVendorSqlTranspilation;
        bool hasKeywordTags = state.Context.Options.KeywordTags.Count > 0;
        var keywordTagsDict = hasKeywordTags ? state.Context.Options.KeywordTags : null;

        int lastSplitIdx = 0;
        for (int j = 0; j < text.Length; j++)
        {
            char c = text[j];
            if (state.InString) { if (c == '\'') { if (j + 1 < text.Length && text[j + 1] == '\'') j++; else state.InString = false; } continue; }
            if (state.InBlockCmt) { if (c == '*' && j + 1 < text.Length && text[j + 1] == '/') { state.InBlockCmt = false; j++; } continue; }
            if (state.InLineCmt) { if (c == '\n' || c == '\r') state.InLineCmt = false; continue; }
            if (c == '\'') { state.InString = true; continue; }
            if (c == '/' && j + 1 < text.Length && text[j + 1] == '*') { state.InBlockCmt = true; j++; continue; }
            if (c == '-' && j + 1 < text.Length && text[j + 1] == '-') { state.InLineCmt = true; j++; continue; }
            
            if (c == '(') { state.PushState(); continue; }
            if (c == ')') { state.PopState(); continue; }

            // ====================================================================
            // NATIVE STRING CONCATENATION '||'
            // ====================================================================
            if (c == '|' && j + 1 < text.Length && text[j + 1] == '|')
            {
                if (j > lastSplitIdx) state.Refined.Add(new SqlSegment(SqlSegmentType.Literal, text[lastSplitIdx..j], segment.RenderMode, segment.Tags));
                state.Refined.Add(new SqlSegment(SqlSegmentType.Literal, "||", null, [SqlSegmentTag.ConcatOperator]));
                j++; 
                lastSplitIdx = j + 1;
                continue;
            }

            bool isWordBoundary = j == 0 || (!char.IsLetterOrDigit(text[j - 1]) && text[j - 1] != '_');
            if (isWordBoundary)
            {
                var span = text.AsSpan(j);
                string? matchedWord = null;
                string? targetTag = null;
                string[]? targetTags = null; 

                if (MatchKeyword(span, SqlKeyword.SelectDistinct.Value))  { matchedWord = SqlKeyword.SelectDistinct.Value; targetTag = SqlSegmentTag.SelectDistinctKeyword; }
                else if (MatchKeyword(span, SqlKeyword.Select.Value))     { matchedWord = SqlKeyword.Select.Value;   targetTag = SqlSegmentTag.SelectKeyword; }
                else if (MatchKeyword(span, SqlKeyword.Update.Value))     { matchedWord = SqlKeyword.Update.Value;   targetTag = SqlSegmentTag.UpdateKeyword; }
                else if (MatchKeyword(span, SqlKeyword.Set.Value))        { matchedWord = SqlKeyword.Set.Value;      targetTag = SqlSegmentTag.SetKeyword; }
                else if (MatchKeyword(span, SqlKeyword.Insert.Value))     { matchedWord = SqlKeyword.Insert.Value;   targetTag = SqlSegmentTag.InsertKeyword; }
                else if (MatchKeyword(span, SqlKeyword.Into.Value))       { matchedWord = SqlKeyword.Into.Value;     targetTag = SqlSegmentTag.IntoKeyword; }
                else if (MatchKeyword(span, SqlKeyword.Values.Value))     { matchedWord = SqlKeyword.Values.Value;   targetTag = SqlSegmentTag.InsertValuesKeyword; }
                else if (MatchKeyword(span, SqlKeyword.From.Value))       { matchedWord = SqlKeyword.From.Value;     targetTag = SqlSegmentTag.FromKeyword; }
                else if (MatchKeyword(span, SqlKeyword.InnerJoin.Value))  { matchedWord = SqlKeyword.InnerJoin.Value; targetTag = SqlSegmentTag.FromKeyword; }
                else if (MatchKeyword(span, SqlKeyword.LeftJoin.Value))   { matchedWord = SqlKeyword.LeftJoin.Value;  targetTag = SqlSegmentTag.FromKeyword; }
                else if (MatchKeyword(span, SqlKeyword.RightJoin.Value))  { matchedWord = SqlKeyword.RightJoin.Value; targetTag = SqlSegmentTag.FromKeyword; }
                else if (MatchKeyword(span, SqlKeyword.CrossJoin.Value))  { matchedWord = SqlKeyword.CrossJoin.Value; targetTag = SqlSegmentTag.FromKeyword; }
                else if (MatchKeyword(span, SqlKeyword.Join.Value))       { matchedWord = SqlKeyword.Join.Value;     targetTag = SqlSegmentTag.FromKeyword; }
                else if (MatchKeyword(span, SqlKeyword.Where.Value))      { matchedWord = SqlKeyword.Where.Value;    targetTag = SqlSegmentTag.WhereKeyword; }
                else if (MatchKeyword(span, SqlKeyword.OrderBy.Value))    { matchedWord = SqlKeyword.OrderBy.Value;   targetTag = SqlSegmentTag.WhereKeyword; } 
                else if (MatchKeyword(span, SqlKeyword.GroupBy.Value))    { matchedWord = SqlKeyword.GroupBy.Value;   targetTag = SqlSegmentTag.WhereKeyword; }
                else if (MatchKeyword(span, SqlKeyword.Having.Value))     { matchedWord = SqlKeyword.Having.Value;   targetTag = SqlSegmentTag.WhereKeyword; }
                else if (MatchKeyword(span, SqlKeyword.Delete.Value))     { matchedWord = SqlKeyword.Delete.Value;   targetTag = SqlSegmentTag.DeleteKeyword; }
                
                // ====================================================================
                // NATIVE BOOLEAN TRANSPILATION
                // ====================================================================
                else if (MatchKeyword(span, "TRUE"))                      { matchedWord = "TRUE";                    targetTag = SqlSegmentTag.TrueKeyword; }
                else if (MatchKeyword(span, "FALSE"))                     { matchedWord = "FALSE";                   targetTag = SqlSegmentTag.FalseKeyword; }

                else if (xvSql && MatchKeyword(span, SqlKeyword.Limit.Value))      { matchedWord = SqlKeyword.Limit.Value;    targetTag = SqlSegmentTag.Paging; }
                else if (xvSql && MatchKeyword(span, SqlKeyword.Returning.Value))  { matchedWord = SqlKeyword.Returning.Value;targetTag = SqlSegmentTag.ReturningKeyword; }
                else if (xvSql && MatchKeyword(span, SqlKeyword.ForUpdate.Value))  { matchedWord = SqlKeyword.ForUpdate.Value;  targetTag = SqlSegmentTag.ForUpdateKeyword; }
                else if (xvSql && MatchKeyword(span, SqlKeyword.ForShare.Value))   { matchedWord = SqlKeyword.ForShare.Value;   targetTag = SqlSegmentTag.ForShareKeyword; }
                
                else if (MatchKeyword(span, "WITH RECURSIVE"))
                {
                    matchedWord = "WITH RECURSIVE";
                    if (string.IsNullOrWhiteSpace(text.Substring(j + matchedWord.Length)))
                    {
                        int forward = i + 1;
                        while (forward < segments.Count)
                        {
                            if (segments[forward].Value is ISqlRoleable roleableTarget) { roleableTarget.Role = SqlEntityRole.Cte; break; }
                            if (segments[forward].Type == SqlSegmentType.Literal && !string.IsNullOrWhiteSpace(segments[forward].Value?.ToString())) break; 
                            forward++;
                        }
                    }
                }
                else if (MatchKeyword(span, SqlKeyword.With.Value))
                {
                    matchedWord = SqlKeyword.With.Value;
                    if (string.IsNullOrWhiteSpace(text.Substring(j + matchedWord.Length)))
                    {
                        int forward = i + 1;
                        while (forward < segments.Count)
                        {
                            if (segments[forward].Value is ISqlRoleable roleableTarget) { roleableTarget.Role = SqlEntityRole.Cte; break; }
                            if (segments[forward].Type == SqlSegmentType.Literal && !string.IsNullOrWhiteSpace(segments[forward].Value?.ToString())) break; 
                            forward++;
                        }
                    }
                }
                else if (hasKeywordTags)
                {
                    int wordLen = 0;
                    while (wordLen < span.Length && (char.IsLetterOrDigit(span[wordLen]) || span[wordLen] == '_')) wordLen++;
                    
                    if (wordLen > 0)
                    {
                        var currentWord = span.Slice(0, wordLen).ToString();
                        if (keywordTagsDict!.TryGetValue(currentWord, out targetTags))
                        {
                            matchedWord = currentWord;
                        }
                    }
                }

                if (matchedWord != null)
                {
                    if (targetTag == SqlSegmentTag.InsertKeyword || targetTag == SqlSegmentTag.ReturningKeyword) state.ForceBaseNamePhase = true;
                    else if (targetTag == SqlSegmentTag.SelectKeyword || targetTag == SqlSegmentTag.SelectDistinctKeyword || 
                             targetTag == SqlSegmentTag.UpdateKeyword || targetTag == SqlSegmentTag.DeleteKeyword || 
                             targetTag == SqlSegmentTag.InsertValuesKeyword || targetTag == SqlSegmentTag.IntoKeyword || 
                             targetTag == SqlSegmentTag.SetKeyword || targetTag == SqlSegmentTag.WhereKeyword) state.ForceBaseNamePhase = false;

                    if (j > lastSplitIdx) state.Refined.Add(new SqlSegment(SqlSegmentType.Literal, text[lastSplitIdx..j], segment.RenderMode, segment.Tags));
                    
                    if (targetTags != null)
                    {
                        state.Refined.Add(new SqlSegment(SqlSegmentType.Literal, text.Substring(j, matchedWord.Length), null, targetTags));
                    }
                    else
                    {
                        // CRITICAL FIX: Ensure Native Operators bypass parenthesis stripping!
                        bool isOperator = targetTag == SqlSegmentTag.TrueKeyword || 
                                          targetTag == SqlSegmentTag.FalseKeyword || 
                                          targetTag == SqlSegmentTag.ConcatOperator;

                        var appliedTags = targetTag != null && (state.ParenDepth == 0 || isOperator) ? new[] { targetTag } : [];
                        state.Refined.Add(new SqlSegment(SqlSegmentType.Literal, text.Substring(j, matchedWord.Length), null, appliedTags));
                    }
                    
                    state.CurrentKeyword = matchedWord;
                    state.CurrentClauseTag = targetTag;
                    
                    if (matchedWord == SqlKeyword.From.Value) state.FromCount++;
                    else if (
                        matchedWord == SqlKeyword.Update.Value || 
                        matchedWord == SqlKeyword.Delete.Value || 
                        matchedWord == SqlKeyword.Select.Value || 
                        matchedWord == SqlKeyword.SelectDistinct.Value || 
                        matchedWord == SqlKeyword.Insert.Value) 
                    {
                        state.ActiveDmlKeyword = matchedWord;
                        state.FromCount = 0;
                    }

                    j += matchedWord.Length - 1; 
                    lastSplitIdx = j + 1;
                }
            }
        }

        if (lastSplitIdx < text.Length)
        {
            var chunk = text[lastSplitIdx..];
            var trimmedText = chunk.TrimEnd();
            if (trimmedText.EndsWith("AS", StringComparison.OrdinalIgnoreCase))
            {
                state.ExpectsAlias = trimmedText.Length == 2 || (!char.IsLetterOrDigit(trimmedText[^3]) && trimmedText[^3] != '_');
                if (state.ExpectsAlias)
                {
                    int asIdx = chunk.LastIndexOf("AS", StringComparison.OrdinalIgnoreCase);
                    string prefix = chunk[..asIdx];
                    if (prefix.Length > 0) state.Refined.Add(new SqlSegment(SqlSegmentType.Literal, prefix, segment.RenderMode, segment.Tags));

                    string determinedTag = DetermineAliasTag(state);
                    string asKeyword = chunk.Substring(asIdx, 2);
                    state.Refined.Add(new SqlSegment(SqlSegmentType.Literal, asKeyword, segment.RenderMode, determinedTag == "ColumnAliasAsKeyword" ? segment.Tags : [determinedTag]));
                    
                    string afterAs = chunk[(asIdx + 2)..];
                    if (afterAs.Length > 0) state.Refined.Add(new SqlSegment(SqlSegmentType.Literal, afterAs, segment.RenderMode, segment.Tags));
                }
                else state.Refined.Add(new SqlSegment(SqlSegmentType.Literal, chunk, segment.RenderMode, segment.Tags));
            }
            else state.Refined.Add(new SqlSegment(SqlSegmentType.Literal, chunk, segment.RenderMode, segment.Tags));
        }
    }

    public static string SafeReplaceKeyword(string sql, string target, string replacement)
    {
        if (string.IsNullOrEmpty(sql) || sql.IndexOf(target, StringComparison.OrdinalIgnoreCase) == -1) 
            return sql;
            
        var sb = new System.Text.StringBuilder(sql.Length);
        bool inString = false, inLineCmt = false, inBlockCmt = false;
        int targetLen = target.Length;

        for (int i = 0; i < sql.Length; i++)
        {
            char c = sql[i];
            
            if (inString) { if (c == '\'') { if (i + 1 < sql.Length && sql[i + 1] == '\'') { sb.Append("''"); i++; continue; } else inString = false; } sb.Append(c); continue; }
            if (inBlockCmt) { if (c == '*' && i + 1 < sql.Length && sql[i + 1] == '/') { inBlockCmt = false; sb.Append("*/"); i++; continue; } sb.Append(c); continue; }
            if (inLineCmt) { if (c == '\n' || c == '\r') inLineCmt = false; sb.Append(c); continue; }
            
            if (c == '\'') { inString = true; sb.Append(c); continue; }
            if (c == '/' && i + 1 < sql.Length && sql[i + 1] == '*') { inBlockCmt = true; sb.Append("/*"); i++; continue; }
            if (c == '-' && i + 1 < sql.Length && sql[i + 1] == '-') { inLineCmt = true; sb.Append("--"); i++; continue; }

            if (char.ToUpperInvariant(c) == char.ToUpperInvariant(target[0]))
            {
                if (i + targetLen <= sql.Length && sql.Substring(i, targetLen).Equals(target, StringComparison.OrdinalIgnoreCase))
                {
                    bool wordStart = i == 0 || (!char.IsLetterOrDigit(sql[i - 1]) && sql[i - 1] != '_');
                    bool wordEnd = (i + targetLen == sql.Length) || (!char.IsLetterOrDigit(sql[i + targetLen]) && sql[i + targetLen] != '_');
                    
                    if (wordStart && wordEnd)
                    {
                        sb.Append(replacement);
                        i += targetLen - 1; 
                        continue;
                    }
                }
            }
            sb.Append(c);
        }
        return sb.ToString();
    }
}