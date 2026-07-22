using Microsoft.Extensions.DependencyInjection;
using SqlInterpol.Configuration;
using SqlInterpol.Pipeline;

namespace SqlInterpol;

/// <summary>
/// Extension methods for registering SqlInterpol services with the Microsoft dependency injection container.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers the SqlInterpol options, segment preprocessor, and segment renderer
    /// as singletons in the dependency injection container.
    /// </summary>
    /// <param name="services">The service collection to add the SQL services to.</param>
    /// <param name="configure">An optional action to configure the global options.</param>
    /// <returns>The original service collection for method chaining.</returns>
    public static IServiceCollection AddSqlInterpol(this IServiceCollection services, Action<SqlInterpolOptions>? configure = null)
    {
        var options = SqlInterpolOptions.DefaultFactory?.Invoke() ?? new SqlInterpolOptions();
        
        configure?.Invoke(options);
        
        services.AddSingleton(options);
        services.AddSingleton(sp => options.Preprocessor ?? SqlSegmentPreprocessor.Instance);
        services.AddSingleton(sp => options.Renderer ?? SqlSegmentRenderer.Instance);
        
        return services;
    }
}