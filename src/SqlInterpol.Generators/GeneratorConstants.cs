namespace SqlInterpol.Generators;

/// <summary>
/// Well-known string constants used throughout the AOT source generator pipeline
/// to avoid magic-string duplication and make refactoring safe.
/// </summary>
internal static class GeneratorConstants
{
    // MSBuild property keys (must be lowercase; MSBuild normalises them)
    /// <summary>MSBuild property key to disable AOT generation.</summary>
    public const string MsBuildDisableAot = "build_property.sqlinterpoldisableaot";

    /// <summary>MSBuild property key specifying the comma-separated list of target dialects.</summary>
    public const string MsBuildDialects = "build_property.sqlinterpoldialects";

    /// <summary>Default dialect used when <see cref="MsBuildDialects"/> is absent.</summary>
    public const string DefaultDialect = "PostgreSql";

    // SqlBuilder method names targeted by the walker and emitter
    /// <summary>The <c>Append</c> method name.</summary>
    public const string MethodAppend = "Append";

    /// <summary>The <c>AppendLine</c> method name.</summary>
    public const string MethodAppendLine = "AppendLine";

    /// <summary>The <c>Entity</c> method name.</summary>
    public const string MethodEntity = "Entity";

    /// <summary>The <c>Query</c> method name.</summary>
    public const string MethodQuery = "Query";

    /// <summary>The <c>Template</c> method name.</summary>
    public const string MethodTemplate = "Template";

    // Interpolation format specifiers
    /// <summary>Format specifier that renders an entity as a full declaration (e.g., <c>FROM [Table] AS [t]</c>).</summary>
    public const string FormatDeclaration = "decl";

    /// <summary>Format specifier that renders only the alias portion of an entity or column.</summary>
    public const string FormatAlias = "alias";

    /// <summary>Format specifier that renders only the base (physical) name of an entity.</summary>
    public const string FormatBaseName = "base";

    /// <summary>Format specifier that renders only the column name (no qualifier).</summary>
    public const string FormatColumn = "col";

    // Extension method names that set an explicit render mode on an interpolation hole.
    // These must stay in sync with the actual extension method names on SqlInterpol's entity types.
    /// <summary>Extension method name that sets the <c>decl</c> render mode on a hole.</summary>
    public const string ExtensionAsDeclaration = "AsDeclaration";

    /// <summary>Extension method name that sets the <c>alias</c> render mode on a hole.</summary>
    public const string ExtensionAsAlias = "AsAlias";

    /// <summary>Extension method name that sets the <c>base</c> render mode on a hole.</summary>
    public const string ExtensionAsBase = "AsBase";

    /// <summary>Extension method name that sets the <c>col</c> render mode on a hole.</summary>
    public const string ExtensionAsColumn = "AsColumn";

    // Named argument / parameter names used in Entity<T>() calls
    /// <summary>Named argument <c>alias</c> in <c>db.Entity&lt;T&gt;(alias: "a", out var t)</c>.</summary>
    public const string ParamAlias = "alias";

    /// <summary>Named argument <c>name</c> (physical table name override).</summary>
    public const string ParamName = "name";

    /// <summary>Named argument <c>schema</c> (schema name override).</summary>
    public const string ParamSchema = "schema";

    // Diagnostic IDs — kept consistent with the SQLIG prefix
    /// <summary>Diagnostic emitted when AOT generation is disabled via MSBuild.</summary>
    public const string DiagnosticAotDisabled = "SQLIG03";

    /// <summary>Diagnostic emitted when a <c>[SqlQuery]</c> container class is not static.</summary>
    public const string DiagnosticInvalidContainer = "SQLIG01";

    /// <summary>Diagnostic emitted when a <c>[SqlQuery]</c> method is an extension method.</summary>
    public const string DiagnosticInvalidSignature = "SQLIG02";
}
