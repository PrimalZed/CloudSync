using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using PrimalZed.CloudSync.Async;
using PrimalZed.CloudSync.Logging;

namespace PrimalZed.CloudSync;
internal class TestWorker(
	EventLogRegistrar eventLogRegistrar,
	IHostApplicationLifetime applicationLifetime,
	ILogger<Worker> logger
) : BackgroundService {
	protected override async Task ExecuteAsync(CancellationToken stoppingToken) {
		eventLogRegistrar.Register();

		logger.LogInformation("test service");

		await stoppingToken;

		logger.LogInformation("stopping");

		applicationLifetime.StopApplication();
	}
}
