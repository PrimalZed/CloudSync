using CloudSync.App;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using PrimalZed.CloudSync;
using PrimalZed.CloudSync.DependencyInjection;
using PrimalZed.CloudSync.Remote.Local;
using PrimalZed.CloudSync.Shell;
using PrimalZed.CloudSync.Shell.DependencyInjection;

var builder = Host.CreateApplicationBuilder(args);

builder.Services
	.AddLocalRemoteServices()
	.AddCloudSyncWorker();

// Shell
builder.Services
	.AddCommonClassObjects()
	.AddLocalClassObjects()
	.AddSingleton<ShellRegistrar>();
	//.AddHostedService<ShellWorker>();

builder.Services
	.AddHostedService<SyncProviderWorker>()
	.AddSingleton<RegistrarViewModel>()
	.AddHostedService<AppService>();

var host = builder.Build();

WinRT.ComWrappersSupport.InitializeComWrappers();
// TODO: Efficiency mode?

await host.StartAsync();
