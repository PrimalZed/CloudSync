using Microsoft.Extensions.DependencyInjection;

namespace PrimalZed.CloudSync.Remote; 
public static class ServiceCollectionExtensions {
	public static IServiceCollection AddRemoteFactories(this IServiceCollection services) =>
		services
			.AddScoped<RemoteReadServiceFactory>()
			.AddScoped((sp) => sp.GetRequiredService<RemoteReadServiceFactory>().Create())
			.AddScoped<RemoteReadWriteServiceFactory>()
			.AddScoped((sp) => sp.GetRequiredService<RemoteReadWriteServiceFactory>().Create())
			.AddScoped<RemoteWatcherFactory>()
			.AddScoped((sp) => sp.GetRequiredService<RemoteWatcherFactory>().Create());
}
