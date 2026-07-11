namespace SqlInterpol;

public partial class SqlBuilder : ISqlGeneratorBuilder
{
    // Explicitly map the ISqlContext to the builder's internal SqlContext
    ISqlContext ISqlGeneratorBuilder.Context => Context;

    void ISqlGeneratorBuilder.AppendRaw(string rawSql, params string[] segmentTags)
    {
        // Add a literal segment. The SqlSegment constructor naturally handles the params array.
        // We explicitly name 'renderMode: null' to skip over the optional argument and hit the tags.
        _segments.Add(new SqlSegment(SqlSegmentType.Literal, rawSql, renderMode: null, tags: segmentTags));
    }

    void ISqlGeneratorBuilder.AppendSegment(SqlSegment segment)
    {
        _segments.Add(segment);
    }

    string ISqlGeneratorBuilder.ResolveAlias(string variableName, string defaultTableName, bool wasAutoAliased)
    {
        if (ScopedVariables.TryGetValue(variableName, out var entity) && 
            entity is ISqlEntityBase { Reference.Alias: { Length: > 0 } alias })
        {
            if (wasAutoAliased)
            {
                return Context.Options.EntityAutoAliasing ? alias : defaultTableName;
            }
            return alias; 
        }
        return defaultTableName;
    }

    void ISqlGeneratorBuilder.AppendDeclaration(string tableName, string? schema, string variableName, bool wasAutoAliased)
    {
        var genDb = (ISqlGeneratorBuilder)this;
        var dialect = genDb.Context.Dialect;

        genDb.AppendRaw(dialect.QuoteEntityName(tableName, !string.IsNullOrEmpty(schema) ? schema : null));

        string alias = genDb.ResolveAlias(variableName, tableName, wasAutoAliased);
        
        if (alias != tableName)
        {
            genDb.AppendRaw(" AS ", Parsing.SqlSegmentTag.TableAliasAsKeyword);
            genDb.AppendRaw(dialect.QuoteIdentifier(alias));
        }
    }
}