using System.ComponentModel;
using SqlInterpol.Configuration;
using SqlInterpol.Segments;

namespace SqlInterpol.Infrastructure;

/// <summary>
/// The internal engine interface used by both the runtime pipeline and the AOT Source Generator 
/// to construct SQL queries. 
/// 
/// Note: These methods are intentionally hidden from the public SqlBuilder API via 
/// explicit interface implementation to prevent IntelliSense pollution for end-users.
/// </summary>
[EditorBrowsable(EditorBrowsableState.Never)]
public interface ISqlGeneratorBuilder
{
    /// <summary>
    /// Gets the configuration and dialect context for the current builder.
    /// </summary>
    ISqlContext Context { get; }

    /// <summary>
    /// Appends a raw string directly to the SQL stream.
    /// </summary>
    /// <param name="rawSql">The literal string to append.</param>
    /// <param name="segmentTag">Optional tags (e.g., <see cref="SqlSegmentTag.WhereKeyword"/>) 
    /// used to synchronize the runtime state tracker during JIT fallback scenarios.</param>
    void AppendRaw(string rawSql, params string[]? segmentTag);

    /// <summary>
    /// Appends a fully materialized segment (usually a parameter or a deferred fragment) to the stream.
    /// </summary>
    void AppendSegment(SqlSegment segment);

    /// <summary>
    /// Resolves the correct alias or physical table name for a tracked entity variable at runtime in O(1) time.
    /// Respects the <see cref="SqlInterpolOptions.EntityAutoAliasing"/> toggle.
    /// </summary>
    /// <param name="variableName">The C# local variable name (e.g., "ol").</param>
    /// <param name="defaultTableName">The physical table name mapped via Roslyn at compile-time.</param>
    /// <param name="wasAutoAliased">True if the alias was derived via CallerArgumentExpression; False if explicitly provided.</param>
    /// <returns>The resolved string to use in the SQL query.</returns>
    string ResolveAlias(string variableName, string defaultTableName, bool wasAutoAliased);

    /// <summary>
    /// Emits a full table declaration for the FROM or JOIN clause (e.g., <c>[dbo].[Products] AS [p]</c>).
    /// Respects the <see cref="SqlInterpolOptions.EntityAutoAliasing"/> toggle.
    /// </summary>
    /// <param name="tableName">The physical table name mapped via Roslyn at compile-time.</param>
    /// <param name="schema">The optional schema mapped via Roslyn at compile-time.</param>
    /// <param name="variableName">The C# local variable name.</param>
    /// <param name="wasAutoAliased">True if the alias was derived via CallerArgumentExpression.</param>
    void AppendDeclaration(string tableName, string? schema, string variableName, bool wasAutoAliased);
}