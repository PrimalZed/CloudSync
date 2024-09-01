using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
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
			.AddOptionsWithValidateOnStart<ClientOptions>()
			.Configure<IConfiguration>((options, config) => {
				options.Directory = @"C:\SyncTestClient";
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
			.AddSingleton<SyncProvider>()
			.AddSingleton<SyncRootReader>()
			.AddTransient<ClientWatcher>()
			.AddSingleton<CreateClientWatcher>((sp) => () => sp.GetRequiredService<ClientWatcher>())
			.AddSingleton<ClientWatcherFactory>()
			.AddTransient<RemoteWatcher>()
			.AddSingleton<CreateRemoteWatcher>((sp) => () => sp.GetRequiredService<RemoteWatcher>())
			.AddSingleton<RemoteWatcherFactory>()

			// Shell
			.AddCommonClassObjects()
			.AddLocalClassObjects()
			.AddSingleton<ShellRegistrar>()

			//.AddHostedService<Worker>()
			.AddHostedService<TestWorker>();
}
