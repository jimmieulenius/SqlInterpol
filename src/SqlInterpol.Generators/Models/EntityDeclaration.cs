using System.Collections.Immutable;

namespace SqlInterpol.Generators;

/// <summary>
/// Represents a tracked entity variable produced by a <c>db.Entity&lt;T&gt;(out var name)</c>
/// call-site, as extracted by <see cref="SqlAotSyntaxWalker"/>.
/// </summary>
/// <param name="VariableName">The C# variable name bound via the <c>out</c> parameter (e.g. <c>"p"</c>).</param>
/// <param name="TypeName">The source-text representation of the type argument (e.g. <c>"Product"</c>).</param>
/// <param name="MappedTableName">The physical table/view name resolved from <c>[SqlTable]</c> or the class name.</param>
/// <param name="MappedSchemaName">The schema name, or <see langword="null"/> when the default schema is used.</param>
/// <param name="ExplicitAlias">The alias supplied explicitly by the caller, or <see langword="null"/> for auto-aliasing.</param>
/// <param name="WasAutoAliased"><see langword="true"/> when no explicit alias was provided and the emitter assigned one automatically.</param>
/// <param name="Columns">
/// Ordered column mappings extracted from the entity's property symbols.
/// Uses <see cref="ImmutableArray{T}"/> to provide value-equality semantics for the Roslyn incremental pipeline,
/// preventing spurious re-execution of the emit step when the entity schema has not changed.
/// </param>
public record EntityDeclaration(
    string VariableName,
    string TypeName,
    string MappedTableName,
    string? MappedSchemaName,
    string? ExplicitAlias,
    bool WasAutoAliased,
    ImmutableArray<ColumnMap> Columns
);

/// <summary>
/// Maps a C# property name to its physical SQL column name as resolved from <c>[SqlColumn]</c> attributes.
/// </summary>
/// <param name="PropertyName">The C# property name (e.g. <c>"Name"</c>).</param>
/// <param name="ColumnName">The physical column name (e.g. <c>"PROD_NAME"</c>).</param>
public record ColumnMap(string PropertyName, string ColumnName);