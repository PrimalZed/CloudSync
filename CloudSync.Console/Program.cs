using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using PrimalZed.CloudSync;
using PrimalZed.CloudSync.DependencyInjection;
using PrimalZed.CloudSync.Remote.Abstractions;
using PrimalZed.CloudSync.Remote.Local;

var builder = Host.CreateApplicationBuilder(args);
builder.Services
	.AddCloudSyncWorker()
//.AddSingleton<IRemoteInfo, LocalRemoteInfo>()
//.AddHostedService<SingleProcessWorker>()

//.AddHostedService<PipeClientWorker>()
//.AddHostedService<PipeServerWorker>()

.AddHostedService<SyncProviderWorker>()
.AddHostedService<PipeWorker>();
var host = builder.Build();

await host.RunAsync();
