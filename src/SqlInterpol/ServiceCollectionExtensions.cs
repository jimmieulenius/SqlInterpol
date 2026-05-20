using Microsoft.Extensions.DependencyInjection;
using SqlInterpol;
using SqlInterpol.Parsing;

public static class ServiceCollectionExtensions
{
    extension (IServiceCollection services)
    {
        public IServiceCollection AddSqlInterpol(SqlInterpolOptions? options = null)
        {
            var resolved = options ?? new SqlInterpolOptions();
            services.AddSingleton(resolved);
            services.AddSingleton<ISqlInterpolationParser>(sp => resolved.Parser ?? new SqlInterpolationParser());
            return services;
        }
    }
}