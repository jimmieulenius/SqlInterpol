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
        return parts[^1]; // Modern index-from-end syntax
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
        // Safely create a dummy token without invoking any constructors
        if (typeof(T).IsValueType)
        {
            dummyPoco = default!;
        }
        else
        {
            dummyPoco = (T)RuntimeHelpers.GetUninitializedObject(typeof(T));
        }
        
        string? cleanVarName = ExtractVariableName(varName);
        
        // If an explicit alias is provided, use it. Otherwise, check the auto-aliasing flag.
        string? sqlAlias = alias ?? (builder.Context.Options.EntityAutoAliasing ? cleanVarName : null);
        
        var entity = ((ISqlEntityRegistry)builder).RegisterEntity<T>(alias: sqlAlias);
        
        if (!string.IsNullOrEmpty(cleanVarName))
        {
            builder.ScopedVariables[cleanVarName] = entity;
        }
        
        return builder;
    }

    /// <summary>
    /// Imports an externally passed entity reference into the current method's local scope.
    /// This seamlessly maps the existing AST node across method boundaries without registering a new table.
    /// </summary>
    public static SqlBuilder Entity<T>(
        this SqlBuilder builder,
        T passedVariable,
        out T localDummy,
        [CallerArgumentExpression(nameof(passedVariable))] string? passedName = null,
        [CallerArgumentExpression(nameof(localDummy))] string? localName = null)
    {
        // 1. Safely initialize the local dummy token
        if (typeof(T).IsValueType)
        {
            localDummy = default!;
        }
        else
        {
            localDummy = (T)RuntimeHelpers.GetUninitializedObject(typeof(T));
        }

        // 2. Extract the clean variable names
        string? cleanPassedName = ExtractVariableName(passedName);
        string? cleanLocalName = ExtractVariableName(localName);

        // 3. Map the underlying AST node from the passed name to the new local name
        if (!string.IsNullOrEmpty(cleanPassedName) && !string.IsNullOrEmpty(cleanLocalName))
        {
            if (builder.ScopedVariables.TryGetValue(cleanPassedName, out var entityNode))
            {
                builder.ScopedVariables[cleanLocalName] = entityNode;
            }
        }

        return builder;
    }

    /// <summary>
    /// Captures a subquery and binds its SQL definition to the specified dummy POCO variable.
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
            builder.ScopedVariables[cleanVarName] = typedQuery; // Upgrades Table to Subquery!
        }
        
        return typedQuery;
    }

    /// <summary>
    /// Captures a subquery and binds its SQL definition to the specified dummy POCO variable.
    /// Provides the current SqlBuilder to the lambda to avoid outer-variable shadowing.
    /// </summary>
    public static ISqlQuery<T> Subquery<T>(
        this SqlBuilder builder,
        T dummyPoco,
        Action<SqlBuilder> action,
        string? alias = null,
        [CallerArgumentExpression(nameof(dummyPoco))] string? varName = null)
    {
        // Wrap the Action<SqlBuilder> into a parameterless Action by passing the builder context in
        var innerQuery = builder.Query(() => action(builder));
        
        string? cleanVarName = ExtractVariableName(varName);
        string? sqlAlias = alias ?? (builder.Context.Options.EntityAutoAliasing ? cleanVarName : null);
        
        var typedQuery = new SqlQuery<T>(builder, innerQuery, sqlAlias);
        
        if (!string.IsNullOrEmpty(cleanVarName))
        {
            builder.ScopedVariables[cleanVarName] = typedQuery; // Upgrades Table to Subquery!
        }
        
        return typedQuery;
    }
}