using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
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
    
    [GeneratedRegex(@"^(\s*\)*\s*)(AS)(\s+)([a-zA-Z0-9_]+)", RegexOptions.IgnoreCase)]
    private static partial Regex ExplicitAliasRegex();

    [GeneratedRegex(@"^(\s*\)*\s+)([a-zA-Z0-9_]+)", RegexOptions.IgnoreCase)]
    private static partial Regex ImplicitAliasRegex();

    // ====================================================================
    // ROLE INTERFACES & PROXIES
    // ====================================================================
    private static void SetAlias(object target, string? alias)
    {
        if (target is ISqlAliasable aliasableTarget)
        {
            aliasableTarget.Alias = alias;
        }
    }

    /// <summary>
    /// A lightweight proxy that applies a forward-resolved alias without mutating the underlying AST reference.
    /// </summary>
    private class TemporaryAliasReference : ISqlReference, ISqlAliasable
    {
        private readonly ISqlReference _baseRef;
        
        public string? Alias { get; set; }
        public string? FallbackAlias => _baseRef.FallbackAlias;
        
        public bool IsAliasQuoted { get; set; }
        public ISqlFragment Source => _baseRef.Source; 

        public TemporaryAliasReference(ISqlReference baseRef, string alias)
        {
            _baseRef = baseRef;
            Alias = alias;
            IsAliasQuoted = true; 
        }

        public string ToSql(ISqlContext context, SqlRenderMode renderMode = SqlRenderMode.Default)
        {
            if (!string.IsNullOrWhiteSpace(Alias)) return Alias;
            return _baseRef.ToSql(context, renderMode);
        }
    }

    /// <summary>
    /// Prevents a Subquery AST node from double-rendering its own alias natively, 
    /// while securely passing the alias mutation down to the underlying reference so Columns can resolve it!
    /// </summary>
    private class AliaslessReference : ISqlReference, ISqlAliasable
    {
        private readonly ISqlReference _baseRef;
        public AliaslessReference(ISqlReference baseRef) { _baseRef = baseRef; }
        
        public string? Alias 
        { 
            get => null; // Blind the Subquery output!
            set { if (_baseRef is ISqlAliasable a) a.Alias = value; } // Persist it for the Columns!
        }
        
        public string? FallbackAlias => null; // Prevent "CategoryStats" from leaking natively!
        
        public bool IsAliasQuoted 
        { 
            get => false; 
            set { } // FIX: Safely ignore the setter, this proxy suppresses the alias entirely!
        }
        
        public ISqlFragment Source => _baseRef.Source; 

        public string ToSql(ISqlContext context, SqlRenderMode renderMode = SqlRenderMode.Default)
        {
            return _baseRef.ToSql(context, renderMode);
        }
    }

    private static IReadOnlyList<SqlSegment>? ExtractInternalSegments(object obj)
    {
        if (obj is ISqlSegmentContainer container) return container.Segments;
        if (obj is ISqlQuery query) return query.Segments; 

        if (obj is ISqlEntityBase ent)
        {
            if (ent is ISqlSegmentContainer entContainer) return entContainer.Segments;
            if (ent is ISqlQuery entQuery) return entQuery.Segments;
            
            if (ent.Reference is ISqlSegmentContainer refContainer) return refContainer.Segments;
            if (ent.Reference is ISqlQuery refQuery) return refQuery.Segments;
        }

        return null;
    }

    private static SqlColumnReference ResolveDynamicColumn(SqlDynamicColumnFragment dynCol, IReadOnlyList<SqlSegment> segments, ISqlContext context)
    {
        ISqlEntityBase? targetEntity = null;
        string? forwardAlias = null;

        for (int i = 0; i < segments.Count; i++)
        {
            if (segments[i].Value is ISqlEntityBase ent && 
                ent.ModelType == dynCol.EntityType && 
                !(segments[i].Value is SqlDynamicColumnFragment))
            {
                targetEntity = ent; 
                string? tempAlias = null;

                for (int j = i + 1; j < segments.Count; j++)
                {
                    if (segments[j].Type == SqlSegmentType.Literal && segments[j].Value is string text)
                    {
                        if (string.IsNullOrWhiteSpace(text)) continue; 

                        var explicitMatch = ExplicitAliasRegex().Match(text);
                        if (explicitMatch.Success)
                        {
                            tempAlias = explicitMatch.Groups[4].Value;
                            break;
                        }

                        var implicitMatch = ImplicitAliasRegex().Match(text);
                        if (implicitMatch.Success)
                        {
                            string upperWord = implicitMatch.Groups[2].Value.ToUpperInvariant();
                            bool isIgnored = upperWord switch
                            {
                                "WHERE" or "JOIN" or "ON" or "SET" or "LEFT" or "RIGHT" or "INNER" or "OUTER" or "FULL" or "CROSS" or 
                                "FROM" or "AS" or "INTO" or "ORDER" or "GROUP" or "BY" or "HAVING" or "LIMIT" or "VALUES" or 
                                "RETURNING" or "OFFSET" or "FETCH" or "FOR" or "UNION" or "EXCEPT" or "INTERSECT" or 
                                "SELECT" or "UPDATE" or "DELETE" or "INSERT" or "AND" or "OR" or "NOT" or "IS" or "NULL" or "ASC" or "DESC" => true,
                                _ => false
                            };
                            if (!isIgnored) tempAlias = implicitMatch.Groups[2].Value;
                        }
                        break; 
                    }
                }

                if (tempAlias != null)
                {
                    forwardAlias = tempAlias;
                    break; 
                }
            }
        }

        if (targetEntity == null)
            throw new InvalidOperationException($"Could not find registered entity of type '{dynCol.EntityType.Name}' in context.");

        ISqlReference activeRef = targetEntity.Reference;
        if (forwardAlias != null && string.IsNullOrWhiteSpace(activeRef.Alias))
        {
            activeRef = new TemporaryAliasReference(activeRef, context.Dialect.QuoteIdentifier(forwardAlias));
        }

        var entityMeta = SqlMetadataRegistry.GetMetadata(dynCol.EntityType);
        var memberMeta = entityMeta.Columns.Keys.FirstOrDefault(k => k.Name.Equals(dynCol.PropertyName, StringComparison.OrdinalIgnoreCase));
        if (memberMeta == null)
            throw new ArgumentException($"Property '{dynCol.PropertyName}' not found.");

        return new SqlColumnReference(activeRef, entityMeta.Columns[memberMeta], memberMeta.Name);
    }

    /// <inheritdoc />
    public IReadOnlyList<SqlSegment> Process(IReadOnlyList<SqlSegment> segments, ISqlContext context)
    {
        var refined = new List<SqlSegment>(segments.Count + 10);
        
        bool inString = false;
        bool inLineCmt = false;
        bool inBlockCmt = false;
        
        int parenDepth = 0;
        int fromCount = 0;
        string? currentKeyword = null;
        string? currentClauseTag = null;
        string? activeDmlKeyword = null;
        bool expectsAlias = false;
        bool forceBaseNamePhase = false;
        
        ISqlEntityBase? activeEntityTarget = null;
        object? lastAliasableTarget = null;
        ISqlReference? lastEntityRef = null;

        var kwStack = new List<string?>();
        var clauseStack = new List<string?>();
        var dmlStack = new List<string?>();
        var fromStack = new List<int>();
        var baseNameStack = new List<bool>();

        void PopState()
        {
            if (parenDepth > 0)
            {
                parenDepth--; 
                currentKeyword = kwStack[^1]; kwStack.RemoveAt(kwStack.Count - 1);
                currentClauseTag = clauseStack[^1]; clauseStack.RemoveAt(clauseStack.Count - 1);
                activeDmlKeyword = dmlStack[^1]; dmlStack.RemoveAt(dmlStack.Count - 1);
                fromCount = fromStack[^1]; fromStack.RemoveAt(fromStack.Count - 1);
                forceBaseNamePhase = baseNameStack[^1]; baseNameStack.RemoveAt(baseNameStack.Count - 1);
            }
        }

        for (int i = 0; i < segments.Count; i++)
        {
            var segment = segments[i];

            // ====================================================================
            // WHITESPACE-INSENSITIVE RECURSIVE SUBQUERY PASS
            // ====================================================================
            bool isSubquery = segment.Value is ISqlQueryFragment || 
                              (segment.Value is ISqlEntityBase e && e.Reference is ISqlQueryFragment) ||
                              segment.Value is ISqlQuery;
                              
            var innerSegments = isSubquery && segment.Value != null ? ExtractInternalSegments(segment.Value) : null;
            
            if (innerSegments != null)
            {
                bool hasLeftParen = false;
                for (int idx = refined.Count - 1; idx >= 0; idx--)
                {
                    if (refined[idx].Type == SqlSegmentType.Literal && refined[idx].Value is string lText)
                    {
                        int checkIdx = lText.Length - 1;
                        while (checkIdx >= 0 && char.IsWhiteSpace(lText[checkIdx])) checkIdx--;
                        if (checkIdx >= 0) { if (lText[checkIdx] == '(') hasLeftParen = true; break; }
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
                        if (checkIdx < rText.Length) { if (rText[checkIdx] == ')') hasRightParen = true; break; }
                    }
                    else break;
                }

                var processedInner = Process(innerSegments, context).ToList();
                var entityRef = (segment.Value as ISqlEntityBase)?.Reference;
                
                bool shouldExclude = (hasLeftParen && hasRightParen);
                if (segment.Value is ISqlQueryFragment qFrag && qFrag.ExcludeParentheses) shouldExclude = true;
                
                bool hasInlineAlias = false;
                for (int n = i + 1; n < segments.Count; n++)
                {
                    if (segments[n].Type == SqlSegmentType.Literal && segments[n].Value is string nText)
                    {
                        if (string.IsNullOrWhiteSpace(nText)) continue;
                        if (ExplicitAliasRegex().IsMatch(nText)) hasInlineAlias = true;
                        else
                        {
                            var impMatch = ImplicitAliasRegex().Match(nText);
                            if (impMatch.Success)
                            {
                                string w = impMatch.Groups[2].Value.ToUpperInvariant();
                                bool isIgnored = w switch { "WHERE" or "JOIN" or "ON" or "SET" or "LEFT" or "RIGHT" or "INNER" or "OUTER" or "FULL" or "CROSS" or "FROM" or "ORDER" or "GROUP" or "BY" or "HAVING" or "LIMIT" or "VALUES" or "RETURNING" or "OFFSET" or "FETCH" or "FOR" or "UNION" or "EXCEPT" or "INTERSECT" or "SELECT" or "UPDATE" or "DELETE" or "INSERT" or "AND" or "OR" or "NOT" or "IS" or "NULL" or "ASC" or "DESC" => true, _ => false };
                                if (!isIgnored) hasInlineAlias = true;
                            }
                        }
                        break;
                    }
                }
                
                var safeRef = (hasInlineAlias && entityRef != null) ? new AliaslessReference(entityRef) : entityRef;
                
                var nestedFrag = new SqlNestedQueryFragment(processedInner, safeRef) { ExcludeParentheses = shouldExclude };
                refined.Add(new SqlSegment(SqlSegmentType.Reference, nestedFrag, segment.RenderMode, segment.Tags ?? []));
                expectsAlias = false;

                if (segment.Value is ISqlEntityBase structuralEntity)
                {
                    activeEntityTarget = structuralEntity;
                    lastAliasableTarget = structuralEntity;
                    lastEntityRef = safeRef; 
                }
                else
                {
                    activeEntityTarget = nestedFrag;
                    lastAliasableTarget = nestedFrag;
                    lastEntityRef = safeRef; 
                }

                continue;
            }

            // ====================================================================
            // 0. EXPECTS ALIAS INTERCEPTION (Hole-Bound)
            // ====================================================================
            if (expectsAlias && segment.Type != SqlSegmentType.Literal)
            {
                expectsAlias = false;

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
            if (segment.Value is SqlDynamicColumnFragment dynCol)
            {
                var colRef = ResolveDynamicColumn(dynCol, segments, context);
                var mode = segment.RenderMode;
                if (mode == null && forceBaseNamePhase) mode = SqlRenderMode.BaseName;
                
                refined.Add(new SqlSegment(SqlSegmentType.Projection, colRef, mode, segment.Tags ?? []));
                continue;
            }

            if (segment.Value is SqlDynamicOrderFragment dynOrder)
            {
                var colRef = ResolveDynamicColumn(dynOrder.Column, segments, context);
                
                var resolvedFragment = dynOrder.Direction.HasValue 
                    ? new SqlOrderFragment(colRef, dynOrder.Direction.Value)
                    : new SqlOrderFragment(colRef);

                refined.Add(new SqlSegment(SqlSegmentType.Raw, resolvedFragment, segment.RenderMode, segment.Tags ?? []));
                continue;
            }

            if (segment.Value is ISqlEntityBase structuralEntity1)
            {
                activeEntityTarget = structuralEntity1;
                lastAliasableTarget = structuralEntity1;
                lastEntityRef = structuralEntity1.Reference;
            }
            else if (segment.Type == SqlSegmentType.Projection)
            {
                lastAliasableTarget = segment.Value;
                var mode = segment.RenderMode;
                if (mode == null && forceBaseNamePhase) mode = SqlRenderMode.BaseName;
                
                refined.Add(new SqlSegment(segment.Type, segment.Value, mode, segment.Tags ?? []));
                continue;
            }
            else if (segment.Value != null)
            {
                var prop = segment.Value.GetType().GetProperties().FirstOrDefault(p => typeof(ISqlEntityBase).IsAssignableFrom(p.PropertyType));
                if (prop != null && prop.GetValue(segment.Value) is ISqlEntityBase wrappedEntity)
                {
                    activeEntityTarget = wrappedEntity;
                    lastAliasableTarget = wrappedEntity;
                    lastEntityRef = wrappedEntity.Reference;
                }
            }

            // ====================================================================
            // 2. TEXT LITERAL PASS (Inline Alias Plucking)
            // ====================================================================
            if (segment.Type == SqlSegmentType.Literal && segment.Value is string text)
            {
                if (text.Contains(';')) forceBaseNamePhase = false;
                
                bool isAfterReference = refined.Count > 0 && refined[^1].Type == SqlSegmentType.Reference;
                
                if (isAfterReference)
                {
                    var explicitAliasMatch = ExplicitAliasRegex().Match(text);
                    if (explicitAliasMatch.Success)
                    {
                        string prefix = explicitAliasMatch.Groups[1].Value;
                        string quotedAlias = context.Dialect.QuoteIdentifier(explicitAliasMatch.Groups[4].Value);

                        string determinedTag = "ColumnAliasAsKeyword";
                        if (activeDmlKeyword == "UPDATE" && currentClauseTag == SqlSegmentTag.UpdateKeyword) determinedTag = SqlSegmentTag.UpdateAsKeyword;
                        else if (activeDmlKeyword == "DELETE" && (currentClauseTag == SqlSegmentTag.DeleteKeyword || (currentClauseTag == SqlSegmentTag.FromKeyword && fromCount == 1))) determinedTag = SqlSegmentTag.DeleteAsKeyword;
                        else if (currentClauseTag == SqlSegmentTag.FromKeyword) determinedTag = SqlSegmentTag.TableAliasAsKeyword;

                        foreach (char c in prefix) if (c == ')') PopState();
                        
                        if (lastEntityRef != null) SetAlias(lastEntityRef, quotedAlias);
                        else if (lastAliasableTarget is ISqlEntityBase lastEnt) SetAlias(lastEnt.Reference, quotedAlias);
                        else SetAlias(lastAliasableTarget, quotedAlias);
                        
                        if (prefix.Length > 0)
                            refined.Add(new SqlSegment(SqlSegmentType.Literal, prefix, segment.RenderMode, segment.Tags ?? []));
                        
                        string keyword = explicitAliasMatch.Groups[2].Value; // "AS"
                        if (determinedTag == "ColumnAliasAsKeyword")
                            refined.Add(new SqlSegment(SqlSegmentType.Literal, keyword, segment.RenderMode, segment.Tags ?? []));
                        else
                            refined.Add(new SqlSegment(SqlSegmentType.Literal, keyword, segment.RenderMode, [determinedTag]));

                        string suffix = explicitAliasMatch.Groups[3].Value; // trailing whitespace
                        if (suffix.Length > 0)
                            refined.Add(new SqlSegment(SqlSegmentType.Literal, suffix, segment.RenderMode, segment.Tags ?? []));

                        refined.Add(new SqlSegment(SqlSegmentType.Raw, quotedAlias, null, segment.Tags ?? []));
                        
                        text = text.Substring(explicitAliasMatch.Length);
                    }
                    else
                    {
                        var implicitAliasMatch = ImplicitAliasRegex().Match(text);
                        if (implicitAliasMatch.Success)
                        {
                            string prefix = implicitAliasMatch.Groups[1].Value;
                            string matchingWord = implicitAliasMatch.Groups[2].Value;
                            string upperWord = matchingWord.ToUpperInvariant();

                            bool isIgnoredKeyword = upperWord switch
                            {
                                "WHERE" or "JOIN" or "ON" or "SET" or "LEFT" or "RIGHT" or "INNER" or "OUTER" or "FULL" or "CROSS" or 
                                "FROM" or "AS" or "INTO" or "ORDER" or "GROUP" or "BY" or "HAVING" or "LIMIT" or "VALUES" or 
                                "RETURNING" or "OFFSET" or "FETCH" or "FOR" or "UNION" or "EXCEPT" or "INTERSECT" or 
                                "SELECT" or "UPDATE" or "DELETE" or "INSERT" or "AND" or "OR" or "NOT" or "IS" or "NULL" or "ASC" or "DESC" => true,
                                _ => false
                            };

                            if (!isIgnoredKeyword)
                            {
                                string determinedTag = "ColumnAliasAsKeyword";
                                if (activeDmlKeyword == "UPDATE" && currentClauseTag == SqlSegmentTag.UpdateKeyword) determinedTag = SqlSegmentTag.UpdateAsKeyword;
                                else if (activeDmlKeyword == "DELETE" && (currentClauseTag == SqlSegmentTag.DeleteKeyword || (currentClauseTag == SqlSegmentTag.FromKeyword && fromCount == 1))) determinedTag = SqlSegmentTag.DeleteAsKeyword;
                                else if (currentClauseTag == SqlSegmentTag.FromKeyword) determinedTag = SqlSegmentTag.TableAliasAsKeyword;

                                foreach (char c in prefix) if (c == ')') PopState();

                                string quotedAlias = context.Dialect.QuoteIdentifier(matchingWord);
                                
                                if (lastEntityRef != null) SetAlias(lastEntityRef, quotedAlias);
                                else if (lastAliasableTarget is ISqlEntityBase lastEnt) SetAlias(lastEnt.Reference, quotedAlias);
                                else SetAlias(lastAliasableTarget, quotedAlias);
                                
                                if (prefix.Length > 0)
                                    refined.Add(new SqlSegment(SqlSegmentType.Literal, prefix, segment.RenderMode, segment.Tags ?? []));
                                
                                if (determinedTag == "ColumnAliasAsKeyword")
                                    refined.Add(new SqlSegment(SqlSegmentType.Raw, quotedAlias, null, segment.Tags ?? []));
                                else
                                    refined.Add(new SqlSegment(SqlSegmentType.Raw, quotedAlias, null, [determinedTag]));

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
                    
                    if (c == '(') 
                    { 
                        parenDepth++; 
                        kwStack.Add(currentKeyword);
                        clauseStack.Add(currentClauseTag);
                        dmlStack.Add(activeDmlKeyword);
                        fromStack.Add(fromCount);
                        baseNameStack.Add(forceBaseNamePhase);
                        continue; 
                    }
                    if (c == ')') 
                    { 
                        PopState();
                        continue; 
                    }

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
                        else if (Match(span, "INNER JOIN")) { matchedWord = "INNER JOIN"; targetTag = SqlSegmentTag.FromKeyword; }
                        else if (Match(span, "LEFT JOIN"))  { matchedWord = "LEFT JOIN";  targetTag = SqlSegmentTag.FromKeyword; }
                        else if (Match(span, "RIGHT JOIN")) { matchedWord = "RIGHT JOIN"; targetTag = SqlSegmentTag.FromKeyword; }
                        else if (Match(span, "CROSS JOIN")) { matchedWord = "CROSS JOIN"; targetTag = SqlSegmentTag.FromKeyword; }
                        else if (Match(span, "JOIN"))       { matchedWord = "JOIN";       targetTag = SqlSegmentTag.FromKeyword; }
                        else if (Match(span, "WHERE"))      { matchedWord = "WHERE";    targetTag = SqlSegmentTag.WhereKeyword; }
                        else if (Match(span, "ORDER BY"))   { matchedWord = "ORDER BY"; targetTag = SqlSegmentTag.WhereKeyword; } 
                        else if (Match(span, "GROUP BY"))   { matchedWord = "GROUP BY"; targetTag = SqlSegmentTag.WhereKeyword; }
                        else if (Match(span, "HAVING"))     { matchedWord = "HAVING";   targetTag = SqlSegmentTag.WhereKeyword; }
                        else if (Match(span, "DELETE"))     { matchedWord = "DELETE";   targetTag = SqlSegmentTag.DeleteKeyword; }
                        else if (Match(span, "LIMIT"))      { matchedWord = "LIMIT";    targetTag = SqlSegmentTag.Paging; }
                        else if (Match(span, "RETURNING"))  { matchedWord = "RETURNING";targetTag = SqlSegmentTag.ReturningKeyword; }
                        
                        else if (Match(span, "WITH RECURSIVE"))
                        {
                            matchedWord = "WITH RECURSIVE";
                            targetTag = null;
                            
                            // Abort if the user provides a raw text CTE target (e.g., "WITH RECURSIVE ExpensiveProducts AS")
                            if (string.IsNullOrWhiteSpace(text.Substring(j + matchedWord.Length)))
                            {
                                int forward = i + 1;
                                while (forward < segments.Count)
                                {
                                    if (segments[forward].Value is ISqlRoleable roleableTarget)
                                    {
                                        roleableTarget.Role = SqlEntityRole.Cte;
                                        break;
                                    }
                                    if (segments[forward].Type == SqlSegmentType.Literal && !string.IsNullOrWhiteSpace(segments[forward].Value?.ToString()))
                                        break; 
                                    forward++;
                                }
                            }
                        }
                        else if (Match(span, "WITH"))
                        {
                            matchedWord = "WITH";
                            targetTag = null;
                            
                            // Abort if the user provides a raw text CTE target (e.g., "WITH ExpensiveProducts AS")
                            if (string.IsNullOrWhiteSpace(text.Substring(j + matchedWord.Length)))
                            {
                                int forward = i + 1;
                                while (forward < segments.Count)
                                {
                                    if (segments[forward].Value is ISqlRoleable roleableTarget)
                                    {
                                        roleableTarget.Role = SqlEntityRole.Cte;
                                        break;
                                    }
                                    if (segments[forward].Type == SqlSegmentType.Literal && !string.IsNullOrWhiteSpace(segments[forward].Value?.ToString()))
                                        break; 
                                    forward++;
                                }
                            }
                        }

                        if (matchedWord != null)
                        {
                            if (targetTag == SqlSegmentTag.InsertKeyword || targetTag == SqlSegmentTag.ReturningKeyword)
                                forceBaseNamePhase = true;
                            else if (targetTag == SqlSegmentTag.SelectKeyword || targetTag == SqlSegmentTag.SelectDistinctKeyword || 
                                     targetTag == SqlSegmentTag.UpdateKeyword || targetTag == SqlSegmentTag.DeleteKeyword || 
                                     targetTag == SqlSegmentTag.InsertValuesKeyword || targetTag == SqlSegmentTag.IntoKeyword || 
                                     targetTag == SqlSegmentTag.SetKeyword || targetTag == SqlSegmentTag.WhereKeyword)
                                forceBaseNamePhase = false;

                            if (j > lastSplitIdx) refined.Add(new SqlSegment(SqlSegmentType.Literal, text[lastSplitIdx..j], segment.RenderMode, segment.Tags ?? []));
                            
                            var appliedTags = targetTag != null && parenDepth == 0 ? new[] { targetTag } : [];
                            refined.Add(new SqlSegment(SqlSegmentType.Literal, text.Substring(j, matchedWord.Length), null, appliedTags));
                            
                            currentKeyword = matchedWord;
                            currentClauseTag = targetTag;
                            
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

                if (lastSplitIdx < text.Length)
                {
                    var chunk = text[lastSplitIdx..];
                    var trimmedText = chunk.TrimEnd();
                    if (trimmedText.EndsWith("AS", StringComparison.OrdinalIgnoreCase))
                    {
                        expectsAlias = trimmedText.Length == 2 || !char.IsLetterOrDigit(trimmedText[^3]);
                        if (expectsAlias)
                        {
                            int asIdx = chunk.LastIndexOf("AS", StringComparison.OrdinalIgnoreCase);
                            string prefix = chunk[..asIdx];
                            
                            if (prefix.Length > 0)
                                refined.Add(new SqlSegment(SqlSegmentType.Literal, prefix, segment.RenderMode, segment.Tags ?? []));

                            string determinedTag = "ColumnAliasAsKeyword";
                            if (activeDmlKeyword == "UPDATE" && currentClauseTag == SqlSegmentTag.UpdateKeyword) determinedTag = SqlSegmentTag.UpdateAsKeyword;
                            else if (activeDmlKeyword == "DELETE" && (currentClauseTag == SqlSegmentTag.DeleteKeyword || (currentClauseTag == SqlSegmentTag.FromKeyword && fromCount == 1))) determinedTag = SqlSegmentTag.DeleteAsKeyword;
                            else if (currentClauseTag == SqlSegmentTag.FromKeyword) determinedTag = SqlSegmentTag.TableAliasAsKeyword;

                            string asKeyword = chunk.Substring(asIdx, 2); // Exact "AS"
                            if (determinedTag == "ColumnAliasAsKeyword")
                                refined.Add(new SqlSegment(SqlSegmentType.Literal, asKeyword, segment.RenderMode, segment.Tags ?? []));
                            else
                                refined.Add(new SqlSegment(SqlSegmentType.Literal, asKeyword, segment.RenderMode, [determinedTag]));
                            
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

                    var mode = segment.RenderMode;
                    if (mode == null && forceBaseNamePhase) mode = SqlRenderMode.BaseName;
                    refined.Add(new SqlSegment(SqlSegmentType.Reference, entity, mode, segment.Tags ?? []));
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

                if (value != null && value.GetType().IsClass && value is not string && value is not ISqlFragment)
                {
                    bool isIterable = value is IEnumerable && value is not byte[];

                    if (isIterable && currentKeyword != "INSERT" && currentKeyword != "VALUES")
                    {
                        // Proceed natively down to the standard Iterable handler
                    }
                    else
                    {
                        if (activeEntityTarget == null) throw new InvalidOperationException($"DTO context missing for target mapping.");
                        
                        if (currentKeyword == "SET")
                        {
                            var assignments = Sql.BuildAssignments(activeEntityTarget, value, context);
                            foreach (var a in assignments) if (a is ISqlParameterGenerator gen) gen.GenerateParameters(context);
                            refined.Add(new SqlSegment(SqlSegmentType.Reference, new SqlSetFragment(assignments), segment.RenderMode, segment.Tags ?? []));
                            continue;
                        }
                        if (currentKeyword == "INSERT" || currentKeyword == "VALUES")
                        {
                            if (isIterable)
                            {
                                var bulkAssignments = new List<IReadOnlyList<ISqlAssignmentFragment>>();
                                foreach (var item in (IEnumerable)value)
                                {
                                    var itemAssignments = Sql.BuildAssignments(activeEntityTarget, item, context);
                                    foreach (var a in itemAssignments) if (a is ISqlParameterGenerator gen) gen.GenerateParameters(context);
                                    bulkAssignments.Add(itemAssignments);
                                }
                                refined.Add(new SqlSegment(SqlSegmentType.Reference, new SqlInsertValuesFragment(bulkAssignments), segment.RenderMode, segment.Tags ?? []));
                            }
                            else
                            {
                                var assignments = Sql.BuildAssignments(activeEntityTarget, value, context);
                                foreach (var a in assignments) if (a is ISqlParameterGenerator gen) gen.GenerateParameters(context);
                                refined.Add(new SqlSegment(SqlSegmentType.Reference, new SqlInsertValuesFragment(assignments), segment.RenderMode, segment.Tags ?? []));
                            }
                            continue;
                        }
                    }
                }

                if (value is IEnumerable databaseIterable && value is not string && value is not byte[])
                {
                    bool isFragmentCollection = false;
                    foreach (var element in databaseIterable)
                    {
                        if (element is ISqlFragment) { isFragmentCollection = true; break; }
                        if (element != null) break;
                    }

                    if (isFragmentCollection)
                    {
                        var fragments = new List<ISqlFragment>();
                        foreach (var element in databaseIterable)
                        {
                            if (element is ISqlFragment frag)
                            {
                                if (frag is SqlDynamicOrderFragment innerDynOrder)
                                {
                                    var colRef = ResolveDynamicColumn(innerDynOrder.Column, segments, context);
                                    if (innerDynOrder.Direction.HasValue)
                                        fragments.Add(new SqlOrderFragment(colRef, innerDynOrder.Direction.Value));
                                    else
                                        fragments.Add(new SqlOrderFragment(colRef));
                                }
                                else if (frag is SqlDynamicColumnFragment innerDynCol)
                                {
                                    fragments.Add(ResolveDynamicColumn(innerDynCol, segments, context));
                                }
                                else
                                {
                                    fragments.Add(frag);
                                }
                            }
                        }
                        
                        refined.Add(new SqlSegment(SqlSegmentType.Raw, new SqlCollectionFragment(fragments), segment.RenderMode, segment.Tags ?? []));
                        continue;
                    }

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