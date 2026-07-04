using System;
using Microsoft.Extensions.DependencyInjection;
using SqlInterpol.Parsing;

namespace SqlInterpol;

/// <summary>
/// Extension methods for registering SqlInterpol services with the
/// Microsoft dependency injection container.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers the SqlInterpol options, segment preprocessor, and segment renderer
    /// as singletons in the dependency injection container.
    /// </summary>
    public static IServiceCollection AddSqlInterpol(this IServiceCollection services, Action<SqlInterpolOptions>? configure = null)
    {
        // 1. Get the global factory defaults
        var options = SqlInterpolOptions.DefaultFactory?.Invoke() ?? new SqlInterpolOptions();
        
        // 2. Apply any DI-specific configurations on top
        configure?.Invoke(options);
        
        services.AddSingleton(options);
        services.AddSingleton(sp => options.Preprocessor ?? SqlSegmentPreprocessor.Instance);
        services.AddSingleton(sp => options.Renderer ?? SqlSegmentRenderer.Instance);
        
        return services;
    }
}