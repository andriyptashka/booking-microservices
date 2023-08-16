namespace BuildingBlocks.PersistMessageProcessor;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

public class PersistMessageBackgroundService : BackgroundService
{
    private readonly ILogger<PersistMessageBackgroundService> _logger;
    private readonly IServiceProvider _serviceProvider;
    private readonly PersistMessageOptions _options;

    private Task? _executingTask;

    public PersistMessageBackgroundService(
        ILogger<PersistMessageBackgroundService> logger,
        IServiceProvider serviceProvider,
        IOptions<PersistMessageOptions> options)
    {
        ArgumentNullException.ThrowIfNull(logger, nameof(logger));
        ArgumentNullException.ThrowIfNull(serviceProvider, nameof(serviceProvider));
        ArgumentNullException.ThrowIfNull(options, nameof(options));
        _logger = logger;
        _serviceProvider = serviceProvider;
        _options = options.Value;
    }

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("PersistMessage Background Service Starting");

        _executingTask = ProcessAsync(stoppingToken);

        return _executingTask;
    }

    public override Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("PersistMessage Background Service Stopping");

        return base.StopAsync(cancellationToken);
    }

    private async Task ProcessAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            await using (var scope = _serviceProvider.CreateAsyncScope())
            {
                var service = scope.ServiceProvider.GetRequiredService<IPersistMessageProcessor>();
                await service.ProcessAllAsync(stoppingToken);
            }

            var delay = _options.Interval is { }
                ? TimeSpan.FromSeconds((int)_options.Interval)
                : TimeSpan.FromSeconds(30);

            await Task.Delay(delay, stoppingToken);
        }
    }
}
