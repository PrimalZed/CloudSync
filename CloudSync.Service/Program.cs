using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using PrimalZed.CloudSync.Logging;
using PrimalZed.CloudSync.DependencyInjection;

var builder = Host.CreateApplicationBuilder(args);
builder.Logging.AddEventLog(options => options.SourceName = builder.Configuration.GetValue<string>("Logging:EventLog:SourceName"));
builder.Services
	.AddCloudSyncWorker()
	.AddSingleton<EventLogRegistrar>()
	// TODO: Register EventLog source
	.AddWindowsService();
var host = builder.Build();

await host.RunAsync();
