using System.Runtime.CompilerServices;

namespace SqlInterpol;

/// <summary>
/// Extension methods for <see cref="SqlBuilder"/> that provide strongly-typed entity registration
/// and scoped query construction using the modern, zero-allocation syntax.
/// </summary>
public static partial class SqlBuilderExtensions
{
    /// <summary>
    /// Safely extracts the actual variable name from a CallerArgumentExpression, 
    /// stripping away keywords like "out", "var", or explicit type declarations.
    /// </summary>
    private static string? ExtractVariableName(string? expression)
    {
        if (string.IsNullOrWhiteSpace(expression)) return null;
        
        var parts = expression.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        return parts[^1].Trim();
    }

    /// <summary>
    /// Registers an entity and outputs a dummy POCO for zero-allocation property routing.
    /// Returns the SqlBuilder to allow fluent chaining.
    /// </summary>
    public static SqlBuilder Entity<T>(
        this SqlBuilder builder, 
        out T dummyPoco, 
        string? alias = null,
        [CallerArgumentExpression(nameof(dummyPoco))] string? varName = null) 
    {
        if (typeof(T).IsValueType)
        {
            dummyPoco = default!;
        }
        else
        {
            dummyPoco = (T)RuntimeHelpers.GetUninitializedObject(typeof(T));
        }
        
        string? cleanVarName = ExtractVariableName(varName);
        string? sqlAlias = alias ?? (builder.Context.Options.EntityAutoAliasing ? cleanVarName : null);
        
        var entity = ((ISqlEntityRegistry)builder).RegisterEntity<T>(alias: sqlAlias);
        
        if (!string.IsNullOrEmpty(cleanVarName))
        {
            builder.ScopedVariables[cleanVarName] = entity;
        }
        
        return builder;
    }

    /// <summary>
    /// Looks up an existing entity by its key and binds it to a local dummy variable.
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

        if (typeof(T).IsValueType)
        {
            localDummy = default!;
        }
        else
        {
            localDummy = (T)RuntimeHelpers.GetUninitializedObject(typeof(T));
        }

        var cleanSource = CleanKey(sourceKey);
        var cleanLocal = CleanKey(localKey);

        if (!string.IsNullOrEmpty(cleanSource) && !string.IsNullOrEmpty(cleanLocal))
        {
            if (builder.ScopedVariables.TryGetValue(sourceKey!, out var node) ||
                builder.ScopedVariables.TryGetValue(cleanSource, out node))
            {
                builder.ScopedVariables[cleanLocal] = node;
            }
            else
            {
                foreach (var kvp in builder.ScopedVariables)
                {
                    if (kvp.Key.EndsWith(cleanSource))
                    {
                        builder.ScopedVariables[cleanLocal] = kvp.Value;
                        break;
                    }
                }
            }
        }

        return builder;
    }

    /// <summary>
    /// Captures a buildable subquery and binds its SQL definition to the specified dummy POCO variable.
    /// This upgrades the variable in the builder's scope from a Table to a Subquery AST node.
    /// </summary>
    public static ISqlQuery<T> Subquery<T>(
        this SqlBuilder builder,
        T dummyPoco,
        Action action,
        string? alias = null,
        [CallerArgumentExpression(nameof(dummyPoco))] string? varName = null)
    {
        var innerQuery = builder.Query(action);
        
        string? cleanVarName = ExtractVariableName(varName);
        string? sqlAlias = alias ?? (builder.Context.Options.EntityAutoAliasing ? cleanVarName : null);
        
        var typedQuery = new SqlQuery<T>(builder, innerQuery, sqlAlias);
        
        if (!string.IsNullOrEmpty(cleanVarName))
        {
            builder.ScopedVariables[cleanVarName] = typedQuery;
        }
        
        return typedQuery;
    }

    /// <summary>
    /// Captures a buildable subquery and binds its SQL definition to the specified dummy POCO variable.
    /// Provides the current SqlBuilder to the lambda to avoid outer-variable shadowing.
    /// </summary>
    public static ISqlQuery<T> Subquery<T>(
        this SqlBuilder builder,
        T dummyPoco,
        Action<SqlBuilder> action,
        string? alias = null,
        [CallerArgumentExpression(nameof(dummyPoco))] string? varName = null)
    {
        var innerQuery = builder.Query(() => action(builder));
        
        string? cleanVarName = ExtractVariableName(varName);
        string? sqlAlias = alias ?? (builder.Context.Options.EntityAutoAliasing ? cleanVarName : null);
        
        var typedQuery = new SqlQuery<T>(builder, innerQuery, sqlAlias);
        
        if (!string.IsNullOrEmpty(cleanVarName))
        {
            builder.ScopedVariables[cleanVarName] = typedQuery;
        }
        
        return typedQuery;
    }
}