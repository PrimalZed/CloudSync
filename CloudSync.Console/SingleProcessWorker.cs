using Microsoft.Extensions.Hosting;
using PrimalZed.CloudSync.Async;
using PrimalZed.CloudSync.Commands;
using PrimalZed.CloudSync.Remote.Local;
using PrimalZed.CloudSync.Remote.Sftp;
using Windows.Storage;

namespace PrimalZed.CloudSync;
public class SingleProcessWorker(
  SyncRootRegistrar syncRootRegistrar,
	SyncProviderPool syncProviderPool
) : BackgroundService {
  protected override async Task ExecuteAsync(CancellationToken stoppingToken) {
		var registerCommand = new RegisterSyncRootCommand {
			AccountId = @"Sftp!Mountainduck",
			Directory = @"C:\SyncTestClient",
			PopulationPolicy = PopulationPolicy.Full,
		};
		var storageFolder = await StorageFolder.GetFolderFromPathAsync(registerCommand.Directory);
		//var localContext = new LocalContext {
		//	Directory = @"C:\SyncTestServer",
		//};
		var sftpContext = new SftpContext {
			Directory = "/home",
			Host = "sftp.foo.com",
			Port = 2000,
			Username = "guest",
			Password = "pwd",
		};
		var syncRootInfo = syncRootRegistrar.Register(registerCommand, storageFolder, sftpContext);
		syncProviderPool.Start(syncRootInfo);

		await stoppingToken;

		await syncProviderPool.Stop(syncRootInfo.Id);
		syncRootRegistrar.Unregister(syncRootInfo.Id);
	}
}
