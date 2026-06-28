using Microsoft.Extensions.DependencyInjection;
using SqlInterpol;
using SqlInterpol.Parsing;

/// <summary>
/// Extension methods for registering SqlInterpol services with the
/// Microsoft dependency injection container.
/// </summary>
public static class ServiceCollectionExtensions
{
    extension (IServiceCollection services)
    {
        /// <summary>
        /// Registers the SqlInterpol options, segment preprocessor, and segment renderer
        /// as singletons in the dependency injection container.
        /// </summary>
        /// <remarks>
        /// After calling this method, <see cref="SqlInterpolOptions"/>,
        /// <see cref="ISqlSegmentPreprocessor"/>, and <see cref="ISqlSegmentRenderer"/> 
        /// can be injected into any service. A <see cref="SqlBuilder"/> is typically 
        /// created per-request using the dialect factory methods (e.g. <see cref="SqlBuilder.PostgreSql"/>).
        /// <code>
        /// builder.Services.AddSqlInterpol();
        /// </code>
        /// </remarks>
        /// <param name="options">Optional pre-configured options; a default instance is used when <see langword="null"/>.</param>
        /// <returns>The same <see cref="IServiceCollection"/> for method chaining.</returns>
        public IServiceCollection AddSqlInterpol(SqlInterpolOptions? options = null)
        {
            var resolved = options ?? new SqlInterpolOptions();
            
            services.AddSingleton(resolved);
            
            // Register the new pipeline middleware, falling back to the zero-allocation singletons
            services.AddSingleton<ISqlSegmentPreprocessor>(sp => resolved.Preprocessor ?? SqlSegmentPreprocessor.Instance);
            services.AddSingleton<ISqlSegmentRenderer>(sp => resolved.Renderer ?? SqlSegmentRenderer.Instance);
            
            return services;
        }
    }
}