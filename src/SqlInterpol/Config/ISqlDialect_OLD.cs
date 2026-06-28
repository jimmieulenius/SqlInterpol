// namespace SqlInterpol;

// /// <summary>
// /// Defines the dialect-specific behavior for a SQL vendor: identifier quoting, parameter naming,
// /// supported features, and segment rewriting.
// /// </summary>
// /// <remarks>
// /// Built-in implementations are available via the <see cref="SqlBuilder"/> factory methods
// /// (<see cref="SqlBuilder.PostgreSql"/>, <see cref="SqlBuilder.SqlServer"/>, etc.).
// /// Implement this interface to support a custom or unsupported SQL dialect.
// /// </remarks>
// public interface ISqlDialect
// {
//     /// <summary>Gets the identifier for this dialect (e.g. <see cref="SqlDialectKind.PostgreSql"/>, <see cref="SqlDialectKind.SqlServer"/>).</summary>
//     SqlDialectKind Kind { get; }

//     /// <summary>Gets the opening delimiter for quoted identifiers (e.g. <c>"</c> for PostgreSQL, <c>[</c> for SQL Server).</summary>
//     string OpenQuote { get; }

//     /// <summary>Gets the closing delimiter for quoted identifiers (e.g. <c>"</c> for PostgreSQL, <c>]</c> for SQL Server).</summary>
//     string CloseQuote { get; }

//     /// <summary>Gets the prefix character for parameter names (e.g. <c>@</c> for SQL Server/PostgreSQL, <c>:</c> for Oracle).</summary>
//     string ParameterPrefix { get; }

//     /// <summary>
//     /// Gets the maximum number of parameters allowed in a single query by this dialect.
//     /// </summary>
//     int QueryParametersMaxCount { get; }

//     /// <summary>Gets the set of optional SQL features supported by this dialect.</summary>
//     /// <seealso cref="SqlFeature"/>
//     IReadOnlySet<SqlFeature> SupportedFeatures { get; }

//     /// <summary>
//     /// Determines whether the text immediately before an opening parenthesis represents a function
//     /// call or expression context, rather than a subquery or tuple.
//     /// </summary>
//     /// <param name="textBeforeParen">The SQL text fragment immediately preceding the <c>(</c>.</param>
//     /// <returns><see langword="true"/> if the context is a function call or expression; otherwise <see langword="false"/>.</returns>
//     bool IsExpressionContext(string textBeforeParen);

//     /// <summary>Wraps an identifier in this dialect's quote characters.</summary>
//     /// <param name="name">The unquoted identifier to wrap.</param>
//     /// <returns>The quoted identifier (e.g. <c>"MyColumn"</c> or <c>[MyColumn]</c>).</returns>
//     string QuoteIdentifier(string name);

//     /// <summary>Strips this dialect's quote characters from an identifier.</summary>
//     /// <param name="identifier">The possibly-quoted identifier.</param>
//     /// <returns>The bare identifier without surrounding quote characters.</returns>
//     string UnquoteIdentifier(string identifier);

//     /// <summary>Produces a fully qualified, quoted entity name with optional schema.</summary>
//     /// <param name="table">The physical table or view name.</param>
//     /// <param name="schema">The schema name, or <see langword="null"/> to omit schema qualification.</param>
//     /// <returns>The quoted, schema-qualified name (e.g. <c>"dbo"."Orders"</c>).</returns>
//     string QuoteEntityName(string table, string? schema = null);

//     /// <summary>Generates a parameter placeholder for the given zero-based index.</summary>
//     /// <param name="index">The zero-based parameter index.</param>
//     /// <returns>The parameter placeholder (e.g. <c>@p0</c>, <c>:p0</c>, <c>$1</c>).</returns>
//     string GetParameterName(int index);

//     /// <summary>Produces an aliased SQL expression using this dialect's <c>AS</c> syntax.</summary>
//     /// <param name="source">The SQL expression or identifier to alias.</param>
//     /// <param name="alias">The alias to apply, or <see langword="null"/> to return <paramref name="source"/> unchanged.</param>
//     /// <returns>The aliased expression (e.g. <c>"Products" AS "p"</c>).</returns>
//     string ApplyAlias(string source, string? alias = null);

//     /// <summary>Renders an <see cref="ISqlFragment"/> to a SQL string using this dialect's rules.</summary>
//     /// <param name="fragment">The fragment to render.</param>
//     /// <param name="context">The active context providing parameter state and options.</param>
//     /// <returns>The SQL string representation of <paramref name="fragment"/>.</returns>
//     string RenderFragment(ISqlFragment fragment, ISqlContext context);

//     /// <summary>Returns the default <see cref="SqlInterpolOptions"/> for this dialect.</summary>
//     /// <returns>A new <see cref="SqlInterpolOptions"/> with dialect-appropriate defaults.</returns>
//     SqlInterpolOptions GetDefaultOptions() => new();
// }