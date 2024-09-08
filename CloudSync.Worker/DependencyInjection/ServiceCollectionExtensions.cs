using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using PrimalZed.CloudSync.Abstractions;
using PrimalZed.CloudSync.Configuration;
using PrimalZed.CloudSync.IO;
using PrimalZed.CloudSync.Remote.Abstractions;
using PrimalZed.CloudSync.Remote.Local;
using PrimalZed.CloudSync.Shell;
using PrimalZed.CloudSync.Shell.DependencyInjection;

namespace PrimalZed.CloudSync.DependencyInjection; 
public static class ServiceCollectionExtensions {
	public static IServiceCollection AddCloudSyncWorker(this IServiceCollection services) =>
		services
			.AddOptionsWithValidateOnStart<ProviderOptions>()
			.Configure<IConfiguration>((options, config) => {
				options.ProviderId = "PrimalZed:CloudSync";
			})
			.Services
			.AddSingleton<SyncProviderPool>()
			.AddSingleton<SyncProviderContextAccessor>()
			.AddSingleton<ISyncProviderContextAccessor>((sp) => sp.GetRequiredService<SyncProviderContextAccessor>())
			.AddSingleton<LocalContextAccessor>()
			.AddSingleton<ILocalContextAccessor>((sp) => sp.GetRequiredService<LocalContextAccessor>())

			// Sync Provider services
			.AddScoped<SyncProvider>()
			.AddScoped<SyncRootConnector>()
			.AddScoped<SyncRootRegistrar>()
			.AddScoped<IRemoteReadWriteService, LocalRemoteReadWriteService>()
			.AddScoped<IRemoteReadService>((sp) => sp.GetRequiredService<IRemoteReadWriteService>())
			.AddScoped<IRemoteWatcher, LocalRemoteWatcher>()
			.AddScoped<PlaceholdersService>()
			.AddScoped<ClientWatcher>()
			.AddScoped<RemoteWatcher>()

			// Shell
			.AddCommonClassObjects()
			.AddLocalClassObjects()
			.AddSingleton<ShellRegistrar>();
}
