using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using PrimalZed.CloudSync.Async;

namespace PrimalZed.CloudSync;
public sealed class TestWorker(
	IHostApplicationLifetime applicationLifetime,
	ILogger<TestWorker> logger
) : BackgroundService {
	protected override async Task ExecuteAsync(CancellationToken stoppingToken) {
		logger.LogInformation("test service");

		await stoppingToken;

		logger.LogInformation("stopping");

		applicationLifetime.StopApplication();
	}
}
