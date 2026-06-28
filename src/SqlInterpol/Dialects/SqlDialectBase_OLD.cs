// using SqlInterpol.Parsing;

// namespace SqlInterpol.Dialects;

// /// <summary>
// /// Abstract base class for all SQL dialect implementations, providing ANSI-compatible
// /// identifier quoting, parameter naming, expression context detection, and multi-pass
// /// segment rewriting for DML transformations.
// /// </summary>
// public abstract class SqlDialectBase : ISqlDialect
// {
//     /// <inheritdoc />
//     public abstract SqlDialectKind Kind { get; }
//     /// <inheritdoc />
//     public abstract string OpenQuote { get; }
//     /// <inheritdoc />
//     public abstract string CloseQuote { get; }
//     /// <inheritdoc />
//     public abstract string ParameterPrefix { get; }

//     protected static readonly string[] DefaultExpressionSymbols = 
//     [
//         "=", "<", ">", "<=", ">=", "<>", "!=", "+", "-", "*", "/", "%"
//     ];

//     protected static readonly string[] DefaultExpressionKeywords = 
//     [
//         SqlKeyword.In,
//         SqlKeyword.Exists,
//         SqlKeyword.Any,
//         SqlKeyword.All,
//         SqlKeyword.Some
//     ];

//     /// <inheritdoc />
//     public virtual IReadOnlySet<SqlFeature> SupportedFeatures { get; } = new HashSet<SqlFeature>();

//     /// <inheritdoc />
//     public virtual int QueryParametersMaxCount => 999;

//     /// <inheritdoc />
//     public virtual string QuoteIdentifier(string name)
//     {
//         if (string.IsNullOrWhiteSpace(name)) return name;
//         var trimmed = name.Trim();
//         if (trimmed.Length < 2 || !trimmed.StartsWith(OpenQuote) || !trimmed.EndsWith(CloseQuote))
//         {
//             return $"{OpenQuote}{trimmed}{CloseQuote}";
//         }
//         return trimmed;
//     }

//     /// <inheritdoc />
//     public virtual string UnquoteIdentifier(string identifier)
//     {
//         if (string.IsNullOrEmpty(identifier)) return identifier;
//         string open = OpenQuote;
//         string close = CloseQuote;
//         if (identifier.StartsWith(open) && identifier.EndsWith(close))
//         {
//             return identifier.Substring(open.Length, identifier.Length - open.Length - close.Length);
//         }
//         return identifier;
//     }

//     /// <inheritdoc />
//     public virtual string QuoteEntityName(string table, string? schema = null)
//     {
//         var quotedTable = QuoteIdentifier(table);
//         if (string.IsNullOrWhiteSpace(schema)) return quotedTable;
//         return $"{QuoteIdentifier(schema)}.{quotedTable}";
//     }

//     /// <inheritdoc />
//     public virtual string GetParameterName(int index) => $"{ParameterPrefix}{index}";

//     /// <inheritdoc />
//     public virtual bool IsExpressionContext(string textBeforeParen)
//     {
//         if (string.IsNullOrWhiteSpace(textBeforeParen)) return false;

//         foreach (var symbol in DefaultExpressionSymbols)
//         {
//             if (textBeforeParen.EndsWith(symbol)) return true;
//         }

//         int lastSeparator = textBeforeParen.LastIndexOfAny([' ', '\t', '\n', '\r', '(']);
//         string lastWord = lastSeparator >= 0 ? textBeforeParen[(lastSeparator + 1)..] : textBeforeParen;

//         foreach (var keyword in DefaultExpressionKeywords)
//         {
//             if (lastWord.Equals(keyword, StringComparison.OrdinalIgnoreCase)) return true;
//         }

//         return false;
//     }

//     /// <inheritdoc />
//     public string ApplyAlias(string source, string? alias = null)
//     {
//         if (string.IsNullOrWhiteSpace(alias)) return source;
//         return $"{QuoteIdentifier(source)} {SqlKeyword.As.Value} {alias}";
//     }

//     /// <inheritdoc />
//     public virtual SqlInterpolOptions GetDefaultOptions() => new();

//     /// <inheritdoc />
//     public virtual string RenderFragment(ISqlFragment fragment, ISqlContext context)
//     {
//         return fragment switch
//         {
//             SqlDeleteAsFragment delAs => RenderDeleteAs(delAs, context),
//             SqlUpdateAsFragment upAs => RenderUpdateAs(upAs, context),
//             SqlPagingFragment p => $"{SqlKeyword.Limit} {p.Limit} {SqlKeyword.Offset} {p.Offset}",
//             SqlSetOperationFragment setOp => RenderSetOperation(setOp, context),
//             SqlMultiTableUpdateFragment update => RenderMultiTableUpdate(update, context),
//             SqlMultiTableDeleteFragment delete => RenderMultiTableDelete(delete, context),
//             SqlSelectIntoFragment selectInto => RenderSelectInto(selectInto, context),
//             SqlUpdateSubqueryFragment upSub => RenderUpdateSubquery(upSub, context),
//             SqlUpdateCteFragment upCte => RenderUpdateCte(upCte, context),
//             _ => throw new NotSupportedException($"The fragment type '{fragment.GetType().Name}' is not supported by {this.GetType().Name}.")
//         };
//     }

