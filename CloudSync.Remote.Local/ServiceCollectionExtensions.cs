using Microsoft.Extensions.DependencyInjection;
using PrimalZed.CloudSync.Remote.Abstractions;

namespace PrimalZed.CloudSync.Remote.Local; 
public static class ServiceCollectionExtensions {
	public static IServiceCollection AddLocalRemoteServices(this IServiceCollection services) =>
		services
			.AddSingleton<LocalContextAccessor>()
			.AddKeyedSingleton<IRemoteContextSetter>("local", (sp, key) => sp.GetRequiredService<LocalContextAccessor>())
			.AddSingleton((sp) => sp.GetRequiredKeyedService<IRemoteContextSetter>("local"))
			.AddSingleton<ILocalContextAccessor>((sp) => sp.GetRequiredService<LocalContextAccessor>())
			.AddKeyedScoped<IRemoteReadWriteService, LocalReadWriteService>("local")
			.AddScoped((sp) => new LazyRemote<IRemoteReadWriteService>(() => sp.GetRequiredKeyedService<IRemoteReadWriteService>("local"), LocalConstants.KIND))
			.AddKeyedScoped<IRemoteReadService>("local", (sp, key) => sp.GetRequiredService<IRemoteReadWriteService>())
			.AddScoped((sp) => new LazyRemote<IRemoteReadService>(() => sp.GetRequiredKeyedService<IRemoteReadService>("local"), LocalConstants.KIND))
			.AddKeyedScoped<IRemoteWatcher, LocalWatcher>("local")
			.AddScoped((sp) => new LazyRemote<IRemoteWatcher>(() => sp.GetRequiredKeyedService<IRemoteWatcher>("local"), LocalConstants.KIND));
}
