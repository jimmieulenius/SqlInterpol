using Microsoft.Extensions.DependencyInjection;
using SqlInterpol;
using SqlInterpol.Config;
using SqlInterpol.Parsing;

public static class ServiceCollectionExtensions
{
    extension (IServiceCollection services)
    {
        public IServiceCollection AddSqlInterpol(Action<SqlInterpolOptions>? configure = null)
        {
            var options = new SqlInterpolOptions();
            configure?.Invoke(options);
            services.AddSingleton(options);
            services.AddSingleton<ISqlParser>(sp => options.Parser ?? new DefaultSqlParser());
            return services;
        }
    }
}