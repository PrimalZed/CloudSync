using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using PrimalZed.CloudSync.Pipes;

namespace PrimalZed.CloudSync;
public class PipeClientWorker(ILogger<PipeClientWorker> logger) : BackgroundService {
	protected override async Task ExecuteAsync(CancellationToken stoppingToken) {
		await Task.Delay(500, stoppingToken);
		using var pipeClient = new PipeClient(PipeNames.SYNC_ROOT_REGISTRAR);
		pipeClient.ReceivedMessage += PipeClient_ReceivedMessage;

		while (!stoppingToken.IsCancellationRequested) {
			await pipeClient.SendMessage("Hello from client");
			await Task.Delay(3000, stoppingToken);
		}
	}

	private void PipeClient_ReceivedMessage(object? sender, string message) {
		logger.LogInformation("Received pipe message: {message}", message);
	}
}
