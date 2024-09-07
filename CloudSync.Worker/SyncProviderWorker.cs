using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using PrimalZed.CloudSync.Async;
using PrimalZed.CloudSync.Commands;
using PrimalZed.CloudSync.Configuration;
using Windows.Storage.Provider;

namespace PrimalZed.CloudSync;
public class SyncProviderWorker(
	IOptions<ProviderOptions> providerOptions,
	SyncProviderPool pool
) : BackgroundService {
	protected override async Task ExecuteAsync(CancellationToken stoppingToken) {
		ConnectAll();

		await stoppingToken;

		await pool.StopAll();
	}

	private void ConnectAll() {
		var syncRoots = StorageProviderSyncRootManager.GetCurrentSyncRoots()
			.Where(x => x.Id.StartsWith($"{providerOptions.Value.ProviderId}!"))
			.ToArray();

		foreach (var syncRoot in syncRoots) {
			if (pool.Has(syncRoot.Path.Path)) {
				continue;
			}
			
			pool.Start(syncRoot.Id, syncRoot.Path.Path, (PopulationPolicy)syncRoot.PopulationPolicy);
		}
	}
}
