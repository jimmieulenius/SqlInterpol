using System.Runtime.CompilerServices;

namespace SqlInterpol;

public static partial class SqlBuilderExtensions
{
    private static string? ExtractVariableName(string? expression)
    {
        if (string.IsNullOrWhiteSpace(expression)) return null;
        var parts = expression.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        return parts[^1].Trim();
    }

    private static string? GetExistingAlias(SqlBuilder builder, string? cleanVarName)
    {
        if (!string.IsNullOrEmpty(cleanVarName) && builder.ScopedVariables.TryGetValue(cleanVarName, out var existingNode))
        {
            if (existingNode is ISqlEntityBase eBase) return eBase.Reference.Alias;
            if (existingNode is ISqlDeclaration eDecl) return eDecl.Entity.Reference.Alias;
        }
        return null;
    }

    public static SqlBuilder Entity<T>(
        this SqlBuilder builder, 
        out T dummyPoco, 
        string? alias = null,
        string? name = null,
        string? schema = null,
        [CallerArgumentExpression(nameof(dummyPoco))] string? varName = null) 
    {
        if (typeof(T).IsValueType) dummyPoco = default!;
        else dummyPoco = (T)RuntimeHelpers.GetUninitializedObject(typeof(T));
        
        string? cleanVarName = ExtractVariableName(varName);
        string? sqlAlias = alias ?? (builder.Context.Options.EntityAutoAliasing ? cleanVarName : null);
        
        var entity = ((ISqlEntityRegistry)builder).RegisterEntity<T>(name: name, schema: schema, alias: sqlAlias);
        if (!string.IsNullOrEmpty(cleanVarName)) builder.ScopedVariables[cleanVarName] = entity;

        return builder;
    }

    /// <summary>
    /// Re-maps an outer variable scope name to an internal method parameter variable name.
    /// </summary>
    public static SqlBuilder Entity<T>(
        this SqlBuilder builder,
        string? sourceKey,
        out T localDummy,
        [CallerArgumentExpression(nameof(localDummy))] string? localKey = null)
    {
        static string? CleanKey(string? key)
        {
            if (string.IsNullOrWhiteSpace(key)) return null;
            var parts = key.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            return parts[^1].Trim();
        }

        if (typeof(T).IsValueType) localDummy = default!;
        else localDummy = (T)RuntimeHelpers.GetUninitializedObject(typeof(T));

        var cleanSource = CleanKey(sourceKey);
        var cleanLocal = CleanKey(localKey);

        bool successfullyMapped = false;
        if (!string.IsNullOrEmpty(cleanSource) && !string.IsNullOrEmpty(cleanLocal))
        {
            if (builder.ScopedVariables.TryGetValue(sourceKey!, out var node) ||
                builder.ScopedVariables.TryGetValue(cleanSource, out node))
            {
                builder.ScopedVariables[cleanLocal] = node;
                successfullyMapped = true;
            }
            else
            {
                foreach (var kvp in builder.ScopedVariables)
                {
                    if (kvp.Key.EndsWith(cleanSource))
                    {
                        builder.ScopedVariables[cleanLocal] = kvp.Value;
                        successfullyMapped = true;
                        break;
                    }
                }
            }
        }

        // =====================================================================
        // CRITICAL RUNTIME GUARD CLAUSE
        // =====================================================================
        if (!successfullyMapped && !string.IsNullOrEmpty(cleanSource))
        {
            throw new InvalidOperationException(
                $"Scope Error: Cannot map query variable '{cleanLocal}' to expression '{cleanSource}'. " +
                $"The entity '{typeof(T).Name}' must be explicitly registered on this specific SqlBuilder instance via .Entity<{typeof(T).Name}>(out var ...) before it can be used.");
        }

        return builder;
    }

    // =========================================================================
    // 1. INLINE INJECTION OVERLOADS (Renamed from Subquery to Query)
    // =========================================================================

    /// <summary>
    /// Captures the SQL written by <paramref name="action"/> into an isolated, strongly-typed <see cref="ISqlQuery{T}"/> scope.
    /// </summary>
    public static ISqlQuery<T> Query<T>(
        this SqlBuilder builder,
        T dummyPoco,
        Action action,
        string? alias = null,
        [CallerArgumentExpression(nameof(dummyPoco))] string? varName = null)
    {
        var innerQuery = builder.Query(action);
        string? cleanVarName = ExtractVariableName(varName);
        string? sqlAlias = alias ?? GetExistingAlias(builder, cleanVarName) ?? (builder.Context.Options.EntityAutoAliasing ? cleanVarName : null);
        
        var typedQuery = new SqlQuery<T>(innerQuery, sqlAlias);
        if (!string.IsNullOrEmpty(cleanVarName)) builder.ScopedVariables[cleanVarName] = typedQuery;
        
        return typedQuery;
    }

