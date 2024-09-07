using CloudSync.App;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using PrimalZed.CloudSync;
using PrimalZed.CloudSync.DependencyInjection;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddCloudSyncWorker();

builder.Services
	.AddHostedService<SyncProviderWorker>()
	.AddSingleton<SyncRootViewModel>()
	.AddHostedService<AppService>();

var host = builder.Build();

WinRT.ComWrappersSupport.InitializeComWrappers();
// TODO: Efficiency mode?

await host.StartAsync();