//     /// <inheritdoc />
//     public virtual IEnumerable<SqlSegment> RewriteSegments(IReadOnlyList<SqlSegment> segments)
//     {
//         // =========================================================================
//         // STEP 1: PRE-PASS KEYWORD SPLITTING (Paren-Depth Shielded Context Isolator)
//         // =========================================================================
//         var splitSegments = new List<SqlSegment>();
//         int currentParenDepth = 0;
//         bool inStr = false, inLineCmt = false, inBlockCmt = false;

//         for (int i = 0; i < segments.Count; i++)
//         {
//             var segment = segments[i];
//             if (segment.Type != SqlSegmentType.Literal || segment.Value is not string text)
//             {
//                 splitSegments.Add(segment);
//                 continue;
//             }

//             int lastSplitIdx = 0;
//             for (int j = 0; j < text.Length; j++)
//             {
//                 char c = text[j];
//                 if (c == '\'' && !inBlockCmt && !inLineCmt) { if (inStr && j + 1 < text.Length && text[j + 1] == '\'') { j++; continue; } inStr = !inStr; continue; }
//                 if (inStr) continue;
//                 if (!inLineCmt && c == '/' && j + 1 < text.Length && text[j + 1] == '*') { inBlockCmt = true; j++; continue; }
//                 if (inBlockCmt && c == '*' && j + 1 < text.Length && text[j + 1] == '/') { inBlockCmt = false; j++; continue; }
//                 if (inBlockCmt) continue;
//                 if (!inBlockCmt && c == '-' && j + 1 < text.Length && text[j + 1] == '-') { inLineCmt = true; j++; continue; }
//                 if (inLineCmt && (c == '\n' || c == '\r')) { inLineCmt = false; continue; }
//                 if (inLineCmt) continue;

//                 if (c == '(') currentParenDepth++;
//                 else if (c == ')') currentParenDepth--;

//                 if (currentParenDepth == 0 && (j == 0 || !char.IsLetterOrDigit(text[j - 1])))
//                 {
//                     var span = text.AsSpan(j);
//                     string? matchedKeyword = null;
//                     if (span.StartsWith("UPDATE", StringComparison.OrdinalIgnoreCase) && (span.Length == 6 || !char.IsLetterOrDigit(span[6]))) matchedKeyword = "UPDATE";
//                     else if (span.StartsWith("SET", StringComparison.OrdinalIgnoreCase) && (span.Length == 3 || !char.IsLetterOrDigit(span[3]))) matchedKeyword = "SET";
//                     else if (span.StartsWith("FROM", StringComparison.OrdinalIgnoreCase) && (span.Length == 4 || !char.IsLetterOrDigit(span[4]))) matchedKeyword = "FROM";
//                     else if (span.StartsWith("WHERE", StringComparison.OrdinalIgnoreCase) && (span.Length == 5 || !char.IsLetterOrDigit(span[5]))) matchedKeyword = "WHERE";
//                     else if (span.StartsWith("DELETE", StringComparison.OrdinalIgnoreCase) && (span.Length == 6 || !char.IsLetterOrDigit(span[6]))) matchedKeyword = "DELETE";

//                     if (matchedKeyword != null && j > lastSplitIdx)
//                     {
//                         splitSegments.Add(new SqlSegment(SqlSegmentType.Literal, text[lastSplitIdx..j], null, segment.Tag));
//                         lastSplitIdx = j;
//                     }
//                 }
//             }
//             if (lastSplitIdx < text.Length)
//             {
//                 splitSegments.Add(new SqlSegment(SqlSegmentType.Literal, text[lastSplitIdx..], null, segment.Tag));
//             }
//         }

//         // =========================================================================
//         // STEP 2: METADATA TAG TRANSLATION PASS
//         // =========================================================================
//         var rewritten = new List<SqlSegment>(splitSegments.Count);
//         bool forceBaseNamePhase = false;
//         int parenDepth = 0; bool inString = false, inLineComment = false, inBlockComment = false;

//         bool TryRewriteKeywordFragment<T>(string keyword, SqlSegment segment, int index) where T : ISqlFragment
//         {
//             if (index + 1 < splitSegments.Count && splitSegments[index + 1].Value is T)
//             {
//                 if (segment.Value is string text)
//                 {
//                     int keywordIndex = text.LastIndexOf(keyword, StringComparison.OrdinalIgnoreCase);
//                     if (keywordIndex > -1)
//                     {
//                         rewritten.Add(new SqlSegment(SqlSegmentType.Literal, text[..keywordIndex]));
//                         return true;
//                     }
//                 }
//                 rewritten.Add(new SqlSegment(SqlSegmentType.Literal, " "));
//                 return true; 
//             }
//             return false;
//         }

