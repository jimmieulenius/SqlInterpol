using System.Runtime.CompilerServices;

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

    // =========================================================================
    // INSERT
    // =========================================================================

    public static SqlBuilder AppendInsert<TEntity, TDto>(
        this SqlBuilder builder, 
        TEntity entity, 
        params TDto[] payloads)
    {
        var template = SqlCrudTemplateCache.GetInsertTemplate<TEntity, TDto>(builder.Context.Dialect);
        return builder.Append(template, payloads);
    }

    public static SqlBuilder AppendInsert<TEntity, TDto>(
        this SqlBuilder builder, 
        TEntity entity, 
        IEnumerable<TDto> payloads)
    {
        var template = SqlCrudTemplateCache.GetInsertTemplate<TEntity, TDto>(builder.Context.Dialect);
        return builder.Append(template, payloads);
    }

    // =========================================================================
    // UPDATE
    // =========================================================================

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

    // =========================================================================
    // DELETE
    // =========================================================================

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