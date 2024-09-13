using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using PrimalZed.CloudSync.Abstractions;
using PrimalZed.CloudSync.Configuration;
using PrimalZed.CloudSync.IO;
using PrimalZed.CloudSync.Remote;
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
			.AddSingleton<SyncProviderPool>()
			.AddSingleton<SyncProviderContextAccessor>()
			.AddSingleton<ISyncProviderContextAccessor>((sp) => sp.GetRequiredService<SyncProviderContextAccessor>())

			// Sync Provider services
			.AddRemoteFactories()
			.AddScoped<FileLocker>()
			.AddScoped((sp) =>
				Channel.CreateUnbounded<Func<Task>>(
					new UnboundedChannelOptions {
						SingleReader = true,
					}
				)
			)
			.AddScoped((sp) => sp.GetRequiredService<Channel<Func<Task>>>().Reader)
			.AddScoped((sp) => sp.GetRequiredService<Channel<Func<Task>>>().Writer)
			.AddScoped<TaskQueue>()
			.AddScoped<SyncProvider>()
			.AddScoped<SyncRootConnector>()
			.AddScoped<SyncRootRegistrar>()
			.AddScoped<PlaceholdersService>()
			.AddScoped<ClientWatcher>()
			.AddScoped<RemoteWatcher>();
}