//         for (int i = 0; i < splitSegments.Count; i++)
//         {
//             var segment = splitSegments[i];

//             if (segment.Type == SqlSegmentType.Literal && segment.Value is string litText)
//             {
//                 for (int j = 0; j < litText.Length; j++)
//                 {
//                     char c = litText[j];
//                     if (c == '\'' && !inBlockComment && !inLineComment) { if (inString && j + 1 < litText.Length && litText[j + 1] == '\'') { j++; continue; } inString = !inString; continue; }
//                     if (inString) continue;
//                     if (!inLineComment && c == '/' && j + 1 < litText.Length && litText[j + 1] == '*') { inBlockComment = true; j++; continue; }
//                     if (inBlockComment && c == '*' && j + 1 < litText.Length && litText[j + 1] == '/') { inBlockComment = false; j++; continue; }
//                     if (inBlockComment) continue;
//                     if (!inBlockComment && c == '-' && j + 1 < litText.Length && litText[j + 1] == '-') { inLineComment = true; j++; continue; }
//                     if (inLineComment && (c == '\n' || c == '\r')) { inLineComment = false; continue; }
//                     if (inLineComment) continue;

//                     if (c == '(') parenDepth++;
//                     else if (c == ')') parenDepth--;
//                     else if (c == ';' && parenDepth == 0) forceBaseNamePhase = false; 
//                 }
//             }

//             var keywordMeta = SqlKeyword.AllKeywords.FirstOrDefault(k => segment.Tag != null && segment.Tag.StartsWith(k.Value, StringComparison.OrdinalIgnoreCase));
//             if (keywordMeta?.ExpectsBaseName.HasValue == true) forceBaseNamePhase = keywordMeta.ExpectsBaseName.Value;
            
//             if (segment.Tag == SqlSegmentTag.OnConflictKeyword || (segment.Type == SqlSegmentType.Literal && segment.Value is string s1 && s1.Contains("ON CONFLICT", StringComparison.OrdinalIgnoreCase)))
//             {
//                 forceBaseNamePhase = true;
//                 if (segment.Value is string text)
//                 {
//                     string clean = text;
//                     if (segment.Tag == SqlSegmentTag.OnConflictKeyword)
//                     {
//                         clean = text.EndsWith(" ") ? text[..^1] : text;
//                         if (!clean.EndsWith('(')) clean += " (";
//                     }
//                     rewritten.Add(new SqlSegment(SqlSegmentType.Literal, clean));
//                     continue;
//                 }
//             }
//             else if (segment.Tag == SqlSegmentTag.DoUpdateSetKeyword || (segment.Type == SqlSegmentType.Literal && segment.Value is string s2 && s2.Contains("DO UPDATE SET", StringComparison.OrdinalIgnoreCase)))
//             {
//                 forceBaseNamePhase = false; 
//                 if (segment.Value is string text)
//                 {
//                     string clean = text;
//                     if (segment.Tag == SqlSegmentTag.DoUpdateSetKeyword)
//                     {
//                         if (!clean.TrimStart().StartsWith(')')) clean = ")\n" + clean.TrimStart();
//                     }
                    
//                     int keywordIndex = clean.LastIndexOf(SqlKeyword.Set, StringComparison.OrdinalIgnoreCase);
//                     if (keywordIndex > -1 && i + 1 < splitSegments.Count && splitSegments[i + 1].Value is SqlSetFragment)
//                     {
//                         rewritten.Add(new SqlSegment(SqlSegmentType.Literal, clean[..keywordIndex]));
//                         continue;
//                     }
//                     rewritten.Add(new SqlSegment(SqlSegmentType.Literal, clean));
//                     continue;
//                 }
//             }
//             else if (segment.Tag == SqlSegmentTag.ForUpdateKeyword && segment.Value is string updateText)
//             {
//                 int idx = updateText.IndexOf("FOR UPDATE", StringComparison.OrdinalIgnoreCase);
//                 if (idx > -1)
//                 {
//                     rewritten.Add(new SqlSegment(SqlSegmentType.Literal, updateText[..idx].TrimEnd(' ', '\t')));
//                     rewritten.Add(new SqlSegment(SqlSegmentType.Raw, new SqlLockFragment(SqlLockMode.Update)));
//                     rewritten.Add(new SqlSegment(SqlSegmentType.Literal, updateText[(idx + 10)..]));
//                     continue;
//                 }
//             }
//             else if (segment.Tag == SqlSegmentTag.ForShareKeyword && segment.Value is string shareText)
//             {
//                 int idx = shareText.IndexOf("FOR SHARE", StringComparison.OrdinalIgnoreCase);
//                 if (idx > -1)
//                 {
//                     rewritten.Add(new SqlSegment(SqlSegmentType.Literal, shareText[..idx].TrimEnd(' ', '\t')));
//                     rewritten.Add(new SqlSegment(SqlSegmentType.Raw, new SqlLockFragment(SqlLockMode.Share)));
//                     rewritten.Add(new SqlSegment(SqlSegmentType.Literal, shareText[(idx + 9)..]));
//                     continue;
//                 }
//             }
//             else if (segment.Tag == SqlSegmentTag.ExceptKeyword || segment.Tag == SqlSegmentTag.IntersectKeyword || segment.Tag == SqlSegmentTag.UnionKeyword || segment.Tag == SqlSegmentTag.UnionAllKeyword)
//             {
//                 var op = segment.Tag switch
//                 {
//                     SqlSegmentTag.ExceptKeyword => SqlSetOperator.Except,
//                     SqlSegmentTag.IntersectKeyword => SqlSetOperator.Intersect,
//                     SqlSegmentTag.UnionAllKeyword => SqlSetOperator.UnionAll,
//                     _ => SqlSetOperator.Union
//                 };

