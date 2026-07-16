using System.Runtime.CompilerServices;
using SqlInterpol.Execution;

namespace SqlInterpol;

/// <summary>
/// Provides fluent extension methods to append optimized, globally cached CRUD operations.
/// </summary>
public static partial class SqlBuilderExtensions
{
    private static string[] ExtractKeyNames<TKey>(string? callerExpression)
    {
        var type = typeof(TKey);
        if (type.Name.Contains("AnonymousType"))
        {
            return type.GetProperties().Select(p => p.Name).ToArray();
        }

        if (string.IsNullOrWhiteSpace(callerExpression))
            throw new ArgumentException("Could not resolve key columns from the provided key selector.");

        var expr = callerExpression.Trim();
        if (expr.StartsWith("(") && expr.EndsWith(")"))
        {
            return expr.Trim('(', ')')
                       .Split(',')
                       .Select(p => p.Split('.').Last().Trim())
                       .ToArray();
        }

        return [expr.Split('.').Last().Trim()];
    }

    /// <summary>
    /// Appends a highly-optimized, globally cached INSERT statement for multiple payloads.
    /// </summary>
    /// <typeparam name="TEntity">The target table model type.</typeparam>
    /// <typeparam name="TDto">The data transfer object type defining properties to persist.</typeparam>
    /// <param name="builder">The builder instance.</param>
    /// <param name="entity">The target entity reference.</param>
    /// <param name="payloads">An array of data payloads to insert.</param>
    /// <returns>The current builder instance for method chaining.</returns>
    public static SqlBuilder AppendInsert<TEntity, TDto>(
        this SqlBuilder builder, 
        TEntity entity, 
        params TDto[] payloads)
    {
        var template = SqlCrudTemplateCache.GetInsertTemplate<TEntity, TDto>(builder.Context.Dialect);
        return builder.Append(template, payloads);
    }

    /// <summary>
    /// Appends a highly-optimized, globally cached INSERT statement for an enumerable of payloads.
    /// </summary>
    /// <typeparam name="TEntity">The target table model type.</typeparam>
    /// <typeparam name="TDto">The data transfer object type defining properties to persist.</typeparam>
    /// <param name="builder">The builder instance.</param>
    /// <param name="entity">The target entity reference.</param>
    /// <param name="payloads">An enumerable sequence of data payloads to insert.</param>
    /// <returns>The current builder instance for method chaining.</returns>
    public static SqlBuilder AppendInsert<TEntity, TDto>(
        this SqlBuilder builder, 
        TEntity entity, 
        IEnumerable<TDto> payloads)
    {
        var template = SqlCrudTemplateCache.GetInsertTemplate<TEntity, TDto>(builder.Context.Dialect);
        return builder.Append(template, payloads);
    }

    /// <summary>
    /// Appends a highly-optimized, globally cached UPDATE statement utilizing a key lambda selector.
    /// </summary>
    /// <typeparam name="TEntity">The target table model type.</typeparam>
    /// <typeparam name="TKey">The type of the selected key structure.</typeparam>
    /// <typeparam name="TDto">The data transfer object type containing fields to modify.</typeparam>
    /// <param name="builder">The builder instance.</param>
    /// <param name="entity">The target entity reference.</param>
    /// <param name="keySelector">A selector designating the key properties (automatically mapped via CallerArgumentExpression).</param>
    /// <param name="payload">The data payload containing values for the update.</param>
    /// <param name="keyExpression">The compiler-injected string representing the key selection expression.</param>
    /// <returns>The current builder instance for method chaining.</returns>
    public static SqlBuilder AppendUpdate<TEntity, TKey, TDto>(
        this SqlBuilder builder, 
        TEntity entity, 
        TKey keySelector,
        TDto payload,
        [CallerArgumentExpression(nameof(keySelector))] string? keyExpression = null)
    {
        var keys = ExtractKeyNames<TKey>(keyExpression);
        var template = SqlCrudTemplateCache.GetUpdateTemplate<TEntity, TDto>(builder.Context.Dialect, keys);
        
        // Wrap scalars defensively, pass through native enumerables
        object args = (payload is System.Collections.IEnumerable and not string) ? payload : new[] { payload };
        return builder.Append(template, args);
    }

    /// <summary>
    /// Appends a highly-optimized, globally cached DELETE statement utilizing a key lambda selector.
    /// </summary>
    /// <typeparam name="TEntity">The target table model type.</typeparam>
    /// <typeparam name="TKey">The type of the selected key structure.</typeparam>
    /// <typeparam name="TDto">The filter payload model type.</typeparam>
    /// <param name="builder">The builder instance.</param>
    /// <param name="entity">The target entity reference.</param>
    /// <param name="keySelector">A selector designating the key properties (automatically mapped via CallerArgumentExpression).</param>
    /// <param name="payload">The filter payload containing values for the deletion condition.</param>
    /// <param name="keyExpression">The compiler-injected string representing the key selection expression.</param>
    /// <returns>The current builder instance for method chaining.</returns>
    public static SqlBuilder AppendDelete<TEntity, TKey, TDto>(
        this SqlBuilder builder, 
        TEntity entity, 
        TKey keySelector,
        TDto payload,
        [CallerArgumentExpression(nameof(keySelector))] string? keyExpression = null)
    {
        var keys = ExtractKeyNames<TKey>(keyExpression);
        var template = SqlCrudTemplateCache.GetDeleteTemplate<TEntity, TDto>(builder.Context.Dialect, keys);
        
        // Wrap scalars defensively, pass through native enumerables
        object args = (payload is System.Collections.IEnumerable and not string) ? payload : new[] { payload };
        return builder.Append(template, args);
    }
}