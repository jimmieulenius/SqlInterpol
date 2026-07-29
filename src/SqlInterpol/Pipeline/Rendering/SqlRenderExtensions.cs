using System.Runtime.CompilerServices;
using SqlInterpol.Segments;

namespace SqlInterpol;

/// <summary>
/// Fluent extension methods to replace format string specifiers (e.g., :decl, :col, :alias)
/// with strongly-typed, IntelliSense-discoverable method calls.
/// </summary>
public static class SqlRenderExtensions
{
    /// <summary>
    /// Instructs the query builder to render this entity as a full table declaration (e.g., [dbo].[Table] AS [Alias]).
    /// Replaces the ":decl" format specifier.
    /// </summary>
    public static SqlRenderModifier<T> AsDeclaration<T>(this T entity, [CallerArgumentExpression("entity")] string? expression = null)
    {
        return new SqlRenderModifier<T>(entity, SqlRenderMode.Declaration, expression);
    }

    /// <summary>
    /// Instructs the query builder to render only the alias of this entity or column (e.g., [Alias]).
    /// Replaces the ":alias" format specifier.
    /// </summary>
    public static SqlRenderModifier<T> AsAlias<T>(this T entityOrColumn, [CallerArgumentExpression("entityOrColumn")] string? expression = null)
    {
        return new SqlRenderModifier<T>(entityOrColumn, SqlRenderMode.AliasOnly, expression);
    }

    /// <summary>
    /// Instructs the query builder to render only the base physical name without schema or alias qualifications.
    /// Replaces the ":base" format specifier.
    /// </summary>
    public static SqlRenderModifier<T> AsBase<T>(this T entity, [CallerArgumentExpression("entity")] string? expression = null)
    {
        return new SqlRenderModifier<T>(entity, SqlRenderMode.BaseName, expression);
    }

    /// <summary>
    /// Instructs the query builder to render this property strictly as its base physical column name.
    /// Replaces the ":col" format specifier.
    /// </summary>
    public static SqlRenderModifier<T> AsColumn<T>(this T column, [CallerArgumentExpression("column")] string? expression = null)
    {
        return new SqlRenderModifier<T>(column, SqlRenderMode.BaseName, expression);
    }
}