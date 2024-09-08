using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using PrimalZed.CloudSync;
using PrimalZed.CloudSync.DependencyInjection;
using PrimalZed.CloudSync.Remote.Local;

var builder = Host.CreateApplicationBuilder(args);
builder.Services
	.AddLocalRemoteServices()
	.AddCloudSyncWorker()
	.AddHostedService<SingleProcessWorker>();

//.AddHostedService<SyncProviderWorker>();
var host = builder.Build();

await host.RunAsync();
