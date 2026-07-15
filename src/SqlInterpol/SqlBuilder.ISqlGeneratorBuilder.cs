namespace SqlInterpol;

public partial class SqlBuilder : ISqlGeneratorBuilder
{
    // Explicitly map the ISqlContext to the builder's internal SqlContext
    ISqlContext ISqlGeneratorBuilder.Context => Context;

    void ISqlGeneratorBuilder.AppendRaw(string rawSql, params string[] segmentTags)
    {
        _segments.Add(new SqlSegment(SqlSegmentType.Raw, rawSql, renderMode: null, tags: segmentTags));
    }

    void ISqlGeneratorBuilder.AppendSegment(SqlSegment segment)
    {
        // 🌟 FAST-TRACK UNROLLING: 
        if (segment.Type == SqlSegmentType.Raw && segment.Value is SqlSegmentCollectionFragment collection)
        {
            string indent = "";
            if (_segments.Count > 0 && (_segments[^1].Type == SqlSegmentType.Literal || _segments[^1].Type == SqlSegmentType.Raw))
            {
                var prevText = _segments[^1].Value?.ToString();
                if (!string.IsNullOrEmpty(prevText))
                {
                    int lastNewline = prevText.LastIndexOf('\n');
                    if (lastNewline >= 0)
                    {
                        int chars = 0;
                        int k = lastNewline + 1;
                        while (k < prevText.Length && (prevText[k] == ' ' || prevText[k] == '\t')) 
                        {
                            chars++;
                            k++;
                        }
                        if (chars > 0) indent = prevText.Substring(lastNewline + 1, chars);
                    }
                }
            }
            
            foreach (var innerSeg in collection.Segments)
            {
                if (indent.Length > 0 && (innerSeg.Type == SqlSegmentType.Literal || innerSeg.Type == SqlSegmentType.Raw) && innerSeg.Value is string s && s.Contains('\n'))
                {
                    _segments.Add(new SqlSegment(innerSeg.Type, s.Replace("\n", "\n" + indent), innerSeg.RenderMode, innerSeg.Tags));
                }
                else
                {
                    _segments.Add(innerSeg);
                }
            }
        }
        else
        {
            _segments.Add(segment);
        }
    }

    string ISqlGeneratorBuilder.ResolveAlias(string variableName, string defaultTableName, bool wasAutoAliased)
    {
        // 🌟 FIX: Strictly return the active alias or empty, so AOT can properly route fallbacks!
        if (ScopedVariables.TryGetValue(variableName, out var entity))
        {
            ISqlEntityBase? eBase = entity as ISqlEntityBase;
            if (entity is ISqlDeclaration decl) eBase = decl.Entity;

            if (eBase != null)
            {
                string? alias = eBase.Reference.Alias;
                if (!string.IsNullOrEmpty(alias)) return alias;
                
                if (wasAutoAliased && Context.Options.EntityAutoAliasing)
                {
                    return variableName;
                }
            }
        }
        return "";
    }

    void ISqlGeneratorBuilder.AppendDeclaration(string tableName, string? schema, string variableName, bool wasAutoAliased)
    {
        var genDb = (ISqlGeneratorBuilder)this;
        var dialect = genDb.Context.Dialect;
        genDb.AppendRaw(dialect.QuoteEntityName(tableName, !string.IsNullOrEmpty(schema) ? schema : null));

        string alias = genDb.ResolveAlias(variableName, tableName, wasAutoAliased);
        
        if (!string.IsNullOrEmpty(alias) && alias != tableName)
        {
            genDb.AppendRaw(" AS ", Parsing.SqlSegmentTag.TableAliasAsKeyword);
            genDb.AppendRaw(dialect.QuoteIdentifier(alias));
        }
    }
}