//                 if (rewritten.Count > 0 && rewritten[^1].Value is ISqlFragment left && i + 1 < splitSegments.Count && splitSegments[i + 1].Value is ISqlFragment right)
//                 {
//                     var fragment = new SqlSetOperationFragment(left, right, op);
//                     rewritten[^1] = new SqlSegment(SqlSegmentType.Raw, fragment);
//                     i++; 
//                     continue; 
//                 }
//             }

//             if (segment.Tag == SqlSegmentTag.DeleteAsKeyword)
//             {
//                 if (!SupportedFeatures.Contains(SqlFeature.DeleteAs))
//                     throw new SqlDialectException($"'DELETE' with a target table alias is not supported by {Kind}.");

//                 int delIdx = -1;
//                 for (int k = rewritten.Count - 1; k >= 0; k--)
//                 {
//                     if (rewritten[k].Tag == SqlSegmentTag.DeleteKeyword) { delIdx = k; break; }
//                 }

//                 int targetIdx = -1;
//                 for (int k = rewritten.Count - 1; k > delIdx; k--)
//                 {
//                     if (rewritten[k].Type == SqlSegmentType.Reference && rewritten[k].Value is ISqlEntityBase) { targetIdx = k; break; }
//                 }

//                 if (delIdx > -1 && targetIdx > -1)
//                 {
//                     var targetEntity = (ISqlEntityBase)rewritten[targetIdx].Value!;
//                     var frag = new SqlDeleteAsFragment(targetEntity);

//                     string delText = rewritten[delIdx].Value?.ToString() ?? "";
//                     int deleteKeywordIdx = delText.LastIndexOf("DELETE", StringComparison.OrdinalIgnoreCase);
//                     string prefix = deleteKeywordIdx > 0 ? delText[..deleteKeywordIdx] : "";

//                     rewritten.RemoveRange(delIdx, rewritten.Count - delIdx);
//                     if (!string.IsNullOrEmpty(prefix)) rewritten.Add(new SqlSegment(SqlSegmentType.Literal, prefix));
//                     rewritten.Add(new SqlSegment(SqlSegmentType.Raw, frag));
//                 }
//             }

//             if (segment.Tag == SqlSegmentTag.UpdateAsKeyword)
//             {
//                 if (!SupportedFeatures.Contains(SqlFeature.UpdateAs))
//                     throw new SqlDialectException($"'UPDATE' with a target table alias is not supported by {Kind}.");

//                 int localUpdateIdx = -1; 
//                 for (int k = rewritten.Count - 1; k >= 0; k--)
//                 {
//                     if (rewritten[k].Tag == SqlSegmentTag.UpdateKeyword) { localUpdateIdx = k; break; }
//                 }

//                 int targetIdx = -1;
//                 for (int k = rewritten.Count - 1; k > localUpdateIdx; k--)
//                 {
//                     if (rewritten[k].Type == SqlSegmentType.Reference && rewritten[k].Value is ISqlEntityBase) { targetIdx = k; break; }
//                 }

//                 if (localUpdateIdx > -1 && targetIdx > -1)
//                 {
//                     var targetEntity = (ISqlEntityBase)rewritten[targetIdx].Value!;
//                     var frag = new SqlUpdateAsFragment(targetEntity);

//                     string updateText = rewritten[localUpdateIdx].Value?.ToString() ?? "";
//                     int updateKeywordIdx = updateText.LastIndexOf("UPDATE", StringComparison.OrdinalIgnoreCase);
//                     string prefix = updateKeywordIdx > 0 ? updateText[..updateKeywordIdx] : "";

//                     rewritten.RemoveRange(localUpdateIdx, rewritten.Count - localUpdateIdx);
//                     if (!string.IsNullOrEmpty(prefix)) rewritten.Add(new SqlSegment(SqlSegmentType.Literal, prefix));
//                     rewritten.Add(new SqlSegment(SqlSegmentType.Raw, frag));
//                 }
//             }

