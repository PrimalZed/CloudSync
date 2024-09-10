using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using PrimalZed.CloudSync;
using PrimalZed.CloudSync.DependencyInjection;
using PrimalZed.CloudSync.Remote.Local;
using PrimalZed.CloudSync.Remote.Sftp;

var builder = Host.CreateApplicationBuilder(args);
builder.Services
	.AddLocalRemoteServices()
	.AddSftpRemoteServices()
	.AddCloudSyncWorker()
	.AddHostedService<SingleProcessWorker>();

//.AddHostedService<SyncProviderWorker>();
var host = builder.Build();

await host.RunAsync();
