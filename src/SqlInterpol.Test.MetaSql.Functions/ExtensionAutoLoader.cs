using System.Runtime.CompilerServices;

namespace SqlInterpol.Test.MetaSql.Functions;

/// <summary>
/// Automatically wires up the MetaSql.Functions extension into the core engine 
/// the moment this assembly is loaded into memory.
/// </summary>
internal static class ExtensionAutoLoader
{
    #pragma warning disable CA2255
    [ModuleInitializer]
    internal static void Initialize()
    {
        SqlExtensionRegistry.Register(new SqlMetaSqlFunctionsExtension());
    }
    #pragma warning restore CA2255
}