using Microsoft.Extensions.Hosting;
using PrimalZed.CloudSync.Async;

namespace PrimalZed.CloudSync;
public class SyncProviderWorker(SyncProviderPool pool) : BackgroundService {
	protected override async Task ExecuteAsync(CancellationToken stoppingToken) {
		await stoppingToken;
		await pool.StopAll();
	}
}