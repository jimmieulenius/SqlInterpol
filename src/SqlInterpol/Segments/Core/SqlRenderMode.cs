namespace SqlInterpol.Segments;

/// <summary>
/// Dictates how a SQL fragment should format its output string during the rendering phase.
/// </summary>
public enum SqlRenderMode
{
    /// <summary>Renders the fragment according to its default standard representation.</summary>
    Default,

    /// <summary>Renders only the alias of the fragment (e.g., <c>"MyAlias"</c>).</summary>
    AliasOnly,

    /// <summary>Renders the raw base name of the fragment without aliases (e.g., <c>"users"</c>).</summary>
    BaseName,

    /// <summary>Renders the full declaration, typically combining the base name and the alias (e.g., <c>"users" AS "u"</c>).</summary>
    Declaration,

    /// <summary>Renders the fragment formatted explicitly as an alias assignment (e.g., <c>AS "MyAlias"</c>).</summary>
    AsAlias
}