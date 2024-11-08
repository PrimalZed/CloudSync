﻿using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using PrimalZed.CloudSync.Async;
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
		var syncRootsInfos = StorageProviderSyncRootManager.GetCurrentSyncRoots()
			.Where(x => x.Id.StartsWith($"{providerOptions.Value.ProviderId}!"))
			.ToArray();

		foreach (var syncRootInfo in syncRootsInfos) {
			if (pool.Has(syncRootInfo.Id)) {
				continue;
			}
			
			pool.Start(syncRootInfo);
		}
	}
}
