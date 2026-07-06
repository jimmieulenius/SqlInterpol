using System.Runtime.CompilerServices;

namespace SqlInterpol.Test.XvSql.Functions;

/// <summary>
/// Automatically wires up the XvSql.Functions extension into the core engine 
/// the moment this assembly is loaded into memory.
/// </summary>
internal static class ExtensionAutoLoader
{
    #pragma warning disable CA2255
    [ModuleInitializer]
    internal static void Initialize()
    {
        SqlExtensionRegistry.Register(new XvSqlFunctionsExtension());
    }
    #pragma warning restore CA2255
}