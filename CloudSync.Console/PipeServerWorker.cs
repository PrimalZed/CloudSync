using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using PrimalZed.CloudSync.Pipes;

namespace PrimalZed.CloudSync;
public sealed class PipeServerWorker : BackgroundService {
	private readonly PipeServer _pipeServer;
	private readonly ILogger _logger;

	public PipeServerWorker(ILogger<PipeServerWorker> logger) {
		_pipeServer = new(PipeNames.TEST, 3, logger);
		_logger = logger;
		_pipeServer.ReceivedMessage += PipeServer_ReceivedMessage;
	}

	protected override async Task ExecuteAsync(CancellationToken stoppingToken) {
		while (!stoppingToken.IsCancellationRequested) {
			await _pipeServer.SendMessage("Hello from server", stoppingToken);
			await Task.Delay(3000, stoppingToken);
		}
	}

	private void PipeServer_ReceivedMessage(object? sender, string message) {
		_logger.LogInformation("Received pipe message: {message}", message);
	}

	public override void Dispose() {
		_pipeServer.ReceivedMessage -= PipeServer_ReceivedMessage;
		_pipeServer.Dispose();
		base.Dispose();
	}
}
