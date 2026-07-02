namespace SqlInterpol.Parsing;

public partial class SqlSegmentPreprocessor
{
    private static void SetAlias(object? target, string? alias)
    {
        if (target is ISqlAliasable aliasableTarget) aliasableTarget.Alias = alias;
    }

    private static void ApplyAliasToTarget(string quotedAlias, PreprocessorState state)
    {
        if (state.LastEntityRef != null) SetAlias(state.LastEntityRef, quotedAlias);
        else if (state.LastAliasableTarget is ISqlEntityBase lastEnt) SetAlias(lastEnt.Reference, quotedAlias);
        else SetAlias(state.LastAliasableTarget, quotedAlias);
    }

    private static string DetermineAliasTag(PreprocessorState state)
    {
        if (state.ActiveDmlKeyword == SqlKeyword.Update.Value && state.CurrentClauseTag == SqlSegmentTag.UpdateKeyword) 
            return SqlSegmentTag.UpdateAsKeyword;
            
        if (state.ActiveDmlKeyword == SqlKeyword.Delete.Value && (state.CurrentClauseTag == SqlSegmentTag.DeleteKeyword || (state.CurrentClauseTag == SqlSegmentTag.FromKeyword && state.FromCount == 1))) 
            return SqlSegmentTag.DeleteAsKeyword;
            
        if (state.CurrentClauseTag == SqlSegmentTag.FromKeyword) 
            return SqlSegmentTag.TableAliasAsKeyword;
            
        return "ColumnAliasAsKeyword";
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

                        // High-Performance centralized peeking! No duplication.
                        if (TryParseAlias(text.AsSpan(), out var parseResult))
                        {
                            tempAlias = parseResult.Identifier.ToString();
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
            throw new ArgumentException($"Property '{dynCol.PropertyName}' not found on '{dynCol.EntityType.Name}'.");

        return new SqlColumnReference(activeRef, entityMeta.Columns[memberMeta], memberMeta.Name);
    }

    private bool TryExtractInlineAlias(ref string text, SqlSegment segment, PreprocessorState state)
    {
        if (string.IsNullOrEmpty(text)) return false;

        // Zero-allocation structural scanning via the shared state machine helper
        if (!TryParseAlias(text.AsSpan(), out var parseResult))
        {
            return false;
        }

        string prefix = text[..parseResult.PrefixLength];
        string identifier = parseResult.Identifier.ToString();
        string quotedAlias = state.Context.Dialect.QuoteIdentifier(identifier);
        string determinedTag = DetermineAliasTag(state);

        foreach (char c in prefix) if (c == ')') state.PopState();
        ApplyAliasToTarget(quotedAlias, state);

        if (prefix.Length > 0) 
            state.Refined.Add(new SqlSegment(SqlSegmentType.Literal, prefix, segment.RenderMode, segment.Tags));

        if (parseResult.IsExplicit)
        {
            string keyword = text.Substring(parseResult.PrefixLength, 2);
            state.Refined.Add(new SqlSegment(SqlSegmentType.Literal, keyword, segment.RenderMode, determinedTag == "ColumnAliasAsKeyword" ? segment.Tags : [determinedTag]));
            
            int wsLength = parseResult.IdentifierStart - (parseResult.PrefixLength + 2);
            if (wsLength > 0)
            {
                string wsAfterAs = text.Substring(parseResult.PrefixLength + 2, wsLength);
                state.Refined.Add(new SqlSegment(SqlSegmentType.Literal, wsAfterAs, segment.RenderMode, segment.Tags));
            }
            
            state.Refined.Add(new SqlSegment(SqlSegmentType.Raw, quotedAlias, null, segment.Tags));
        }
        else
        {
            state.Refined.Add(new SqlSegment(SqlSegmentType.Raw, quotedAlias, null, determinedTag == "ColumnAliasAsKeyword" ? segment.Tags : [determinedTag]));
        }

        text = text.Substring(parseResult.TotalLength);
        return true;
    }

    private bool ProcessHoleBoundAlias(SqlSegment segment, PreprocessorState state)
    {
        if (!state.ExpectsAlias || segment.Type == SqlSegmentType.Literal) return false;
        state.ExpectsAlias = false;

        if (segment.Type == SqlSegmentType.Projection || segment.Value is ISqlProjection)
        {
            state.Refined.Add(new SqlSegment(segment.Type, segment.Value, SqlRenderMode.AliasOnly, segment.Tags));
            return true;
        }

        string? customAliasString = null;

        if (segment.Value is ISqlEntityBase entityTarget)
        {
            string rawAlias = !string.IsNullOrWhiteSpace(entityTarget.Reference.Alias) ? entityTarget.Reference.Alias : (entityTarget.Reference.FallbackAlias ?? entityTarget.ModelType.Name);
            customAliasString = state.Context.Dialect.QuoteIdentifier(rawAlias.Trim('[', ']', '"', '`', ' ', '<', '>'));
            SetAlias(entityTarget.Reference, customAliasString); 
        }
        else if (segment.Value is ISqlReference referenceTarget)
        {
            string rawAlias = !string.IsNullOrWhiteSpace(referenceTarget.Alias) ? referenceTarget.Alias : (referenceTarget.FallbackAlias ?? "Unknown");
            customAliasString = state.Context.Dialect.QuoteIdentifier(rawAlias.Trim('[', ']', '"', '`', ' ', '<', '>'));
            SetAlias(referenceTarget, customAliasString);
        }
        else if (segment.Value is ISqlFragment frag)
        {
            customAliasString = frag.ToSql(state.Context, SqlRenderMode.AliasOnly);
            ApplyAliasToTarget(customAliasString, state);
        }
        else if (segment.Value != null)
        {
            string rawAlias = segment.Value.ToString() ?? "";
            customAliasString = state.Context.Dialect.QuoteIdentifier(rawAlias.Trim('[', ']', '"', '`', ' ', '<', '>'));
            ApplyAliasToTarget(customAliasString, state);
        }

        if (!string.IsNullOrWhiteSpace(customAliasString))
        {
            state.Refined.Add(new SqlSegment(SqlSegmentType.Raw, customAliasString, null, segment.Tags));
            return true;
        }

        if (segment.Type == SqlSegmentType.Reference && segment.Value is ISqlEntityBase)
        {
            state.Refined.Add(new SqlSegment(segment.Type, segment.Value, SqlRenderMode.AliasOnly, segment.Tags));
            return true;
        }

        return false;
    }

    private class TemporaryAliasReference : ISqlReference, ISqlAliasable
    {
        private readonly ISqlReference _baseRef;
        public string? Alias { get; set; }
        public string? FallbackAlias => _baseRef.FallbackAlias;
        public bool IsAliasQuoted { get; set; }
        public ISqlFragment Source => _baseRef.Source; 

        public TemporaryAliasReference(ISqlReference baseRef, string alias) { _baseRef = baseRef; Alias = alias; IsAliasQuoted = true; }
        public string ToSql(ISqlContext context, SqlRenderMode renderMode = SqlRenderMode.Default) => !string.IsNullOrWhiteSpace(Alias) ? Alias : _baseRef.ToSql(context, renderMode);
    }

    private class AliaslessReference : ISqlReference, ISqlAliasable
    {
        private readonly ISqlReference _baseRef;
        public AliaslessReference(ISqlReference baseRef) { _baseRef = baseRef; }
        
        public string? Alias { get => null; set { if (_baseRef is ISqlAliasable a) a.Alias = value; } }
        public string? FallbackAlias => null; 
        public bool IsAliasQuoted { get => false; set { } }
        public ISqlFragment Source => _baseRef.Source; 

        public string ToSql(ISqlContext context, SqlRenderMode renderMode = SqlRenderMode.Default) => _baseRef.ToSql(context, renderMode);
    }
}