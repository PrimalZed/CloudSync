using Microsoft.Extensions.Hosting;
using PrimalZed.CloudSync.Async;
using PrimalZed.CloudSync.Commands;
using Windows.Storage;
using Windows.Storage.Provider;

namespace PrimalZed.CloudSync;
public class SingleProcessWorker(
  SyncRootRegistrar syncRootRegistrar,
	SyncProviderPool syncProviderPool
) : BackgroundService {
  protected override async Task ExecuteAsync(CancellationToken stoppingToken) {
		var registerCommand = new RegisterSyncRootCommand {
			AccountId = "TestAccount1",
			Directory = @"C:\SyncTestClient",
			PopulationPolicy = PopulationPolicy.Full,
		};
		var storageFolder = await StorageFolder.GetFolderFromPathAsync(registerCommand.Directory);
		//syncRootRegistrar.Register(registerCommand, storageFolder);
		//syncProviderPool.Start(registerCommand.Directory, (StorageProviderPopulationPolicy)registerCommand.PopulationPolicy);

		await stoppingToken;

		//await syncProviderPool.Stop(registerCommand.Directory);
		//syncRootRegistrar.Unregister(registerCommand.AccountId);
	}
}
