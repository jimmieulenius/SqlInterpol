using Microsoft.Extensions.DependencyInjection;
using SqlInterpol;
using SqlInterpol.Config;
using SqlInterpol.Parsing;

public static class ServiceCollectionExtensions
{
    extension (IServiceCollection services)
    {
        public IServiceCollection AddSqlInterpol(SqlInterpolOptions? options = null)
        {
            var resolved = options ?? new SqlInterpolOptions();
            services.AddSingleton(resolved);
            services.AddSingleton<ISqlParser>(sp => resolved.Parser ?? new DefaultSqlParser());
            return services;
        }
    }
}