using Microsoft.Extensions.DependencyInjection;
using PrimalZed.CloudSync.Remote.Abstractions;

namespace PrimalZed.CloudSync.Remote.Local; 
public static class ServiceCollectionExtensions {
	public static IServiceCollection AddLocalRemoteServices(this IServiceCollection services) =>
		services
			.AddSingleton<LocalContextAccessor>()
			.AddSingleton<IRemoteContextSetter>((sp) => sp.GetRequiredService<LocalContextAccessor>())
			.AddSingleton<ILocalContextAccessor>((sp) => sp.GetRequiredService<LocalContextAccessor>())
			.AddScoped<IRemoteReadWriteService, LocalRemoteReadWriteService>()
			.AddScoped<IRemoteReadService>((sp) => sp.GetRequiredService<IRemoteReadWriteService>())
			.AddScoped<IRemoteWatcher, LocalRemoteWatcher>();
}
