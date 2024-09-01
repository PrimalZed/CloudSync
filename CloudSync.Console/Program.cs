using Microsoft.Extensions.Hosting;
using PrimalZed.CloudSync.DependencyInjection;

var builder = Host.CreateApplicationBuilder(args);
builder.Services
	.AddCloudSyncWorker();
var host = builder.Build();

await host.RunAsync();
