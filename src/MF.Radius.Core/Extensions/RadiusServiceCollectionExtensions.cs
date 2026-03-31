using MF.Radius.Core.Interfaces;
using MF.Radius.Core.Options;
using MF.Radius.Core.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace MF.Radius.Core.Extensions;

/// <summary>
/// Provides extension methods for configuring RADIUS-related services in an <see cref="IServiceCollection"/>.
/// </summary>
public static class RadiusServiceCollectionExtensions
{
    /// <summary>
    /// Registers a RADIUS server components with the dependency injection system.
    /// Configures the required services, hosted service, and processor implementation.
    /// </summary>
    /// <typeparam name="TProcessor">
    /// The type of the RADIUS processor to be used. The type must implement <see cref="IRadiusProcessor"/>.
    /// </typeparam>
    /// <param name="services">
    /// The <see cref="IServiceCollection"/> to which the RADIUS server services are added.
    /// </param>
    /// <param name="setupAction">
    /// An optional configuration delegate to configure the <see cref="RadiusListenerOptions"/>.
    /// </param>
    /// <returns>
    /// The modified instance of the <see cref="IServiceCollection"/>.
    /// </returns>
    public static IServiceCollection AddRadiusServer<TProcessor>(
        this IServiceCollection services, 
        Action<RadiusListenerOptions>? setupAction = null
    )
        where TProcessor : class, IRadiusProcessor
    {
        if (setupAction != null)
            services.Configure(setupAction);
        
        services.TryAddScoped<IRadiusProcessor, TProcessor>();
        services.AddHostedService<RadiusListener>();
        services.TryAddSingleton<IRadiusSender, RadiusSender>();
        
        return services;
    }
    
}
