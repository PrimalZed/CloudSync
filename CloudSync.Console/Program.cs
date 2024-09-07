using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using PrimalZed.CloudSync;
using PrimalZed.CloudSync.DependencyInjection;

var builder = Host.CreateApplicationBuilder(args);
builder.Services
	.AddCloudSyncWorker()
//.AddSingleton<IRemoteInfo, LocalRemoteInfo>()
//.AddHostedService<SingleProcessWorker>()

.AddHostedService<SyncProviderWorker>();
var host = builder.Build();

await host.RunAsync();