//             if (forceBaseNamePhase)
//             {
//                 if (segment.Type == SqlSegmentType.Projection && segment.Value is ISqlProjection proj)
//                 {
//                     rewritten.Add(new SqlSegment(SqlSegmentType.Projection, proj, SqlRenderMode.BaseName));
//                     continue;
//                 }
//                 if (segment.Type == SqlSegmentType.Reference && segment.Value is ISqlEntityBase entity)
//                 {
//                     rewritten.Add(new SqlSegment(SqlSegmentType.Reference, entity, SqlRenderMode.BaseName));
//                     continue;
//                 }
//             }

//             switch (segment.Tag)
//             {
//                 case SqlSegmentTag.InsertValuesKeyword:
//                     if (TryRewriteKeywordFragment<SqlInsertValuesFragment>(SqlKeyword.Values, segment, i)) continue;
//                     break;
//                 case SqlSegmentTag.SetKeyword:
//                     if (TryRewriteKeywordFragment<SqlSetFragment>(SqlKeyword.Set, segment, i)) continue;
//                     break;
//                 case SqlSegmentTag.SelectKeyword:
//                     if (TryRewriteKeywordFragment<SqlSelectFragment>(SqlKeyword.Select, segment, i)) continue;
//                     break;
//                 case SqlSegmentTag.SelectDistinctKeyword:
//                     if (TryRewriteKeywordFragment<SqlSelectFragment>($"{SqlKeyword.Select} {SqlKeyword.Distinct}", segment, i)) continue;
//                     break;
//             }

//             rewritten.Add(segment);
//         }

//         // =========================================================================
//         // STEP 3: SELECT INTO EARLY EXTRACTION ROUTE
//         // =========================================================================
//         int intoIdx = -1;
//         for (int i = 0; i < rewritten.Count; i++)
//         {
//             if (rewritten[i].Tag == SqlSegmentTag.SelectIntoKeyword) { intoIdx = i; break; }
//         }

//         if (intoIdx >= 0)
//         {
//             int selectIdx = -1;
//             for (int i = intoIdx - 1; i >= 0; i--)
//             {
//                 if (rewritten[i].Tag == SqlSegmentTag.SelectKeyword || rewritten[i].Tag == SqlSegmentTag.SelectDistinctKeyword)
//                 {
//                     selectIdx = i; break;
//                 }
//             }

//             if (selectIdx >= 0)
//             {
//                 if (!SupportedFeatures.Contains(SqlFeature.SelectInto))
//                     throw new SqlDialectException("'SELECT INTO' is not supported by this dialect. Use an explicit CREATE TABLE and INSERT statement.");

//                 var intoSegment = rewritten[intoIdx];
//                 var text = intoSegment.Value as string ?? "";
//                 int keywordPos = text.IndexOf("INTO", StringComparison.OrdinalIgnoreCase);
//                 string leftOfInto = keywordPos >= 0 ? text[..keywordPos] : "";
//                 string rightOfInto = keywordPos >= 0 ? text[(keywordPos + 4)..] : text;
                
//                 object? targetTable = null;
//                 SqlSegment? targetParamSegment = null;

//                 var rightTrimmed = rightOfInto.TrimStart();
//                 if (rightTrimmed.Length > 0 && rightTrimmed[0] != '\n' && rightTrimmed[0] != '\r') 
//                 {
//                     int endOfTarget = 0;
//                     while (endOfTarget < rightTrimmed.Length && !char.IsWhiteSpace(rightTrimmed[endOfTarget])) endOfTarget++;
//                     targetTable = rightTrimmed[..endOfTarget];
//                     rightOfInto = rightTrimmed[endOfTarget..];
//                 }
//                 else if (intoIdx + 1 < rewritten.Count && rewritten[intoIdx + 1].Type != SqlSegmentType.Literal)
//                 {
//                     targetParamSegment = rewritten[intoIdx + 1];
//                     targetTable = targetParamSegment.Value ?? targetParamSegment;
//                 }

//                 if (targetTable != null)
//                 {
//                     var sourceSegments = new List<SqlSegment>();
//                     for(int i = 0; i <= selectIdx; i++) sourceSegments.Add(rewritten[i]);
//                     for(int i = selectIdx + 1; i < intoIdx; i++) sourceSegments.Add(rewritten[i]);
//                     if (!string.IsNullOrWhiteSpace(leftOfInto)) sourceSegments.Add(new SqlSegment(SqlSegmentType.Literal, leftOfInto));
//                     int intoInsertionIndex = sourceSegments.Count;
//                     if (!string.IsNullOrWhiteSpace(rightOfInto)) sourceSegments.Add(new SqlSegment(SqlSegmentType.Literal, rightOfInto));
//                     int startRest = targetParamSegment != null ? intoIdx + 2 : intoIdx + 1;
//                     for(int i = startRest; i < rewritten.Count; i++) sourceSegments.Add(rewritten[i]);
                    
//                     var fragment = new SqlSelectIntoFragment(targetParamSegment ?? targetTable, sourceSegments, intoInsertionIndex);
//                     rewritten.Clear();
//                     rewritten.Add(new SqlSegment(SqlSegmentType.Raw, fragment));
//                     return rewritten;
//                 }
//             }
//         }

