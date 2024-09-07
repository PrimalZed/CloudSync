using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
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
			.AddOptionsWithValidateOnStart<LocalRemoteOptions>()
			.Configure((options) => {
				options.AccountId = "TestAccount1";
				options.Directory = @"C:\SyncTestServer";
			})
			.Services
			.AddSingleton<IRemoteReadWriteService, LocalRemoteReadWriteService>()
			.AddSingleton<IRemoteReadService>((sp) => sp.GetRequiredService<IRemoteReadWriteService>())
			.AddTransient<IRemoteWatcher, LocalRemoteWatcher>()
			.AddSingleton<PlaceholdersService>()
			.AddSingleton<SyncProviderPool>()
			.AddSingleton<SyncProviderContextAccessor>()
			.AddSingleton<ISyncProviderContextAccessor>((sp) => sp.GetRequiredService<SyncProviderContextAccessor>())

			// Sync Provider services
			.AddScoped<SyncProvider>()
			.AddScoped<SyncRootConnector>()
			.AddScoped<SyncRootRegistrar>()
			.AddTransient<ClientWatcher>()
			.AddScoped<CreateClientWatcher>((sp) => (string rootDirectory) =>
				new ClientWatcher(rootDirectory, sp.GetRequiredService<IRemoteReadWriteService>(), sp.GetRequiredService<ILogger<ClientWatcher>>())
			)
			.AddScoped<ClientWatcherFactory>()
			.AddTransient<RemoteWatcher>()
			.AddScoped<CreateRemoteWatcher>((sp) => (string rootDirectory) =>
				new RemoteWatcher(rootDirectory, sp.GetRequiredService<IRemoteReadService>(), sp.GetRequiredService<IRemoteWatcher>(), sp.GetRequiredService<PlaceholdersService>(), sp.GetRequiredService<ILogger<RemoteWatcher>>())
			)
			.AddScoped<RemoteWatcherFactory>()

			// Shell
			.AddCommonClassObjects()
			.AddLocalClassObjects()
			.AddSingleton<ShellRegistrar>();
}
