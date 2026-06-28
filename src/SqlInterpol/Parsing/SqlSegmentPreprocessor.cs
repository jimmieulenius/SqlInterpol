using System.Collections;
using System.Reflection;
using System.Text.RegularExpressions;

namespace SqlInterpol.Parsing;

/// <summary>
/// The default semantic preprocessor that normalizes text, isolates core DML keywords, 
/// handles target entity aliases (both hole-bound and plain text), and routes projection mapping.
/// </summary>
public partial class SqlSegmentPreprocessor : ISqlSegmentPreprocessor
{
    public static readonly SqlSegmentPreprocessor Instance = new();

    // ====================================================================
    // PRE-COMPILED REGEX ENGINES (C# 11+)
    // ====================================================================
    [GeneratedRegex(@"^(\s*)(AS)(\s+)([a-zA-Z0-9_]+)", RegexOptions.IgnoreCase)]
    private static partial Regex ExplicitAliasRegex();

    [GeneratedRegex(@"^\s+([a-zA-Z0-9_]+)", RegexOptions.IgnoreCase)]
    private static partial Regex ImplicitAliasRegex();

    // ====================================================================
    // ROLE INTERFACES
    // ====================================================================
    private static void SetAlias(object target, string? alias)
    {
        if (target is ISqlAliasable aliasableTarget)
        {
            aliasableTarget.Alias = alias;
        }
    }

    private static IReadOnlyList<SqlSegment>? ExtractInternalSegments(object obj)
    {
        if (obj is ISqlSegmentContainer container)
        {
            return container.Segments;
        }
        return null;
    }

    private static SqlColumnReference ResolveDynamicColumn(SqlDynamicColumn dynCol, IReadOnlyList<SqlSegment> segments)
    {
        ISqlEntityBase? targetEntity = null;
        for (int i = 0; i < segments.Count; i++)
        {
            if (segments[i].Value is ISqlEntityBase ent && ent.ModelType == dynCol.EntityType)
            {
                targetEntity = ent; 
                break; 
            }
        }

        if (targetEntity == null)
            throw new InvalidOperationException($"Could not find registered entity of type '{dynCol.EntityType.Name}' in context.");

        var entityMeta = SqlMetadataRegistry.GetMetadata(dynCol.EntityType);
        var memberMeta = entityMeta.Columns.Keys.FirstOrDefault(k => k.Name.Equals(dynCol.PropertyName, StringComparison.OrdinalIgnoreCase));
        if (memberMeta == null)
            throw new ArgumentException($"Property '{dynCol.PropertyName}' not found.");

        return new SqlColumnReference(targetEntity.Reference, entityMeta.Columns[memberMeta], memberMeta.Name);
    }

