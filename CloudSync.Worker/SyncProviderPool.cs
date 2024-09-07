using Microsoft.Extensions.Logging;
using PrimalZed.CloudSync.Async;
using PrimalZed.CloudSync.Helpers;
using PrimalZed.CloudSync.IO;
using Windows.Storage.Provider;
using static Vanara.PInvoke.CldApi;

namespace PrimalZed.CloudSync;
public class SyncProviderPool(
	SyncProvider syncProvider,
	PlaceholdersService placeholdersService,
	ClientWatcherFactory clientWatcherFactory,
	RemoteWatcherFactory remoteWatcherFactory,
	ILogger<SyncProviderPool> logger
) {
	private readonly Dictionary<string, CancellableThread> _threads = [];
	private bool _stopping = false;

	public void Start(string directory, StorageProviderPopulationPolicy populationPolicy) {
		if (_stopping) {
			return;
		}
		var thread = new CancellableThread((CancellationToken cancellation) => Run(directory, populationPolicy, cancellation), logger);
		thread.Stopped += (object? sender, EventArgs e) => {
			_threads.Remove(directory);
			(sender as CancellableThread)?.Dispose();
		};
		thread.Start();
		_threads.Add(directory, thread);
	}

	public bool Has(string directory) => _threads.ContainsKey(directory);

	public async Task StopAll() {
		_stopping = true;

		var stopTasks = _threads.Values.Select((thread) => thread.Stop()).ToArray();
		await Task.WhenAll(stopTasks);
	}

	public async Task Stop(string syncRootPath) {
		if (!_threads.TryGetValue(syncRootPath, out var thread)) {
			return;
		}
		await thread.Stop();
	}

	private async Task Run(string directory, StorageProviderPopulationPolicy populationPolicy, CancellationToken cancellation) {
		logger.LogDebug("Connecting...");
		// Hook up callback methods (in this class) for transferring files between client and server
		using var providerCancellation = new CancellationTokenSource();
		using var providerDisposable = new Disposable(providerCancellation.Cancel);
		using var connectDisposable = new Disposable<CF_CONNECTION_KEY>(syncProvider.Connect(directory, providerCancellation.Token), syncProvider.Disconnect);
		_ = Task.Run(() => syncProvider.ProcessQueueAsync(providerCancellation.Token));

		// Create the placeholders in the client folder so the user sees something
		if (populationPolicy == StorageProviderPopulationPolicy.AlwaysFull) {
			placeholdersService.CreateBulk(string.Empty);
		}

		// TODO: Sync changes since last time this service ran

		// Stage 2: Running
		//--------------------------------------------------------------------------------------------
		// The file watcher loop for this sample will run until the user presses Ctrl-C.
		// The file watcher will look for any changes on the files in the client (syncroot) in order
		// to let the cloud know.
		using var clientWatcher = clientWatcherFactory.CreateAndStart();
		using var serverWatcher = remoteWatcherFactory.CreateAndStart(cancellation);

		// Run until SIGTERM
		await cancellation;

		logger.LogDebug("Disconnecting...");
		providerCancellation.Cancel();

		// TODO: Only on uninstall (or not at all?)
		//placeholdersService.DeleteBulk(directory);
	}

	private sealed class CancellableThread : IDisposable {
		private readonly CancellationTokenSource _cts = new();
		private readonly Task _task;
		public event EventHandler? Stopped;

		public CancellableThread(Func<CancellationToken, Task> action, ILogger logger) {
			_task = new Task(async () => {
				try {
					await action(_cts.Token);
				}
				catch (Exception ex) {
					logger.LogError(ex, "Thread stopped unexpectedly");
				}
				Stopped?.Invoke(this, EventArgs.Empty);
			});
		}

		public static CancellableThread CreateAndStart(Func<CancellationToken, Task> action, ILogger logger) {
			var cans = new CancellableThread(action, logger);
			cans.Start();
			return cans;
		}

		public void Start() {
			_task.Start();
		}

		public async Task Stop() {
			_cts.Cancel();
			await _task;

		}
		public void Dispose() {
			_cts.Cancel();
			_cts.Dispose();
		}
	}
}
