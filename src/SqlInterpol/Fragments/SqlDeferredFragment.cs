
namespace SqlInterpol;

/// <summary>
/// An internal fragment whose SQL string is computed lazily via a delegate at render time.
/// </summary>
/// <param name="renderer">The delegate invoked to produce the SQL string.</param>
internal sealed class SqlDeferredFragment(Func<ISqlContext, string> renderer) : ISqlFragment
{
    /// <inheritdoc />
    public string ToSql(ISqlContext context, SqlRenderMode mode = SqlRenderMode.Default) 
        => renderer(context);
}