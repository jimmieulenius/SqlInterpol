
namespace SqlInterpol;

/// <summary>
/// Represents any SQL construct that can render itself to a SQL string given a dialect context.
/// </summary>
/// <remarks>
/// All higher-level abstractions — entities, references, fragments, and queries — implement
/// <see cref="ISqlFragment"/> so they can be embedded directly into interpolated SQL strings.
/// </remarks>
public interface ISqlFragment
{
    /// <summary>
    /// Renders this fragment to a SQL string using the specified dialect context and render mode.
    /// </summary>
    /// <param name="context">The active <see cref="ISqlContext"/> providing dialect, options, and parameter state.</param>
    /// <param name="mode">Controls how the fragment renders itself (e.g. alias-only, full declaration).</param>
    /// <returns>The SQL string representation of this fragment.</returns>
    string ToSql(ISqlContext context, SqlRenderMode mode = SqlRenderMode.Default);
}