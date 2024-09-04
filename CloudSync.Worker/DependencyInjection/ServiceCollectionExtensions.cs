using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using PrimalZed.CloudSync.Configuration;
using PrimalZed.CloudSync.IO;
using PrimalZed.CloudSync.Pipes;
using PrimalZed.CloudSync.Remote.Abstractions;
using PrimalZed.CloudSync.Remote.Local;
using PrimalZed.CloudSync.Shell;
using PrimalZed.CloudSync.Shell.DependencyInjection;
using System.Threading.Channels;

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
			.AddKeyedSingleton("registrar", (sp, key) => new PipeServer(PipeNames.SYNC_ROOT_REGISTRAR, 3, sp.GetRequiredService<ILogger<PipeServer>>()))
			.AddSingleton<SyncRootRegistrar>()
			.AddSingleton<SyncProviderPool>()
			.AddTransient<ClientWatcher>()
			.AddSingleton<CreateClientWatcher>((sp) => () => sp.GetRequiredService<ClientWatcher>())
			.AddSingleton<ClientWatcherFactory>()
			.AddTransient<RemoteWatcher>()
			.AddSingleton<CreateRemoteWatcher>((sp) => () => sp.GetRequiredService<RemoteWatcher>())
			.AddSingleton<RemoteWatcherFactory>()

			// Shell
			.AddCommonClassObjects()
			.AddLocalClassObjects()
			.AddSingleton<ShellRegistrar>();
}
