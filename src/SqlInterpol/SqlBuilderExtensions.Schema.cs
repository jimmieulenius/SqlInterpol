using System.Runtime.CompilerServices;
using SqlInterpol.Execution;
using SqlInterpol.Schema;
using SqlInterpol.Segments;

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

    /// <summary>
    /// Registers a new entity reference into the builder's local scope for interpolation.
    /// </summary>
    /// <typeparam name="T">The model type to register.</typeparam>
    /// <param name="builder">The builder instance.</param>
    /// <param name="dummyPoco">An uninitialized reference used strictly for property routing in lambdas or interpolation holes.</param>
    /// <param name="alias">An optional explicit alias to use for this entity.</param>
    /// <param name="name">An optional override for the physical table or view name.</param>
    /// <param name="schema">An optional override for the physical database schema.</param>
    /// <param name="varName">The compiler-injected variable name used for scoping.</param>
    /// <returns>The current builder instance for method chaining.</returns>
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
    /// <typeparam name="T">The model type to map.</typeparam>
    /// <param name="builder">The builder instance.</param>
    /// <param name="sourceKey">The name of the variable from the outer scope.</param>
    /// <param name="localDummy">An uninitialized reference mapped to the local variable name.</param>
    /// <param name="localKey">The compiler-injected local variable name used for scoping.</param>
    /// <returns>The current builder instance for method chaining.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the source key cannot be resolved from the active builder scope.</exception>
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

        if (!successfullyMapped && !string.IsNullOrEmpty(cleanSource))
        {
            throw new InvalidOperationException(
                $"Scope Error: Cannot map query variable '{cleanLocal}' to expression '{cleanSource}'. " +
                $"The entity '{typeof(T).Name}' must be explicitly registered on this specific SqlBuilder instance via .Entity<{typeof(T).Name}>(out var ...) before it can be used.");
        }

        return builder;
    }

    /// <summary>
    /// Captures the SQL written by <paramref name="action"/> into an isolated, strongly-typed <see cref="ISqlQuery{T}"/> scope.
    /// </summary>
    /// <typeparam name="T">The model type representing the resulting query projection.</typeparam>
    /// <param name="builder">The builder instance.</param>
    /// <param name="dummyPoco">An uninitialized reference used to strongly type the query projection.</param>
    /// <param name="action">The delegate action defining the query structure.</param>
    /// <param name="alias">An optional explicit alias to use for this subquery.</param>
    /// <param name="varName">The compiler-injected variable name used for scoping.</param>
    /// <returns>A captured query fragment that can be interpolated back into the main stream.</returns>
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
    /// <typeparam name="T">The model type representing the resulting query projection.</typeparam>
    /// <param name="builder">The builder instance.</param>
    /// <param name="dummyPoco">An uninitialized reference used to strongly type the query projection.</param>
    /// <param name="existingQuery">The previously captured untyped query.</param>
    /// <param name="alias">An optional explicit alias to use for this subquery.</param>
    /// <param name="varName">The compiler-injected variable name used for scoping.</param>
    /// <returns>A strongly-typed query fragment.</returns>
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

    /// <summary>
    /// Captures an isolated query block into an output variable while maintaining the builder's fluent chain.
    /// </summary>
    /// <typeparam name="T">The model type representing the resulting query projection.</typeparam>
    /// <param name="builder">The builder instance.</param>
    /// <param name="dummyPoco">An uninitialized reference used to strongly type the query projection.</param>
    /// <param name="capturedSubquery">The output parameter where the captured query will be assigned.</param>
    /// <param name="action">The delegate action defining the query structure.</param>
    /// <param name="alias">An optional explicit alias to use for this subquery.</param>
    /// <param name="varName">The compiler-injected variable name used for scoping.</param>
    /// <returns>The current builder instance for method chaining.</returns>
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
    /// <typeparam name="T">The model type representing the resulting query projection.</typeparam>
    /// <param name="builder">The builder instance.</param>
    /// <param name="dummyPoco">An uninitialized reference mapped into the local scope.</param>
    /// <param name="existingQuery">The previously captured untyped query to map.</param>
    /// <param name="alias">An optional explicit alias to use for this subquery.</param>
    /// <param name="varName">The compiler-injected variable name used for scoping.</param>
    /// <returns>The current builder instance for method chaining.</returns>
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

    /// <summary>
    /// Dynamically constructs a column projection fragment from a string property name.
    /// </summary>
    /// <typeparam name="T">The entity type.</typeparam>
    /// <param name="builder">The builder instance.</param>
    /// <param name="dummyPoco">The scoped entity variable targeted for property lookup.</param>
    /// <param name="propertyName">The name of the property to resolve.</param>
    /// <param name="varName">The compiler-injected variable name representing the targeted entity.</param>
    /// <returns>A structural fragment representing the resolved column projection.</returns>
    /// <exception cref="ArgumentException">Thrown when the target variable name is not registered in the active builder scope.</exception>
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