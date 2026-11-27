namespace SqlInterpol.Testing.Xunit;

/// <summary>
/// Specifies how an E2E database seed is supplied.
/// </summary>
public enum SqlSeedKind
{
    /// <summary>
    /// A relative file or folder path under the suite's <c>Db/Scripts</c> directory.
    /// </summary>
    Path,

    /// <summary>
    /// Raw SQL text executed directly against the database.
    /// </summary>
    Inline
}