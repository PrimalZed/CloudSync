using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using PrimalZed.CloudSync.Logging;
using PrimalZed.CloudSync.DependencyInjection;
using PrimalZed.CloudSync;

var builder = Host.CreateApplicationBuilder(args);
builder.Logging.AddEventLog(options => options.SourceName = builder.Configuration.GetValue<string>("Logging:EventLog:SourceName"));
builder.Services
	.AddCloudSyncWorker()
	.AddSingleton<EventLogRegistrar>()

  .AddHostedService<Worker>()
	//.AddHostedService<TestWorker>();
	// TODO: Register EventLog source: needs admin?
	.AddWindowsService();
var host = builder.Build();

await host.RunAsync();