//         // =========================================================================
//         // STEP 4: UNIVERSAL NEIGHBORHOOD COSMETIC PARENTHESIS STRIPPING
//         // =========================================================================
//         for (int i = 0; i < rewritten.Count; i++)
//         {
//             var segment = rewritten[i];
//             if (segment.Type == SqlSegmentType.Reference && segment.Value is ISqlQuery)
//             {
//                 if (i > 0 && rewritten[i - 1].Type == SqlSegmentType.Literal)
//                 {
//                     var prevText = rewritten[i - 1].Value?.ToString() ?? "";
//                     var trimmedEnd = prevText.TrimEnd();
//                     if (trimmedEnd.EndsWith("("))
//                     {
//                         rewritten[i - 1] = new SqlSegment(SqlSegmentType.Literal, trimmedEnd[..^1]);
//                     }
//                 }

//                 if (i + 1 < rewritten.Count && rewritten[i + 1].Type == SqlSegmentType.Literal)
//                 {
//                     var nextText = rewritten[i + 1].Value?.ToString() ?? "";
//                     var trimmedStart = nextText.TrimStart();
//                     if (trimmedStart.StartsWith(")"))
//                     {
//                         rewritten[i + 1] = new SqlSegment(SqlSegmentType.Literal, trimmedStart[1..]);
//                     }
//                 }
//             }
//         }

//         // =========================================================================
//         // STEP 5: FINAL CLEAN REWRITTEN COMPONENT STATE INDEX MAPPER
//         // =========================================================================
//         int finalUpdateIdx = -1, finalSetIdx = -1, finalFromIdx = -1, finalWhereIdx = -1;
//         int finalFirstFromIdx = -1, finalSecondFromIdx = -1, finalWhereDeleteIdx = -1;
//         bool finalIsDelete = false;
//         parenDepth = 0;

//         for (int i = 0; i < rewritten.Count; i++)
//         {
//             var segment = rewritten[i];
            
//             if (parenDepth == 0)
//             {
//                 if ((segment.Tag == SqlSegmentTag.UpdateKeyword || (segment.Type == SqlSegmentType.Literal && segment.Value?.ToString()?.Trim() == "UPDATE")) && finalUpdateIdx == -1) finalUpdateIdx = i;
//                 else if ((segment.Tag == SqlSegmentTag.SetKeyword || segment.Value is SqlSetFragment || (segment.Type == SqlSegmentType.Literal && segment.Value?.ToString()?.Trim() == "SET")) && finalSetIdx == -1) finalSetIdx = i;
//                 else if (segment.Tag == SqlSegmentTag.DeleteKeyword || (segment.Type == SqlSegmentType.Literal && segment.Value?.ToString()?.Trim() == "DELETE")) { finalIsDelete = true; if (finalFirstFromIdx == -1) finalFirstFromIdx = i; }
                
//                 if (segment.Tag == SqlSegmentTag.FromKeyword || (segment.Type == SqlSegmentType.Literal && segment.Value?.ToString()?.Trim() == "FROM"))
//                 {
//                     if (finalFromIdx == -1) finalFromIdx = i;
//                     if (finalFirstFromIdx == -1) finalFirstFromIdx = i;
//                     else if (finalSecondFromIdx == -1) finalSecondFromIdx = i;
//                 }
//                 else if (segment.Tag == SqlSegmentTag.WhereKeyword || (segment.Type == SqlSegmentType.Literal && segment.Value?.ToString()?.Trim() == "WHERE"))
//                 {
//                     if (finalWhereIdx == -1) finalWhereIdx = i;
//                     if (finalWhereDeleteIdx == -1) finalWhereDeleteIdx = i;
//                 }
//             }

//             if (segment.Type == SqlSegmentType.Literal && segment.Value is string sText)
//             {
//                 for (int j = 0; j < sText.Length; j++)
//                 {
//                     if (sText[j] == '(') parenDepth++;
//                     else if (sText[j] == ')') parenDepth--;
//                 }
//             }
//         }

//         // =========================================================================
//         // STEP 6: DML TREE PACKING BLOCK
//         // =========================================================================
        
//         // CASE A: Standard Multi-Table Join Updates
//         if (finalUpdateIdx >= 0 && finalSetIdx > finalUpdateIdx && finalFromIdx > finalSetIdx)
//         {
//             var target = new SqlSegmentCollectionFragment([.. rewritten.Skip(finalUpdateIdx + 1).Take(finalSetIdx - finalUpdateIdx - 1)]);
//             var endOfSet = finalFromIdx > 0 ? finalFromIdx : (finalWhereIdx > 0 ? finalWhereIdx : rewritten.Count);
            
//             var setClauseSegments = rewritten.Skip(finalSetIdx + 1).Take(endOfSet - finalSetIdx - 1).Select(s => 
//                 s.Type == SqlSegmentType.Projection && s.Value is ISqlProjection p ? new SqlSegment(SqlSegmentType.Projection, p, SqlRenderMode.BaseName) : s
//             ).ToList();
            