    /// <inheritdoc />
    public IReadOnlyList<SqlSegment> Process(IReadOnlyList<SqlSegment> segments, ISqlContext context)
    {
        var refined = new List<SqlSegment>(segments.Count + 10);
        
        int parenDepth = 0;
        bool inString = false;
        bool inLineCmt = false;
        bool inBlockCmt = false;
        
        int fromCount = 0;
        string? currentKeyword = null;
        string? activeDmlKeyword = null;
        bool expectsAlias = false;
        ISqlEntityBase? activeEntityTarget = null;
        object? lastAliasableTarget = null;

        for (int i = 0; i < segments.Count; i++)
        {
            var segment = segments[i];

            // ====================================================================
            // WHITESPACE-INSENSITIVE RECURSIVE SUBQUERY PASS
            // ====================================================================
            if (segment.Value is ISqlFragment fragment && 
                (fragment is ISqlQueryFragment || 
                 fragment.GetType().Name.Contains("Subquery", StringComparison.OrdinalIgnoreCase) || 
                 fragment.GetType().GetInterfaces().Any(i => i.Name.StartsWith("ISqlQueryFragment"))))
            {
                bool hasLeftParen = false;
                for (int idx = refined.Count - 1; idx >= 0; idx--)
                {
                    if (refined[idx].Type == SqlSegmentType.Literal && refined[idx].Value is string lText)
                    {
                        int checkIdx = lText.Length - 1;
                        while (checkIdx >= 0 && char.IsWhiteSpace(lText[checkIdx])) checkIdx--;
                        if (checkIdx >= 0)
                        {
                            if (lText[checkIdx] == '(') hasLeftParen = true;
                            break;
                        }
                    }
                    else break;
                }

                bool hasRightParen = false;
                for (int idx = i + 1; idx < segments.Count; idx++)
                {
                    if (segments[idx].Type == SqlSegmentType.Literal && segments[idx].Value is string rText)
                    {
                        int checkIdx = 0;
                        while (checkIdx < rText.Length && char.IsWhiteSpace(rText[checkIdx])) checkIdx++;
                        if (checkIdx < rText.Length)
                        {
                            if (rText[checkIdx] == ')') hasRightParen = true;
                            break;
                        }
                    }
                    else break;
                }

                var innerSegments = ExtractInternalSegments(fragment) ?? [];
                var processedInner = Process(innerSegments, context).ToList();
                var entityRef = (fragment as ISqlEntityBase)?.Reference;
                
                bool shouldExclude = (hasLeftParen && hasRightParen) || (fragment is ISqlQueryFragment qFrag && qFrag.ExcludeParentheses);
                
                var nestedFrag = new SqlNestedQueryFragment(processedInner, entityRef) { ExcludeParentheses = shouldExclude };
                refined.Add(new SqlSegment(SqlSegmentType.Reference, nestedFrag, segment.RenderMode, segment.Tags ?? []));
                expectsAlias = false;

                if (fragment is ISqlEntityBase structuralEntity)
                {
                    activeEntityTarget = structuralEntity;
                    lastAliasableTarget = structuralEntity;
                }
                else
                {
                    activeEntityTarget = nestedFrag;
                    lastAliasableTarget = nestedFrag;
                }

                continue;
            }

            // ====================================================================
            // 0. EXPECTS ALIAS INTERCEPTION (Hole-Bound)
            // ====================================================================
            if (expectsAlias && segment.Type != SqlSegmentType.Literal)
            {
                expectsAlias = false;

                // FIX: Immediately route projections to AliasOnly mode so they don't get trapped as Table Aliases!
                if (segment.Type == SqlSegmentType.Projection || segment.Value is ISqlProjection)
                {
                    refined.Add(new SqlSegment(segment.Type, segment.Value, SqlRenderMode.AliasOnly, segment.Tags ?? []));
                    continue;
                }

                string? customAliasString = null;

                if (segment.Value is ISqlEntityBase entityTarget)
                {
                    string rawAlias = !string.IsNullOrWhiteSpace(entityTarget.Reference.Alias) 
                        ? entityTarget.Reference.Alias 
                        : (entityTarget.Reference.FallbackAlias ?? entityTarget.ModelType.Name);

                    string cleanAlias = rawAlias.Trim('[', ']', '"', '`', ' ', '<', '>');
                    customAliasString = context.Dialect.QuoteIdentifier(cleanAlias);
                    SetAlias(entityTarget.Reference, customAliasString); 
                }
                else if (segment.Value is ISqlReference referenceTarget)
                {
                    string rawAlias = !string.IsNullOrWhiteSpace(referenceTarget.Alias) 
                        ? referenceTarget.Alias 
                        : (referenceTarget.FallbackAlias ?? "Unknown");

                    string cleanAlias = rawAlias.Trim('[', ']', '"', '`', ' ', '<', '>');
                    customAliasString = context.Dialect.QuoteIdentifier(cleanAlias);
                    SetAlias(referenceTarget, customAliasString);
                }
                else if (segment.Value is ISqlFragment frag)
                {
                    customAliasString = frag.ToSql(context, SqlRenderMode.AliasOnly);
                    if (lastAliasableTarget is ISqlEntityBase targetEnt && targetEnt.Reference != null) 
                    {
                        SetAlias(targetEnt.Reference, customAliasString);
                    }
                }
                else if (segment.Value != null)
                {
                    string rawAlias = segment.Value.ToString() ?? "";
                    string cleanAlias = rawAlias.Trim('[', ']', '"', '`', ' ', '<', '>');
                    customAliasString = context.Dialect.QuoteIdentifier(cleanAlias);
                    
                    if (lastAliasableTarget is ISqlEntityBase targetEnt && targetEnt.Reference != null) 
                    {
                        SetAlias(targetEnt.Reference, customAliasString);
                    }
                }

                if (!string.IsNullOrWhiteSpace(customAliasString))
                {
                    refined.Add(new SqlSegment(SqlSegmentType.Raw, customAliasString, null, segment.Tags ?? []));
                    continue;
                }

                if (segment.Type == SqlSegmentType.Reference && segment.Value is ISqlEntityBase ent)
                {
                    refined.Add(new SqlSegment(segment.Type, segment.Value, SqlRenderMode.AliasOnly, segment.Tags ?? []));
                    continue;
                }
            }

            // ====================================================================
            // 1. CONTEXT TARGET TRACKING & DYNAMIC PROJECTION PASS
            // ====================================================================
            if (segment.Value is SqlDynamicColumn dynCol)
            {
                var colRef = ResolveDynamicColumn(dynCol, segments);
                refined.Add(new SqlSegment(SqlSegmentType.Projection, colRef, segment.RenderMode, segment.Tags ?? []));
                continue;
            }

            if (segment.Value is ISqlEntityBase structuralEntity1)
            {
                activeEntityTarget = structuralEntity1;
                lastAliasableTarget = structuralEntity1;
            }
            else if (segment.Type == SqlSegmentType.Projection)
            {
                lastAliasableTarget = segment.Value;
                refined.Add(segment);
                continue;
            }
            else if (segment.Value != null)
            {
                var prop = segment.Value.GetType().GetProperties().FirstOrDefault(p => typeof(ISqlEntityBase).IsAssignableFrom(p.PropertyType));
                if (prop != null && prop.GetValue(segment.Value) is ISqlEntityBase wrappedEntity)
                {
                    activeEntityTarget = wrappedEntity;
                    lastAliasableTarget = wrappedEntity;
                }
            }

            // ====================================================================
            // 2. TEXT LITERAL PASS (Inline Alias Plucking)
            // ====================================================================
            if (segment.Type == SqlSegmentType.Literal && segment.Value is string text)
            {
                if (refined.Count > 0 && refined[^1].Type == SqlSegmentType.Reference && refined[^1].Value is ISqlEntityBase prevEnt && prevEnt is not ISqlQueryFragment)
                {
                    var explicitAliasMatch = ExplicitAliasRegex().Match(text);
                    if (explicitAliasMatch.Success)
                    {
                        string rawAlias = explicitAliasMatch.Groups[4].Value;
                        string quotedAlias = context.Dialect.QuoteIdentifier(rawAlias);
                        
                        if (prevEnt.Reference != null) SetAlias(prevEnt.Reference, quotedAlias);
                        
                        if (explicitAliasMatch.Groups[1].Length > 0)
                            refined.Add(new SqlSegment(SqlSegmentType.Literal, explicitAliasMatch.Groups[1].Value, segment.RenderMode, segment.Tags ?? []));
                        
                        string targetTag = SqlSegmentTag.TableAliasAsKeyword;
                        if (currentKeyword == "SELECT" || currentKeyword == "SELECT DISTINCT") targetTag = "ColumnAliasAsKeyword";
                        else if (activeDmlKeyword == "UPDATE" && currentKeyword == "UPDATE") targetTag = SqlSegmentTag.UpdateAsKeyword;
                        else if (activeDmlKeyword == "DELETE" && (currentKeyword == "DELETE" || (currentKeyword == "FROM" && fromCount == 1))) targetTag = SqlSegmentTag.DeleteAsKeyword;

                        if (targetTag == "ColumnAliasAsKeyword")
                        {
                            refined.Add(new SqlSegment(SqlSegmentType.Literal, explicitAliasMatch.Groups[2].Value, segment.RenderMode, segment.Tags ?? []));
                        }
                        else
                        {
                            refined.Add(new SqlSegment(SqlSegmentType.Literal, explicitAliasMatch.Groups[2].Value, segment.RenderMode, [targetTag]));
                        }

                        if (explicitAliasMatch.Groups[3].Length > 0)
                            refined.Add(new SqlSegment(SqlSegmentType.Literal, explicitAliasMatch.Groups[3].Value, segment.RenderMode, segment.Tags ?? []));

                        refined.Add(new SqlSegment(SqlSegmentType.Raw, quotedAlias, null, segment.Tags ?? []));
                        
                        text = text.Substring(explicitAliasMatch.Length);
                    }
                    else
                    {
                        var implicitAliasMatch = ImplicitAliasRegex().Match(text);
                        if (implicitAliasMatch.Success)
                        {
                            string matchingWord = implicitAliasMatch.Groups[1].Value;
                            string upperWord = matchingWord.ToUpperInvariant();
                            
                            if (upperWord != "WHERE" && upperWord != "JOIN" && upperWord != "ON" && upperWord != "SET" && 
                                upperWord != "LEFT" && upperWord != "RIGHT" && upperWord != "INNER" && upperWord != "FULL" && 
                                upperWord != "CROSS" && upperWord != "FROM" && upperWord != "AS" && upperWord != "INTO" && 
                                upperWord != "ORDER" && upperWord != "GROUP" && upperWord != "HAVING")
                            {
                                string quotedAlias = context.Dialect.QuoteIdentifier(matchingWord);
                                if (prevEnt.Reference != null) SetAlias(prevEnt.Reference, quotedAlias);
                                
                                int spaceLen = implicitAliasMatch.Length - matchingWord.Length;
                                if (spaceLen > 0)
                                    refined.Add(new SqlSegment(SqlSegmentType.Literal, implicitAliasMatch.Value[..spaceLen], segment.RenderMode, segment.Tags ?? []));
                                
                                string targetTag = SqlSegmentTag.TableAliasAsKeyword;
                                if (currentKeyword == "SELECT" || currentKeyword == "SELECT DISTINCT") targetTag = "ColumnAliasAsKeyword";
                                else if (activeDmlKeyword == "UPDATE" && currentKeyword == "UPDATE") targetTag = SqlSegmentTag.UpdateAsKeyword;
                                else if (activeDmlKeyword == "DELETE" && (currentKeyword == "DELETE" || (currentKeyword == "FROM" && fromCount == 1))) targetTag = SqlSegmentTag.DeleteAsKeyword;

                                if (targetTag == "ColumnAliasAsKeyword")
                                {
                                    refined.Add(new SqlSegment(SqlSegmentType.Raw, quotedAlias, null, segment.Tags ?? []));
                                }
                                else
                                {
                                    refined.Add(new SqlSegment(SqlSegmentType.Raw, quotedAlias, null, [targetTag]));
                                }

                                text = text.Substring(implicitAliasMatch.Length);
                            }
                        }
                    }
                }

                int lastSplitIdx = 0;
                for (int j = 0; j < text.Length; j++)
                {
                    char c = text[j];
                    if (inString) { if (c == '\'') { if (j + 1 < text.Length && text[j + 1] == '\'') j++; else inString = false; } continue; }
                    if (inBlockCmt) { if (c == '*' && j + 1 < text.Length && text[j + 1] == '/') { inBlockCmt = false; j++; } continue; }
                    if (inLineCmt) { if (c == '\n' || c == '\r') inLineCmt = false; continue; }
                    if (c == '\'') { inString = true; continue; }
                    if (c == '/' && j + 1 < text.Length && text[j + 1] == '*') { inBlockCmt = true; j++; continue; }
                    if (c == '-' && j + 1 < text.Length && text[j + 1] == '-') { inLineCmt = true; j++; continue; }
                    if (c == '(') { parenDepth++; continue; }
                    if (c == ')') { parenDepth--; continue; }

                    if (parenDepth == 0)
                    {
                        bool isWordBoundary = j == 0 || (!char.IsLetterOrDigit(text[j - 1]) && text[j - 1] != '_');
                        if (isWordBoundary)
                        {
                            var span = text.AsSpan(j);
                            static bool Match(ReadOnlySpan<char> s, string kw) => s.StartsWith(kw, StringComparison.OrdinalIgnoreCase) && (s.Length == kw.Length || (!char.IsLetterOrDigit(s[kw.Length]) && s[kw.Length] != '_'));

                            string? matchedWord = null;
                            string? targetTag = null;

                            if (Match(span, "SELECT DISTINCT")) { matchedWord = "SELECT DISTINCT"; targetTag = SqlSegmentTag.SelectDistinctKeyword; }
                            else if (Match(span, "SELECT"))     { matchedWord = "SELECT";   targetTag = SqlSegmentTag.SelectKeyword; }
                            else if (Match(span, "UPDATE"))     { matchedWord = "UPDATE";   targetTag = SqlSegmentTag.UpdateKeyword; }
                            else if (Match(span, "SET"))        { matchedWord = "SET";      targetTag = SqlSegmentTag.SetKeyword; }
                            else if (Match(span, "INSERT"))     { matchedWord = "INSERT";   targetTag = SqlSegmentTag.InsertKeyword; }
                            else if (Match(span, "INTO"))       { matchedWord = "INTO";     targetTag = SqlSegmentTag.IntoKeyword; }
                            else if (Match(span, "VALUES"))     { matchedWord = "VALUES";   targetTag = SqlSegmentTag.InsertValuesKeyword; }
                            else if (Match(span, "FROM"))       { matchedWord = "FROM";     targetTag = SqlSegmentTag.FromKeyword; }
                            else if (Match(span, "WHERE"))      { matchedWord = "WHERE";    targetTag = SqlSegmentTag.WhereKeyword; }
                            else if (Match(span, "DELETE"))     { matchedWord = "DELETE";   targetTag = SqlSegmentTag.DeleteKeyword; }

                            if (matchedWord != null)
                            {
                                if (j > lastSplitIdx) refined.Add(new SqlSegment(SqlSegmentType.Literal, text[lastSplitIdx..j], segment.RenderMode, segment.Tags ?? []));
                                refined.Add(new SqlSegment(SqlSegmentType.Literal, text.Substring(j, matchedWord.Length), null, targetTag != null ? [targetTag] : []));
                                currentKeyword = matchedWord;
                                
                                if (matchedWord == "FROM") fromCount++;
                                else if (matchedWord == "UPDATE" || matchedWord == "DELETE" || matchedWord == "SELECT" || matchedWord == "SELECT DISTINCT" || matchedWord == "INSERT") 
                                {
                                    activeDmlKeyword = matchedWord;
                                    fromCount = 0;
                                }

                                j += matchedWord.Length - 1; 
                                lastSplitIdx = j + 1;
                            }
                        }
                    }
                }

                if (lastSplitIdx < text.Length)
                {
                    var chunk = text[lastSplitIdx..];
                    var trimmedText = chunk.TrimEnd();
                    if (trimmedText.EndsWith("AS", StringComparison.OrdinalIgnoreCase))
                    {
                        expectsAlias = trimmedText.Length == 2 || !char.IsLetterOrDigit(trimmedText[^3]);
                        if (expectsAlias && parenDepth == 0)
                        {
                            int asIdx = chunk.LastIndexOf("AS", StringComparison.OrdinalIgnoreCase);
                            if (asIdx > 0) 
                                refined.Add(new SqlSegment(SqlSegmentType.Literal, chunk[..asIdx], segment.RenderMode, segment.Tags ?? []));

                            string targetTag = SqlSegmentTag.TableAliasAsKeyword;
                            if (currentKeyword == "SELECT" || currentKeyword == "SELECT DISTINCT") targetTag = "ColumnAliasAsKeyword";
                            else if (activeDmlKeyword == "UPDATE" && currentKeyword == "UPDATE") targetTag = SqlSegmentTag.UpdateAsKeyword;
                            else if (activeDmlKeyword == "DELETE" && (currentKeyword == "DELETE" || (currentKeyword == "FROM" && fromCount == 1))) targetTag = SqlSegmentTag.DeleteAsKeyword;

                            if (targetTag == "ColumnAliasAsKeyword")
                            {
                                refined.Add(new SqlSegment(SqlSegmentType.Literal, chunk.Substring(asIdx, 2), segment.RenderMode, segment.Tags ?? []));
                            }
                            else
                            {
                                refined.Add(new SqlSegment(SqlSegmentType.Literal, chunk.Substring(asIdx, 2), segment.RenderMode, [targetTag]));
                            }
                            
                            string afterAs = chunk[(asIdx + 2)..];
                            if (afterAs.Length > 0) 
                                refined.Add(new SqlSegment(SqlSegmentType.Literal, afterAs, segment.RenderMode, segment.Tags ?? []));
                        }
                        else 
                        {
                            expectsAlias = false;
                            refined.Add(new SqlSegment(SqlSegmentType.Literal, chunk, segment.RenderMode, segment.Tags ?? []));
                        }
                    }
                    else 
                    {
                        expectsAlias = false;
                        refined.Add(new SqlSegment(SqlSegmentType.Literal, chunk, segment.RenderMode, segment.Tags ?? []));
                    }
                }
                continue;
            }

            // ====================================================================
            // 3. UNRESOLVED OBJECT PASS
            // ====================================================================
            if (segment.Type == SqlSegmentType.Unresolved)
            {
                var value = segment.Value;
                if (value is ISqlEntityBase entity)
                {
                    if (string.Equals(currentKeyword, "SELECT", StringComparison.OrdinalIgnoreCase) || 
                        string.Equals(currentKeyword, "SELECT DISTINCT", StringComparison.OrdinalIgnoreCase))
                    {
                        var entityMeta = SqlMetadataRegistry.GetMetadata(entity.ModelType);
                        var columns = new List<ISqlFragment>();
                        
                        foreach (var col in entityMeta.SortedColumns)
                        {
                            var memberType = col.Key is PropertyInfo prop ? prop.PropertyType : ((FieldInfo)col.Key).FieldType;
                            if (memberType.IsClass && memberType != typeof(string) && memberType != typeof(byte[])) continue;
                            columns.Add(new SqlColumnReference(entity.Reference, col.Value, col.Key.Name));
                        }

                        refined.Add(new SqlSegment(SqlSegmentType.Raw, new SqlSelectFragment(columns, isDistinct: string.Equals(currentKeyword, "SELECT DISTINCT", StringComparison.OrdinalIgnoreCase))));
                        continue;
                    }

                    refined.Add(new SqlSegment(SqlSegmentType.Reference, entity, segment.RenderMode, segment.Tags ?? []));
                    continue;
                }

                if (value is ISqlExpandable expandable)
                {
                    if (activeEntityTarget == null) throw new InvalidOperationException("Sql.Expand requires an active entity context.");
                    
                    var entityMeta = SqlMetadataRegistry.GetMetadata(activeEntityTarget.ModelType);
                    var dtoProps = SqlMetadataRegistry.GetDtoProperties(expandable.DtoType);
                    var assignments = new List<ISqlAssignmentFragment>();
                    bool isSetClause = string.Equals(currentKeyword, "SET", StringComparison.OrdinalIgnoreCase);

                    var sortedDtoProps = dtoProps.OrderBy(p => {
                        var m = entityMeta.Columns.Keys.FirstOrDefault(k => k.Name == p.Name);
                        return m != null ? entityMeta.Columns[m] : p.Name;
                    }).ToArray();

                    foreach (var prop in sortedDtoProps)
                    {
                        if (isSetClause && expandable.KeyProperties.Contains(prop.Name)) continue;
                        var entityMember = entityMeta.Columns.Keys.FirstOrDefault(k => k.Name == prop.Name);
                        if (entityMember != null)
                        {
                            var colRef = new SqlColumnReference(activeEntityTarget.Reference, entityMeta.Columns[entityMember], prop.Name);
                            assignments.Add(new SqlAssignmentFragment(colRef, Sql.Arg(prop.Name)));
                        }
                    }

                    if (isSetClause) refined.Add(new SqlSegment(SqlSegmentType.Reference, new SqlSetFragment(assignments)));
                    else if (string.Equals(currentKeyword, "SELECT", StringComparison.OrdinalIgnoreCase) || string.Equals(currentKeyword, "SELECT DISTINCT", StringComparison.OrdinalIgnoreCase))
                    {
                        var columns = assignments.Select(a => (ISqlFragment)a.Reference).ToList();
                        refined.Add(new SqlSegment(SqlSegmentType.Raw, new SqlSelectFragment(columns, isDistinct: string.Equals(currentKeyword, "SELECT DISTINCT", StringComparison.OrdinalIgnoreCase))));
                    }
                    else refined.Add(new SqlSegment(SqlSegmentType.Reference, new SqlInsertValuesFragment(assignments)));
                    continue;
                }

                if (value != null && value.GetType().IsClass && value is not string && value is not IEnumerable && value is not ISqlFragment)
                {
                    if (activeEntityTarget == null) throw new InvalidOperationException($"DTO context missing for target mapping.");
                    if (currentKeyword == "SET")
                    {
                        var assignments = Sql.BuildAssignments(activeEntityTarget, value, context);
                        foreach (var a in assignments) if (a is ISqlParameterGenerator gen) gen.GenerateParameters(context);
                        refined.Add(new SqlSegment(SqlSegmentType.Reference, new SqlSetFragment(assignments)));
                        continue;
                    }
                    if (currentKeyword == "INSERT" || currentKeyword == "VALUES")
                    {
                        var assignments = Sql.BuildAssignments(activeEntityTarget, value, context);
                        foreach (var a in assignments) if (a is ISqlParameterGenerator gen) gen.GenerateParameters(context);
                        refined.Add(new SqlSegment(SqlSegmentType.Reference, new SqlInsertValuesFragment(assignments)));
                        continue;
                    }
                }

                if (value is IEnumerable databaseIterable && value is not string && value is not byte[])
                {
                    var parameterizedKeys = new List<string>();
                    foreach (var element in databaseIterable) parameterizedKeys.Add(context.AddParameter(element));
                    refined.Add(new SqlSegment(SqlSegmentType.Raw, new SqlRawCollectionFragment(parameterizedKeys), segment.RenderMode, segment.Tags ?? []));
                    continue;
                }

                string registeredParamKey = context.AddParameter(value);
                refined.Add(new SqlSegment(SqlSegmentType.Parameter, registeredParamKey, segment.RenderMode, segment.Tags ?? []));
                continue;
            }

            refined.Add(segment);
            expectsAlias = false; 
        }

        return refined;
    }
}