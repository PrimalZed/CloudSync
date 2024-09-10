
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using PrimalZed.CloudSync;
using PrimalZed.CloudSync.App;
using PrimalZed.CloudSync.App.ViewModels;
using PrimalZed.CloudSync.DependencyInjection;
using PrimalZed.CloudSync.Remote.Local;
using PrimalZed.CloudSync.Remote.Sftp;
using PrimalZed.CloudSync.Shell;
using PrimalZed.CloudSync.Shell.DependencyInjection;

var builder = Host.CreateApplicationBuilder(args);

builder.Services
	//.AddLocalRemoteServices()
	.AddSftpRemoteServices()
	.AddCloudSyncWorker();

// Shell
builder.Services
	.AddCommonClassObjects()
	.AddLocalClassObjects()
	.AddSingleton<ShellRegistrar>();
	//.AddHostedService<ShellWorker>();

builder.Services
	.AddHostedService<SyncProviderWorker>()
	.AddSingleton<LocalContextViewModel>()
	.AddSingleton<SftpContextViewModel>()
	.AddSingleton<RegistrarViewModel>()
	.AddHostedService<AppService>();

var host = builder.Build();

WinRT.ComWrappersSupport.InitializeComWrappers();
// TODO: Efficiency mode?

await host.StartAsync();