//             var setClause = new SqlSegmentCollectionFragment(setClauseSegments);
//             int endOfFrom = finalWhereIdx > 0 ? finalWhereIdx : rewritten.Count;
//             var fromClause = new SqlSegmentCollectionFragment([.. rewritten.Skip(finalFromIdx + 1).Take(endOfFrom - finalFromIdx - 1)]);
//             SqlSegmentCollectionFragment? whereClause = finalWhereIdx > 0 ? new SqlSegmentCollectionFragment([.. rewritten.Skip(finalWhereIdx + 1)]) : null;

//             rewritten.Clear();
//             rewritten.Add(new SqlSegment(SqlSegmentType.Raw, new SqlMultiTableUpdateFragment(target, setClause, fromClause, whereClause)));
//             return rewritten;
//         }

//         // CASE B: Inline View Subquery Updates (Completely isolated by the split-pass keyword shield!)
//         if (finalUpdateIdx >= 0 && finalSetIdx > finalUpdateIdx && finalFromIdx == -1)
//         {
//             int targetIdx = -1;
//             for (int k = finalUpdateIdx + 1; k < finalSetIdx; k++)
//             {
//                 if (rewritten[k].Value is ISqlQuery) { targetIdx = k; break; }
//             }

//             if (targetIdx > -1 && rewritten[targetIdx].Value is ISqlQuery subquery)
//             {
//                 string alias = "stats";
                
//                 // Track down the identifier name cleanly inside the target space chunks
//                 for (int k = targetIdx + 1; k < finalSetIdx; k++)
//                 {
//                     var valStr = rewritten[k].Value?.ToString()?.Trim();
//                     if (string.IsNullOrEmpty(valStr) || string.Equals(valStr, "AS", StringComparison.OrdinalIgnoreCase)) continue;
                    
//                     var cleanIdentifier = UnquoteIdentifier(valStr);
//                     if (System.Text.RegularExpressions.Regex.IsMatch(cleanIdentifier, @"^\w+$"))
//                     {
//                         alias = cleanIdentifier;
//                     }
//                 }

//                 var endOfSet = finalWhereIdx > 0 ? finalWhereIdx : rewritten.Count;
//                 var setClauseSegments = rewritten.Skip(finalSetIdx + 1).Take(endOfSet - finalSetIdx - 1).ToList();
//                 var setClause = new SqlSegmentCollectionFragment(setClauseSegments);
//                 SqlSegmentCollectionFragment? whereClause = finalWhereIdx > 0 ? new SqlSegmentCollectionFragment(rewritten.Skip(finalWhereIdx + 1).ToList()) : null;

//                 rewritten.Clear();
//                 if (!SupportedFeatures.Contains(SqlFeature.UpdatableInlineViews)) 
//                 {
//                     subquery.ExcludeParentheses = true;
//                     rewritten.Add(new SqlSegment(SqlSegmentType.Raw, new SqlUpdateCteFragment(alias, subquery, setClause, whereClause)));
//                 }
//                 else
//                 {
//                     subquery.ExcludeParentheses = false;
//                     rewritten.Add(new SqlSegment(SqlSegmentType.Raw, new SqlUpdateSubqueryFragment(subquery, alias, setClause, whereClause)));
//                 }
//                 return rewritten;
//             }
//         }

//         // CASE C: Complex Multi-Table Delete joins
//         if (finalIsDelete && finalFirstFromIdx > -1 && finalSecondFromIdx > -1)
//         {
//             var targetSegments = rewritten.Skip(finalFirstFromIdx + 1).Take(finalSecondFromIdx - finalFirstFromIdx - 1).ToList();
//             var fromSegments = finalWhereDeleteIdx > -1 
//                 ? rewritten.Skip(finalSecondFromIdx + 1).Take(finalWhereDeleteIdx - finalSecondFromIdx - 1).ToList()
//                 : rewritten.Skip(finalSecondFromIdx + 1).ToList();
//             var whereSegments = finalWhereDeleteIdx > -1 ? rewritten.Skip(finalWhereDeleteIdx + 1).ToList() : null;

//             var targetFrag = new SqlSegmentCollectionFragment(targetSegments);
//             var fromFrag = new SqlSegmentCollectionFragment(fromSegments);
//             var whereFrag = whereSegments != null ? new SqlSegmentCollectionFragment(whereSegments) : null;

//             rewritten.Clear();
//             rewritten.Add(new SqlSegment(SqlSegmentType.Raw, new SqlMultiTableDeleteFragment(targetFrag, fromFrag, whereFrag)));
//             return rewritten;
//         }

//         return rewritten;
//     }

