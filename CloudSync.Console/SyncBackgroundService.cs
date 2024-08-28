using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using PrimalZed.CloudSync.Async;
using PrimalZed.CloudSync.Configuration;
using PrimalZed.CloudSync.Helpers;
using PrimalZed.CloudSync.IO;
using PrimalZed.CloudSync.Shell;

namespace PrimalZed.CloudSync;
public sealed class SyncBackgroundService(
	IOptions<ClientOptions> clientOptions,
	ShellRegistrar shellRegistrar,
	SyncRootRegistrar rootRegistrar,
	SyncProvider syncProvider,
	PlaceholdersService placeholdersService,
	ClientWatcherFactory clientWatcherFactory,
	RemoteWatcherFactory remoteWatcherFactory,
	IHostApplicationLifetime applicationLifetime,
	ILogger<SyncBackgroundService> logger
) : BackgroundService {
	private readonly ClientOptions _clientOptions = clientOptions.Value;

	protected override async Task ExecuteAsync(CancellationToken stoppingToken) {
		logger.LogInformation("Registering and Connecting...");
		// Stage 1: Setup
		//--------------------------------------------------------------------------------------------
		// The client folder (syncroot) must be indexed in order for states to properly display
		var clientDirectory = new DirectoryInfo(_clientOptions.Directory);
		clientDirectory.Attributes &= ~FileAttributes.NotContentIndexed;

		// Start up the task that registers and hosts the services for the shell (such as custom states, menus, etc)
		using var disposableShellCookies = new Disposable<IReadOnlyList<uint>>(shellRegistrar.Register(), shellRegistrar.Revoke);

		// Register the provider with the shell so that the Sync Root shows up in File Explorer
		await rootRegistrar.RegisterAsync();
		// TODO: Only on install
		using var disposableRoot = new Disposable(rootRegistrar.Unregister);


		// Hook up callback methods (in this class) for transferring files between client and server
		using var providerCancellation = new CancellationTokenSource();
		_ = Task.Run(() => syncProvider.ConnectAndRun(providerCancellation.Token), providerCancellation.Token);

		// Create the placeholders in the client folder so the user sees something
		// TODO: Only on install
		if (_clientOptions.PopulationPolicy == PopulationPolicy.AlwaysFull) {
			placeholdersService.CreateBulk(string.Empty);
		}

		// TODO: Sync changes since last time this service ran

		// Stage 2: Running
		//--------------------------------------------------------------------------------------------
		// The file watcher loop for this sample will run until the user presses Ctrl-C.
		// The file watcher will look for any changes on the files in the client (syncroot) in order
		// to let the cloud know.
		using var clientWatcher = clientWatcherFactory.CreateAndStart();
		using var serverWatcher = remoteWatcherFactory.CreateAndStart(stoppingToken);

		// Run until SIGTERM
		await stoppingToken;

		logger.LogInformation("Disconnecting and Unregistering...");
		providerCancellation.Cancel();

		// TODO: Only on uninstall (or not at all?)
		placeholdersService.DeleteBulk(_clientOptions.Directory);

		applicationLifetime.StopApplication();
	}
}
