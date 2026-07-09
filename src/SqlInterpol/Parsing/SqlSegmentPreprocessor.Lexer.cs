using System;
using System.Collections.Generic;

namespace SqlInterpol.Parsing;

public partial class SqlSegmentPreprocessor
{
    private void ProcessTextLiteral(SqlSegment segment, string text, IReadOnlyList<SqlSegment> segments, ref int segmentIndex, SqlPreprocessorState state)
    {
        int lastSplitIdx = 0;

        for (int j = 0; j < text.Length; j++)
        {
            char c = text[j];
            
            // 1. Fast-path state tracking to ignore keywords inside strings or comments
            if (state.InString) { if (c == '\'') { if (j + 1 < text.Length && text[j + 1] == '\'') j++; else state.InString = false; } continue; }
            if (state.InBlockCmt) { if (c == '*' && j + 1 < text.Length && text[j + 1] == '/') { state.InBlockCmt = false; j++; } continue; }
            if (state.InLineCmt) { if (c == '\n' || c == '\r') state.InLineCmt = false; continue; }
            
            if (c == '\'') { state.InString = true; continue; }
            if (c == '/' && j + 1 < text.Length && text[j + 1] == '*') { state.InBlockCmt = true; j++; continue; }
            if (c == '-' && j + 1 < text.Length && text[j + 1] == '-') { state.InLineCmt = true; j++; continue; }

            /*
             * ====================================================================
             * TIER 1 DYNAMIC BYPASS:
             * We explicitly DO NOT parse deep expressions (like '||', AS aliases, 
             * or TRUE/FALSE booleans). Any uncompiled dynamic string appended to 
             * the query simply bypasses this logic, enforcing native SQL syntax 
             * at runtime without allocation overhead.
             * ====================================================================
             */

            bool isWordBoundary = j == 0 || (!char.IsLetterOrDigit(text[j - 1]) && text[j - 1] != '_');
            
            if (isWordBoundary)
            {
                var span = text.AsSpan(j);

                // 2. Scan exclusively for Structural Block Keywords that require fragment routing
                if (MatchKeyword(span, SqlKeyword.Limit.Value))
                {
                    if (j > lastSplitIdx) state.Refined.Add(new SqlSegment(SqlSegmentType.Literal, text[lastSplitIdx..j], segment.RenderMode, segment.Tags));
                    
                    var pagingFragment = ExtractPagingFragment(text, ref j, segments, ref segmentIndex);
                    state.Refined.Add(new SqlSegment(SqlSegmentType.Fragment, pagingFragment));
                    
                    lastSplitIdx = j + 1;
                    continue;
                }
                else if (MatchKeyword(span, SqlKeyword.Returning.Value))
                {
                    if (j > lastSplitIdx) state.Refined.Add(new SqlSegment(SqlSegmentType.Literal, text[lastSplitIdx..j], segment.RenderMode, segment.Tags));
                    
                    var returningFragment = ExtractReturningFragment(text, ref j, segments, ref segmentIndex);
                    state.Refined.Add(new SqlSegment(SqlSegmentType.Fragment, returningFragment));
                    
                    lastSplitIdx = j + 1;
                    continue;
                }
                else if (MatchKeyword(span, SqlKeyword.ForUpdate.Value) || MatchKeyword(span, SqlKeyword.ForShare.Value))
                {
                    if (j > lastSplitIdx) state.Refined.Add(new SqlSegment(SqlSegmentType.Literal, text[lastSplitIdx..j], segment.RenderMode, segment.Tags));
                    
                    bool isForUpdate = MatchKeyword(span, SqlKeyword.ForUpdate.Value);
                    var lockFragment = ExtractLockFragment(text, ref j, isForUpdate, segments, ref segmentIndex);
                    state.Refined.Add(new SqlSegment(SqlSegmentType.Fragment, lockFragment));
                    
                    lastSplitIdx = j + 1;
                    continue;
                }
            }
        }

        // Flush remaining text
        if (lastSplitIdx < text.Length)
        {
            state.Refined.Add(new SqlSegment(SqlSegmentType.Literal, text[lastSplitIdx..], segment.RenderMode, segment.Tags));
        }
    }

    // ====================================================================
    // FRAGMENT EXTRACTORS (Forward-Scan only, manipulating ref indices)
    // ====================================================================

    private SqlPagingFragment ExtractPagingFragment(string text, ref int charIndex, IReadOnlyList<SqlSegment> segments, ref int segmentIndex)
    {
        charIndex += SqlKeyword.Limit.Value.Length - 1; 
        
        int limit = 0;
        int offset = 0;
        
        // Advance segmentIndex if LIMIT argument is passed as an interpolated hole
        if (segmentIndex + 1 < segments.Count && segments[segmentIndex + 1].Type == SqlSegmentType.Parameter)
        {
            segmentIndex++;
        }
        
        // Advance segmentIndex again if OFFSET argument is also passed as an interpolated hole
        if (segmentIndex + 1 < segments.Count && segments[segmentIndex + 1].Type == SqlSegmentType.Parameter)
        {
            segmentIndex++;
        }
        
        return new SqlPagingFragment(limit, offset); 
    }

    private SqlReturningFragment ExtractReturningFragment(string text, ref int charIndex, IReadOnlyList<SqlSegment> segments, ref int segmentIndex)
    {
        charIndex += SqlKeyword.Returning.Value.Length - 1;
        
        // Advance segmentIndex to extract bound return projections (e.g. Returning {projection})
        if (segmentIndex + 1 < segments.Count && segments[segmentIndex + 1].Type == SqlSegmentType.Parameter)
        {
            segmentIndex++;
        }
        
        return new SqlReturningFragment(Array.Empty<ISqlProjection>()); 
    }

    private SqlLockFragment ExtractLockFragment(string text, ref int charIndex, bool isForUpdate, IReadOnlyList<SqlSegment> segments, ref int segmentIndex)
    {
        charIndex += (isForUpdate ? SqlKeyword.ForUpdate.Value.Length : SqlKeyword.ForShare.Value.Length) - 1;
        
        // If a lock mode accepts parameters (like timeouts), advance the segmentIndex here
        if (segmentIndex + 1 < segments.Count && segments[segmentIndex + 1].Type == SqlSegmentType.Parameter)
        {
            segmentIndex++;
        }
        
        return new SqlLockFragment(isForUpdate ? SqlLockMode.Update : SqlLockMode.Share); 
    }

    // ====================================================================
    // UTILITIES
    // ====================================================================

    private static bool MatchKeyword(ReadOnlySpan<char> span, string keyword)
    {
        if (span.Length < keyword.Length) return false;
        if (!span.Slice(0, keyword.Length).Equals(keyword, StringComparison.OrdinalIgnoreCase)) return false;
        return span.Length == keyword.Length || (!char.IsLetterOrDigit(span[keyword.Length]) && span[keyword.Length] != '_');
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