//     protected virtual string RenderSetOperation(SqlSetOperationFragment fragment, ISqlContext context)
//     {
//         string opKeyword = fragment.Operator switch
//         {
//             SqlSetOperator.Except => SqlKeyword.Except,
//             SqlSetOperator.Intersect => SqlKeyword.Intersect,
//             SqlSetOperator.Union => SqlKeyword.Union,
//             SqlSetOperator.UnionAll => SqlKeyword.UnionAll,
//             _ => throw new NotImplementedException($"Set operator {fragment.Operator} is not supported.")
//         };
//         return $"{fragment.Left.ToSql(context)}{Environment.NewLine}{opKeyword}{Environment.NewLine}{fragment.Right.ToSql(context)}";
//     }

//     /// <summary>Renders the default multi-table UPDATE as <c>UPDATE ... SET ... FROM ... WHERE ...</c>.</summary>
//     protected virtual string RenderMultiTableUpdate(SqlMultiTableUpdateFragment update, ISqlContext context)
//     {
//         var setSql = update.SetClause.ToSql(context).TrimStart();
        
//         // Anti-collision guard: If the inner fragment already generated a SET prefix, don't duplicate it!
//         string setPrefix = setSql.StartsWith("SET", StringComparison.OrdinalIgnoreCase) ? "" : "SET ";
        
//         var sql = $"UPDATE {update.Target.ToSql(context)}{Environment.NewLine}{setPrefix}{setSql}";
        
//         if (update.FromClause != null) 
//             sql += $"{Environment.NewLine}FROM {update.FromClause.ToSql(context)}";
            
//         if (update.WhereClause != null) 
//             sql += $"{Environment.NewLine}WHERE {update.WhereClause.ToSql(context)}";
            
//         return sql;
//     }

//     protected virtual string RenderMultiTableDelete(SqlMultiTableDeleteFragment delete, ISqlContext context)
//     {
//         var sql = $"DELETE FROM {delete.Target.ToSql(context)}";
//         if (delete.FromClause != null) sql += $"{Environment.NewLine}FROM {delete.FromClause.ToSql(context)}";
//         if (delete.WhereClause != null) sql += $"{Environment.NewLine}WHERE {delete.WhereClause.ToSql(context)}";
//         return sql;
//     }

//     protected virtual string RenderDeleteAs(SqlDeleteAsFragment fragment, ISqlContext context)
//     {
//         return $"DELETE FROM {fragment.Target.ToSql(context, SqlRenderMode.BaseName)}";
//     }

//     protected virtual string RenderUpdateAs(SqlUpdateAsFragment fragment, ISqlContext context)
//     {
//         return $"UPDATE {fragment.Target.ToSql(context, SqlRenderMode.BaseName)}";
//     }

//     /// <summary>Renders the standardized updatable subquery inline template view statement.</summary>
//     protected virtual string RenderUpdateSubquery(SqlUpdateSubqueryFragment fragment, ISqlContext context)
//     {
//         var quotedAlias = QuoteIdentifier(fragment.Alias);
//         var sql = $"UPDATE ({fragment.Subquery.ToSql(context)}) AS {quotedAlias}{Environment.NewLine}SET {fragment.SetClause.ToSql(context)}";
//         if (fragment.WhereClause != null) sql += $"{Environment.NewLine}WHERE {fragment.WhereClause.ToSql(context)}";
//         return sql;
//     }

//     /// <summary>Renders the standard updatable subquery rewritten inside a Common Table Expression fallback statement.</summary>
//     protected virtual string RenderUpdateCte(SqlUpdateCteFragment fragment, ISqlContext context)
//     {
//         var quotedAlias = QuoteIdentifier(fragment.Alias);
//         var sql = $"WITH {quotedAlias} AS ({Environment.NewLine}{fragment.Subquery.ToSql(context)}{Environment.NewLine}){Environment.NewLine}UPDATE {quotedAlias}{Environment.NewLine}SET {fragment.SetClause.ToSql(context)}";
//         if (fragment.WhereClause != null) sql += $"{Environment.NewLine}WHERE {fragment.WhereClause.ToSql(context)}";
//         return sql;
//     }

//     protected virtual string RenderSelectInto(SqlSelectIntoFragment fragment, ISqlContext context)
//     {
//         string target = fragment.TargetTable switch
//         {
//             string s => QuoteIdentifier(s),
//             SqlSegment paramSeg => SqlSegmentRenderer.Instance.Render(context, paramSeg, 0, [paramSeg]) ?? "",
//             ISqlFragment frag => frag.ToSql(context),
//             _ => fragment.TargetTable.ToString()!
//         };

//         var vsb = new System.Text.StringBuilder();
//         for (int i = 0; i < fragment.SourceSegments.Count; i++)
//         {
//             if (i == fragment.IntoSegmentIndex) vsb.Append($"{Environment.NewLine}INTO {target}");
//             var seg = fragment.SourceSegments[i];
//             vsb.Append(SqlSegmentRenderer.Instance.Render(context, seg, i, fragment.SourceSegments));
//         }
//         if (fragment.IntoSegmentIndex >= fragment.SourceSegments.Count) vsb.Append($"{Environment.NewLine}INTO {target}");
//         return vsb.ToString();
//     }
// }