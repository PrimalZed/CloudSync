using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using PrimalZed.CloudSync.Async;
using PrimalZed.CloudSync.Commands;
using PrimalZed.CloudSync.Pipes;
using Windows.Storage;
using static PrimalZed.CloudSync.Pipes.PipeServer;

namespace PrimalZed.CloudSync;
public sealed class PipeWorker : BackgroundService {
	private readonly PipeServer _pipeServer;
	private readonly SyncRootRegistrar _syncRootRegistrar;
	private readonly SyncProviderPool _syncProviderPool;
	private readonly ILogger _logger;

	public PipeWorker(
		[FromKeyedServices("registrar")] PipeServer pipeServer,
		SyncRootRegistrar syncRootRegistrar,
		SyncProviderPool syncProviderPool,
		ILogger<PipeWorker> logger
	) {
		_pipeServer = pipeServer;
		_syncRootRegistrar = syncRootRegistrar;
		_syncProviderPool = syncProviderPool;
		_logger = logger;
		_pipeServer.ReceivedMessage += PipeServer_ReceivedMessage;
	}

	protected override async Task ExecuteAsync(CancellationToken stoppingToken) {
		await stoppingToken;
	}

	private async void PipeServer_ReceivedMessage(object? sender, string message) {
		if (sender is null || sender is not PipeServerInstance pipeInstance) {
			throw new NotSupportedException("Unexpected type");
		}

		var command = SyncRootCommand.FromMessage(message);
		switch (command) {
			case RegisterSyncRootCommand registerCommand:
				_logger.LogDebug("Received register request {accountId} {directory}", registerCommand.AccountId, registerCommand.Directory);

				var storageFolder = await StorageFolder.GetFolderFromPathAsync(registerCommand.Directory);

				pipeInstance.RunAsClient(() => _syncRootRegistrar.Register(registerCommand, storageFolder));

				// Sends to all pipe server instances, not just the one that sent this message
				await _pipeServer.SendMessage("Registered");
				_syncProviderPool.Start(registerCommand.Directory, (Windows.Storage.Provider.StorageProviderPopulationPolicy)registerCommand.PopulationPolicy);
				break;
			case UnregisterSyncRootCommand unregisterCommand:
				_logger.LogDebug("Received unregister request {accountId} {directory}", unregisterCommand.AccountId, unregisterCommand.Directory);

				await _syncProviderPool.Stop(unregisterCommand.Directory);
				pipeInstance.RunAsClient(() => _syncRootRegistrar.Unregister(unregisterCommand.AccountId));

				// Sends to all pipe server instances, not just the one that sent this message
				await _pipeServer.SendMessage("Unregistered");
				break;
		}
	}

	public override void Dispose() {
		_pipeServer.ReceivedMessage -= PipeServer_ReceivedMessage;
		base.Dispose();
	}
}
