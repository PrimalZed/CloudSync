using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using PrimalZed.CloudSync;
using PrimalZed.CloudSync.Configuration;
using PrimalZed.CloudSync.Remote.Abstractions;
using PrimalZed.CloudSync.IO;
using PrimalZed.CloudSync.Remote.Local;
using PrimalZed.CloudSync.Shell.DependencyInjection;
using PrimalZed.CloudSync.Shell;

var builder = Host.CreateApplicationBuilder(args);
builder.Services
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
	.AddSingleton<IRemoteInfo, LocalRemoteInfo>()
	.AddSingleton<IRemoteReadWriteService, LocalRemoteReadWriteService>()
	.AddSingleton<IRemoteReadService>((sp) => sp.GetRequiredService<IRemoteReadWriteService>())
	.AddTransient<IRemoteWatcher, LocalRemoteWatcher>()
	.AddSingleton<PlaceholdersService>()
	.AddSingleton<SyncProvider>()
	.AddSingleton<SyncRootRegistrar>()
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
	.AddHostedService<SyncBackgroundService>();
var host = builder.Build();

var logger = host.Services.GetRequiredService<ILogger<Program>>();

await host.RunAsync();

logger.LogInformation("Stopped");
