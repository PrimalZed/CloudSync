using Microsoft.Extensions.Logging;
using PrimalZed.CloudSync.Abstractions;
using PrimalZed.CloudSync.Async;
using PrimalZed.CloudSync.Helpers;
using PrimalZed.CloudSync.IO;
using static Vanara.PInvoke.CldApi;

namespace PrimalZed.CloudSync; 
public class SyncProvider(
	ISyncProviderContextAccessor contextAccessor,
	SyncRootConnector syncProvider,
	PlaceholdersService placeholdersService,
	ClientWatcher clientWatcher,
	RemoteWatcher remoteWatcher,
	ILogger<SyncProvider> logger
) {
	public async Task Run(CancellationToken cancellation) {
		logger.LogDebug("Connecting...");
		// Hook up callback methods (in this class) for transferring files between client and server
		using var providerCancellation = new CancellationTokenSource();
		using var providerDisposable = new Disposable(providerCancellation.Cancel);
		using var connectDisposable = new Disposable<CF_CONNECTION_KEY>(syncProvider.Connect(providerCancellation.Token), syncProvider.Disconnect);
		_ = Task.Run(() => syncProvider.ProcessQueueAsync(providerCancellation.Token));

		// Create the placeholders in the client folder so the user sees something
		if (contextAccessor.Context.PopulationPolicy == Commands.PopulationPolicy.AlwaysFull) {
			placeholdersService.CreateBulk(string.Empty);
		}

		// TODO: Sync changes since last time this service ran

		// Stage 2: Running
		//--------------------------------------------------------------------------------------------
		// The file watcher loop for this sample will run until the user presses Ctrl-C.
		// The file watcher will look for any changes on the files in the client (syncroot) in order
		// to let the cloud know.
		clientWatcher.Start();
		remoteWatcher.Start(cancellation);

		// Run until SIGTERM
		await cancellation;

		logger.LogDebug("Disconnecting...");
		providerCancellation.Cancel();

		// TODO: Only on uninstall (or not at all?)
		//placeholdersService.DeleteBulk(directory);
	}
}
