namespace BuildingBlocks.Web;

using Microsoft.Extensions.Hosting;

public static class ServiceProviderExtensions
{
    public static async Task StartTestHostedServices(this IServiceProvider serviceProvider,
        Type[] serviceTypes,
        CancellationToken token = default)
    {
        foreach (var serviceType in serviceTypes)
        {
            if (serviceProvider.GetService(serviceType) is IHostedService hostedService)
            {
                await hostedService.StartAsync(token);
            }
        }
    }

    public static async Task StopTestHostedServices(this IServiceProvider serviceProvider,
        Type[] serviceTypes,
        CancellationToken token = default)
    {
        foreach (var serviceType in serviceTypes)
        {
            if (serviceProvider.GetService(serviceType) is IHostedService hostedService)
            {
                await hostedService.StopAsync(token);
            }
        }
    }
}
