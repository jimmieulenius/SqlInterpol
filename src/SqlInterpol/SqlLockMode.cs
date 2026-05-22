namespace SqlInterpol;

/// <summary>
/// Specifies the row-level lock hint applied by a <c>FOR UPDATE</c> / <c>FOR SHARE</c> clause.
/// The exact SQL emitted depends on the active dialect.
/// </summary>
public enum SqlLockMode
{
    /// <summary>Acquires an exclusive write lock on the selected rows (<c>FOR UPDATE</c> / <c>UPDLOCK</c>).</summary>
    Update,
    /// <summary>Acquires a shared read lock on the selected rows (<c>FOR SHARE</c> / <c>HOLDLOCK</c>).</summary>
    Share,
    /// <summary>Reads without acquiring any lock, allowing dirty reads (<c>NOLOCK</c> / <c>READ UNCOMMITTED</c>).</summary>
    NoLock
}