using Microsoft.Extensions.Hosting;
using PrimalZed.CloudSync.Helpers;
using PrimalZed.CloudSync.Management.Abstractions;

namespace PrimalZed.CloudSync;
public class SingleProcessWorker(
  ISyncRootRegistrar rootRegistrar,
  ShellWorker innerWorker
) : BackgroundService {
  protected override async Task ExecuteAsync(CancellationToken stoppingToken) {
    await rootRegistrar.RegisterAsync();
    try {
      await innerWorker.StartAsync(stoppingToken);
      await innerWorker.ExecuteTask!;
    }
    finally {
      await rootRegistrar.Unregister();
    }
  }
}
