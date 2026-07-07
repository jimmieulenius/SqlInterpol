// using System.Collections.Frozen;

// namespace SqlInterpol.Parsing;

// public partial class SqlSegmentPreprocessor
// {
//     // =====================================================================
//     // GLOBAL RESERVED WORDS & BOUNDARIES
//     // =====================================================================
//     private static readonly FrozenSet<string> ReservedKeywords = SqlKeyword.AllKeywords
//         .SelectMany(k => k.Value.Split(' ', StringSplitOptions.RemoveEmptyEntries))
//         .Concat(new[] { "OVER", "WITH", "WINDOW", "AND", "OR", "IN", "IS", "NOT", "LIKE", "ASC", "DESC" })
//         .ToFrozenSet(StringComparer.OrdinalIgnoreCase);

//     private static bool MatchKeyword(ReadOnlySpan<char> s, string kw) => 
//         s.StartsWith(kw, StringComparison.OrdinalIgnoreCase) && 
//         (s.Length == kw.Length || (!char.IsLetterOrDigit(s[kw.Length]) && s[kw.Length] != '_'));

//     private static bool ContainsKeyword(string text, string keyword)
//     {
//         int startIndex = 0;
//         while ((startIndex = text.IndexOf(keyword, startIndex, StringComparison.OrdinalIgnoreCase)) >= 0)
//         {
//             bool leftOk = startIndex == 0 || (!char.IsLetterOrDigit(text[startIndex - 1]) && text[startIndex - 1] != '_');
//             bool rightOk = startIndex + keyword.Length == text.Length || (!char.IsLetterOrDigit(text[startIndex + keyword.Length]) && text[startIndex + keyword.Length] != '_');
            
//             if (leftOk && rightOk) return true;
//             startIndex += keyword.Length;
//         }
//         return false;
//     }

//     // =========================================================================
//     // HIGH-PERFORMANCE ZERO-ALLOCATION ALIAS STATE MACHINE
//     // =========================================================================
//     private ref struct AliasParseResult
//     {
//         public bool Success;
//         public bool IsExplicit;
//         public ReadOnlySpan<char> Identifier;
//         public int PrefixLength;
//         public int IdentifierStart;
//         public int IdentifierLength;
//         public int TotalLength;
//     }

//     private static bool TryParseAlias(ReadOnlySpan<char> span, out AliasParseResult result)
//     {
//         result = default;
//         if (span.IsEmpty) return false;

//         int idx = 0;
//         bool endsWithWhitespace = false;

//         // Consume leading whitespaces and closing parentheses
//         while (idx < span.Length)
//         {
//             char c = span[idx];
//             if (char.IsWhiteSpace(c))
//             {
//                 endsWithWhitespace = true;
//                 idx++;
//             }
//             else if (c == ')')
//             {
//                 endsWithWhitespace = false;
//                 idx++;
//             }
//             else
//             {
//                 break;
//             }
//         }

//         int prefixLen = idx;

//         // Check for explicit "AS" keyword
//         bool isExplicit = false;
//         int afterAsIdx = prefixLen;

//         if (prefixLen + 2 <= span.Length && 
//             (span[prefixLen] == 'A' || span[prefixLen] == 'a') && 
//             (span[prefixLen + 1] == 'S' || span[prefixLen + 1] == 's'))
//         {
//             if (prefixLen + 3 <= span.Length && char.IsWhiteSpace(span[prefixLen + 2]))
//             {
//                 isExplicit = true;
//                 afterAsIdx = prefixLen + 2;
//                 while (afterAsIdx < span.Length && char.IsWhiteSpace(span[afterAsIdx]))
//                 {
//                     afterAsIdx++;
//                 }
//             }
//         }

//         if (!isExplicit && !endsWithWhitespace) return false;

//         int idStart = isExplicit ? afterAsIdx : prefixLen;
//         int idIdx = idStart;
//         while (idIdx < span.Length && (char.IsLetterOrDigit(span[idIdx]) || span[idIdx] == '_'))
//         {
//             idIdx++;
//         }

//         if (idIdx == idStart) return false;

//         var identifier = span[idStart..idIdx];

//         if (!isExplicit)
//         {
//             string id = new string(identifier);
//             if (ReservedKeywords.Contains(id)) return false;
//         }

//         // Direct construction bypasses all primary constructor capture issues entirely!
//         result = new AliasParseResult
//         {
//             Success = true,
//             IsExplicit = isExplicit,
//             Identifier = identifier,
//             PrefixLength = prefixLen,
//             IdentifierStart = idStart,
//             IdentifierLength = idIdx - idStart,
//             TotalLength = idIdx
//         };
//         return true;
//     }
// }