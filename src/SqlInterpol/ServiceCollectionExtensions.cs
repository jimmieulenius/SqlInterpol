using Microsoft.Extensions.Hosting;
using SqlInterpol.Config;
using SqlInterpol.Parsing;

namespace Microsoft.Extensions.DependencyInjection;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddSqlInterpol(this IServiceCollection services, Action<SqlInterpolOptions>? configure = null)
    {
        var options = new SqlInterpolOptions();
        configure?.Invoke(options);

        // If the user provided a factory, we use it to set the global instance
        // Note: This usually happens during the BuildServiceProvider phase 
        // or via a small 'bootstrapper' service.
        services.AddSingleton(options);
        
        // One way to set the static instance once the container is built:
        if (options.ParserFactory != null)
        {
            // We can't set it 'now' because we don't have the IServiceProvider yet.
            // We register a 'Initializer' service to do it.
            services.AddHostedService<SqlInterpolInitializer>();
        }

        return services;
    }
    
    // Moved to top-level public class for AddHostedService compatibility
    internal class SqlInterpolInitializer : IHostedService
    {
        private readonly IServiceProvider _sp;
        private readonly SqlInterpolOptions _opt;
    
        public SqlInterpolInitializer(IServiceProvider sp, SqlInterpolOptions opt)
        {
            _sp = sp;
            _opt = opt;
        }
    
        public Task StartAsync(CancellationToken ct)
        {
            if (_opt.ParserFactory != null)
                SqlParser.Instance = _opt.ParserFactory(_sp);

            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken ct) => Task.CompletedTask;
    }
}