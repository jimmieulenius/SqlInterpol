namespace SqlInterpol;

/// <summary>
/// High-performance shared helpers for AST segment rewriting and dialect transpilation passes.
/// </summary>
public static class SqlRewriterHelpers
{
    /// <summary>
    /// Safely extracts limit and offset nodes from a paging initiator token sequence.
    /// This zero-allocation parser supports both parameterized values and hardcoded integer literals!
    /// </summary>
    public static bool TryExtractPagingNodes(
        IReadOnlyList<SqlSegment> segments, 
        int currentIndex, 
        out SqlSegment limitNode, 
        out SqlSegment offsetNode, 
        out int nextIndex,
        out string trailingText)
    {
        limitNode = default;
        offsetNode = default;
        nextIndex = currentIndex;
        trailingText = string.Empty;

        SqlSegment? pLimit = null;
        SqlSegment? pOffset = null;
        bool expectOffset = false;

        for (int i = currentIndex + 1; i < segments.Count; i++)
        {
            var seg = segments[i];

            if (seg.Type == SqlSegmentType.Parameter)
            {
                if (expectOffset) { pOffset = seg; expectOffset = false; }
                else if (pLimit == null) pLimit = seg;
                nextIndex = i;
                continue;
            }

            if (seg.Type == SqlSegmentType.Literal && seg.Value is string text)
            {
                var span = text.AsSpan();
                int pos = 0;
                bool unrecognized = false;

                while (pos < span.Length)
                {
                    while (pos < span.Length && char.IsWhiteSpace(span[pos])) pos++;
                    if (pos >= span.Length) break; // Segment was just whitespace

                    // Look for OFFSET keyword
                    if (pos + 6 <= span.Length && span.Slice(pos, 6).Equals("OFFSET", StringComparison.OrdinalIgnoreCase))
                    {
                        bool rightBoundary = pos + 6 == span.Length || char.IsWhiteSpace(span[pos + 6]);
                        if (rightBoundary)
                        {
                            expectOffset = true;
                            pos += 6;
                            continue;
                        }
                    }

                    // Look for raw integers
                    if (char.IsDigit(span[pos]))
                    {
                        int startNum = pos;
                        while (pos < span.Length && char.IsDigit(span[pos])) pos++;
                        var numStr = span.Slice(startNum, pos - startNum).ToString();
                        
                        // Treat the extracted integer as a Raw fragment so it bypasses quoting
                        var numSeg = new SqlSegment(SqlSegmentType.Raw, new SqlRawFragment(numStr));

                        if (expectOffset) { pOffset = numSeg; expectOffset = false; }
                        else if (pLimit == null) pLimit = numSeg;

                        continue;
                    }

                    // We hit something unrecognized (e.g., semicolon, comment, another clause)
                    trailingText = span.Slice(pos).ToString();
                    unrecognized = true;
                    break;
                }

                nextIndex = i;
                if (unrecognized) break;
            }
            else
            {
                break;
            }
        }

        if (pLimit != null)
        {
            limitNode = pLimit;
            // Default offset to 0 if only LIMIT was provided
            offsetNode = pOffset ?? new SqlSegment(SqlSegmentType.Raw, new SqlRawFragment("0"));
            return true;
        }

        return false;
    }

    /// <summary>
    /// Legacy parameter-only extractor (maintained for backwards compatibility with other dialects).
    /// </summary>
    public static bool TryExtractPagingParameters(
        IReadOnlyList<SqlSegment> segments, 
        int currentIndex, 
        out SqlSegment limitParam, 
        out SqlSegment offsetParam, 
        out int nextIndex)
    {
        limitParam = default;
        offsetParam = default;
        nextIndex = currentIndex;

        int p1Idx = currentIndex + 1;
        while (p1Idx < segments.Count && segments[p1Idx].Type == SqlSegmentType.Literal && string.IsNullOrWhiteSpace(segments[p1Idx].Value as string)) p1Idx++;
        
        int p2Idx = p1Idx + 1;
        while (p2Idx < segments.Count && segments[p2Idx].Type == SqlSegmentType.Literal && string.IsNullOrWhiteSpace(segments[p2Idx].Value as string)) p2Idx++;
        
        int p3Idx = p2Idx + 1;
        while (p3Idx < segments.Count && segments[p3Idx].Type == SqlSegmentType.Literal && string.IsNullOrWhiteSpace(segments[p3Idx].Value as string)) p3Idx++;

        if (p3Idx < segments.Count && segments[p1Idx].Type == SqlSegmentType.Parameter && segments[p3Idx].Type == SqlSegmentType.Parameter)
        {
            limitParam = segments[p1Idx];
            offsetParam = segments[p3Idx];
            nextIndex = p3Idx;
            return true;
        }

        return false;
    }

    /// <summary>
    /// Evaluates whether a text segment safely contains an unquoted SQL clause phrase.
    /// </summary>
    public static bool ContainsKeyword(string? text, string keyword)
    {
        if (string.IsNullOrEmpty(text)) return false;

        int startIndex = 0;
        while ((startIndex = text.IndexOf(keyword, startIndex, StringComparison.OrdinalIgnoreCase)) >= 0)
        {
            bool leftOk = startIndex == 0 || (!char.IsLetterOrDigit(text[startIndex - 1]) && text[startIndex - 1] != '_');
            bool rightOk = startIndex + keyword.Length == text.Length || (!char.IsLetterOrDigit(text[startIndex + keyword.Length]) && text[startIndex + keyword.Length] != '_');
            
            if (leftOk && rightOk) return true;
            startIndex += keyword.Length;
        }
        return false;
    }
}