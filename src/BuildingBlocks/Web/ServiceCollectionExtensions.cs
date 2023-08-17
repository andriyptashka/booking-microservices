namespace BuildingBlocks.Web;

using Microsoft.Extensions.DependencyInjection;
using NSubstitute;

public static class ServiceCollectionExtensions
{
    public static void ReplaceTransient<TService>(this IServiceCollection services,
        Func<IServiceProvider, TService> implementationFactory)
        where TService : class
    {
        services.Unregister<TService>();
        services.AddTransient(implementationFactory);
    }

    public static void ReplaceSingleton<TService, TImplementation>(this IServiceCollection services)
        where TService : class
        where TImplementation : class, TService
    {
        services.Unregister<TService>();
        services.AddSingleton<TService, TImplementation>();
    }

    public static void ReplaceSingleton<TService>(this IServiceCollection services,
        Func<IServiceProvider, TService> implementationFactory)
        where TService : class
    {
        services.Unregister<TService>();
        services.AddSingleton(implementationFactory);
    }

    public static void Unregister<TService>(this IServiceCollection services)
    {
        var descriptor = services.FirstOrDefault(d => d.ServiceType == typeof(TService));
        services.Remove(descriptor);
    }

    public static IServiceCollection ReplaceServiceWithSingletonMock<TService>(this IServiceCollection services)
        where TService : class
    {
        var service = services.FirstOrDefault(d => d.ServiceType == typeof(TService));
        services.Remove(service);

        services.AddSingleton(_ => Substitute.For<TService>());
        return services;
    }
}
