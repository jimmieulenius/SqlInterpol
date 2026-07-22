using System.Reflection;
using SqlInterpol.Pipeline;

namespace SqlInterpol.Configuration;

/// <summary>
/// A global, thread-safe registry that discovers and caches third-party extensions.
/// </summary>
public static class SqlExtensionRegistry
{
    private static readonly List<ISqlExtension> _aotRegistered = new();
    private static readonly object _lock = new();

    private static readonly Lazy<List<ISqlExtension>> _allExtensions = new(MergeAndDiscover, true);

    /// <summary>
    /// A clean hook for Extension Authors to globally register their package 
    /// using a [ModuleInitializer] (AOT Safe).
    /// </summary>
    public static void Register(ISqlExtension extension)
    {
        lock (_lock)
        {
            _aotRegistered.Add(extension);
        }
    }

    /// <summary>
    /// Gets the complete list of all global extensions, combining explicitly registered 
    /// modules and auto-discovered reflection modules.
    /// </summary>
    public static IReadOnlyList<ISqlExtension> GetGlobalExtensions() => _allExtensions.Value;

    private static List<ISqlExtension> MergeAndDiscover()
    {
        var finalExtensions = new List<ISqlExtension>();
        var registeredTypes = new HashSet<Type>();

        // 1. Add explicitly registered (Module Initializer) extensions first
        lock (_lock)
        {
            for (int i = 0; i < _aotRegistered.Count; i++)
            {
                var ext = _aotRegistered[i];
                if (registeredTypes.Add(ext.GetType()))
                {
                    finalExtensions.Add(ext);
                }
            }
        }

        // 2. Discover via Reflection (Zero-Touch JIT Fallback)
        var interfaceType = typeof(ISqlExtension);
        try
        {
            var baseDir = AppDomain.CurrentDomain.BaseDirectory;
            if (!string.IsNullOrEmpty(baseDir))
            {
                var dlls = Directory.GetFiles(baseDir, "SqlInterpol.*.dll");
                var loadedNames = new HashSet<string>(AppDomain.CurrentDomain.GetAssemblies().Select(a => a.GetName().Name!));

                foreach (var dll in dlls)
                {
                    var assemblyName = AssemblyName.GetAssemblyName(dll);
                    if (!loadedNames.Contains(assemblyName.Name!))
                    {
                        Assembly.Load(assemblyName);
                    }
                }
            }

            var targetAssemblies = AppDomain.CurrentDomain.GetAssemblies()
                .Where(a => !a.IsDynamic && a.GetName().Name?.StartsWith("SqlInterpol") == true);

            foreach (var assembly in targetAssemblies)
            {
                var types = assembly.GetExportedTypes()
                    .Where(t => interfaceType.IsAssignableFrom(t) && !t.IsInterface && !t.IsAbstract);

                foreach (var type in types)
                {
                    // Ensure we don't register the same extension twice if the ModuleInitializer already fired it!
                    if (registeredTypes.Add(type))
                    {
                        if (Activator.CreateInstance(type) is ISqlExtension instance)
                        {
                            finalExtensions.Add(instance);
                        }
                    }
                }
            }
        }
        catch
        {
            // Fail gracefully in constrained environments (e.g. Strict AOT)
        }

        return finalExtensions;
    }
}