using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using PrimalZed.CloudSync;
using PrimalZed.CloudSync.DependencyInjection;
using PrimalZed.CloudSync.Management;
using PrimalZed.CloudSync.Management.Abstractions;
using PrimalZed.CloudSync.Remote.Abstractions;
using PrimalZed.CloudSync.Remote.Local;

var builder = Host.CreateApplicationBuilder(args);
builder.Services
	.AddCloudSyncWorker()
	.AddSingleton<IRemoteInfo, LocalRemoteInfo>()
	.AddSingleton<ISyncRootRegistrar, SyncRootRegistrar>()
  //.AddSingleton<Worker>()
  //.AddHostedService<HolisticWorker>();
  .AddHostedService<Worker>();
var host = builder.Build();

await host.RunAsync();