    /// <summary>
    /// Wraps an existing standalone <see cref="ISqlQuery"/> into a strongly-typed <see cref="ISqlQuery{T}"/> contextual view.
    /// </summary>
    public static ISqlQuery<T> Query<T>(
        this SqlBuilder builder,
        T dummyPoco,
        ISqlQuery existingQuery,
        string? alias = null,
        [CallerArgumentExpression(nameof(dummyPoco))] string? varName = null)
    {
        string? cleanVarName = ExtractVariableName(varName);
        string? sqlAlias = alias ?? GetExistingAlias(builder, cleanVarName) ?? (builder.Context.Options.EntityAutoAliasing ? cleanVarName : null);
        
        var typedQuery = new SqlQuery<T>(existingQuery, sqlAlias);
        if (!string.IsNullOrEmpty(cleanVarName)) builder.ScopedVariables[cleanVarName] = typedQuery;
        
        return typedQuery;
    }

    // =========================================================================
    // 2. FLUENT CHAINING OVERLOADS (Renamed from Subquery to Query)
    // =========================================================================

    /// <summary>
    /// Captures an isolated query block into an output variable while maintaining the builder's fluent chain.
    /// </summary>
    public static SqlBuilder Query<T>(
        this SqlBuilder builder,
        T dummyPoco,
        out ISqlQuery<T> capturedSubquery,
        Action action,
        string? alias = null,
        [CallerArgumentExpression(nameof(dummyPoco))] string? varName = null)
    {
        var innerQuery = builder.Query(action);
        string? cleanVarName = ExtractVariableName(varName);
        string? sqlAlias = alias ?? GetExistingAlias(builder, cleanVarName) ?? (builder.Context.Options.EntityAutoAliasing ? cleanVarName : null);
        
        var typedQuery = new SqlQuery<T>(innerQuery, sqlAlias);
        capturedSubquery = typedQuery;
        
        if (!string.IsNullOrEmpty(cleanVarName)) builder.ScopedVariables[cleanVarName] = typedQuery;
        
        return builder;
    }

    /// <summary>
    /// Fluently registers an existing query object into the current builder's scope as a strongly-typed entity view.
    /// </summary>
    public static SqlBuilder Query<T>(
        this SqlBuilder builder,
        out T dummyPoco,
        ISqlQuery existingQuery,
        string? alias = null,
        [CallerArgumentExpression(nameof(dummyPoco))] string? varName = null)
    {
        if (typeof(T).IsValueType) dummyPoco = default!;
        else dummyPoco = (T)RuntimeHelpers.GetUninitializedObject(typeof(T));

        string? cleanVarName = ExtractVariableName(varName);
        string? sqlAlias = alias ?? (builder.Context.Options.EntityAutoAliasing ? cleanVarName : null);
        
        _ = ((ISqlEntityRegistry)builder).RegisterEntity<T>(alias: sqlAlias);
        var typedQuery = new SqlQuery<T>(existingQuery, sqlAlias);
        
        if (!string.IsNullOrEmpty(cleanVarName)) builder.ScopedVariables[cleanVarName] = typedQuery;
        
        return builder;
    }

    // =========================================================================
    // 3. DYNAMIC METADATA RESOLUTION
    // =========================================================================

    public static ISqlFragment Column<T>(
        this SqlBuilder builder,
        T dummyPoco,
        string propertyName,
        [CallerArgumentExpression(nameof(dummyPoco))] string? varName = null)
    {
        string? cleanVarName = ExtractVariableName(varName);

        if (!string.IsNullOrEmpty(cleanVarName) && builder.ScopedVariables.TryGetValue(cleanVarName, out var entity))
        {
            ISqlEntityBase? entityBase = entity as ISqlEntityBase;
            if (entity is ISqlDeclaration decl)
            {
                entityBase = decl.Entity;
            }

            if (entityBase != null)
            {
                var meta = SqlMetadataRegistry.GetMetadata(entityBase.ModelType);
                var memberMeta = meta.Columns.Keys.FirstOrDefault(k => k.Name.Equals(propertyName, StringComparison.OrdinalIgnoreCase));
                string physicalColumnName = memberMeta != null ? meta.Columns[memberMeta] : propertyName;

                return new SqlColumnReference(entityBase.Reference, physicalColumnName, propertyName);
            }
        }

        throw new ArgumentException($"Cannot resolve dynamic column '{propertyName}' because '{cleanVarName}' is not a registered entity in the current scope.");
    }
}