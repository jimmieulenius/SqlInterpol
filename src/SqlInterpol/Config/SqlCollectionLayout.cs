namespace SqlInterpol;

/// <summary>
/// Controls how a collection of values (e.g. an array or <see cref="System.Collections.IEnumerable"/>)
/// is rendered in SQL output.
/// </summary>
/// <seealso cref="SqlInterpolOptions.CollectionLayout"/>
public enum SqlCollectionLayout
{
    /// <summary>Renders all values on a single line, separated by <see cref="SqlInterpolOptions.CollectionSeparator"/>.</summary>
    Horizontal,

    /// <summary>Renders each value on its own line, indented by <see cref="SqlInterpolOptions.IndentSize"/> spaces.</summary>
    Vertical
}