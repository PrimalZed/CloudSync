using Microsoft.Extensions.Hosting;
using PrimalZed.CloudSync.Async;
using PrimalZed.CloudSync.Commands;
using PrimalZed.CloudSync.Remote.Local;
using Windows.Storage;

namespace PrimalZed.CloudSync;
public class SingleProcessWorker(
  SyncRootRegistrar syncRootRegistrar,
	SyncProviderPool syncProviderPool
) : BackgroundService {
  protected override async Task ExecuteAsync(CancellationToken stoppingToken) {
		var registerCommand = new RegisterSyncRootCommand {
			AccountId = @"Local!C:|SyncTestServer",
			Directory = @"C:\SyncTestClient",
			PopulationPolicy = PopulationPolicy.Full,
		};
		var storageFolder = await StorageFolder.GetFolderFromPathAsync(registerCommand.Directory);
		var localContext = new LocalContext {
			Directory = @"C:\SyncTestServer",
			EnableDeleteDirectoryWhenEmpty = true,
		};
		var syncRootInfo = syncRootRegistrar.Register(registerCommand, storageFolder, localContext);
		syncProviderPool.Start(syncRootInfo);

		await stoppingToken;

		await syncProviderPool.Stop(registerCommand.Directory);
		syncRootRegistrar.Unregister(registerCommand.AccountId);
	}
}
