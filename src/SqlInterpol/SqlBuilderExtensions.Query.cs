using System.Runtime.CompilerServices;

namespace SqlInterpol;

/// <summary>
/// Extension methods for <see cref="SqlBuilder"/> that provide strongly-typed entity registration
/// and scoped query construction using the modern, zero-allocation syntax.
/// </summary>
public static partial class SqlBuilderExtensions
{
    /// <summary>
    /// Registers an entity and outputs a dummy POCO for zero-allocation property routing.
    /// Returns the SqlBuilder to allow fluent chaining.
    /// </summary>
    /// <typeparam name="T">The CLR model type to map.</typeparam>
    /// <param name="builder">The builder to register the entity with.</param>
    /// <param name="dummyPoco">The output POCO used for column routing.</param>
    /// <param name="varName">Automatically captured C# variable name.</param>
    /// <returns>The <see cref="SqlBuilder"/> for fluent chaining.</returns>
    public static SqlBuilder Entity<T>(
        this SqlBuilder builder, 
        out T dummyPoco, 
        [CallerArgumentExpression(nameof(dummyPoco))] string? varName = null) 
        where T : new()
    {
        dummyPoco = new T(); 
        
        string? sqlAlias = builder.Context.Options.AutoAliasing ? varName : null;
        var entity = ((ISqlEntityRegistry)builder).RegisterEntity<T>(alias: sqlAlias);
        
        if (!string.IsNullOrEmpty(varName))
        {
            builder.ScopedVariables[varName] = entity;
        }
        
        return builder;
    }

    /// <summary>
    /// Captures a subquery and binds its SQL definition to the specified dummy POCO variable.
    /// This upgrades the variable in the builder's scope from a Table to a Subquery AST node.
    /// </summary>
    /// <typeparam name="T">The CLR model type the subquery is bound to.</typeparam>
    /// <param name="builder">The builder capturing the query.</param>
    /// <param name="dummyPoco">The existing dummy POCO to bind the subquery to.</param>
    /// <param name="action">The action that appends the subquery's SQL to the builder.</param>
    /// <param name="varName">Automatically captured C# variable name.</param>
    /// <returns>The typed <see cref="ISqlQuery{T}"/> containing the captured subquery.</returns>
    public static ISqlQuery<T> Query<T>(
        this SqlBuilder builder,
        T dummyPoco,
        Action action,
        [CallerArgumentExpression(nameof(dummyPoco))] string? varName = null)
    {
        var innerQuery = builder.Query(action);
        
        string? sqlAlias = builder.Context.Options.AutoAliasing ? varName : null;
        var typedQuery = new SqlQuery<T>(builder, innerQuery, sqlAlias);
        
        if (!string.IsNullOrEmpty(varName))
        {
            builder.ScopedVariables[varName] = typedQuery; // Upgrades Table to Subquery!
        }
        
        return typedQuery;
    }
}