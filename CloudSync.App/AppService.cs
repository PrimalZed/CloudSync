using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;

namespace CloudSync.App;
public class AppService(Func<App> appFactory, ILogger<AppService> logger) : BackgroundService {
	protected override Task ExecuteAsync(CancellationToken stoppingToken) {
		Application.Start(_ => {
			try {
				var context = new DispatcherQueueSynchronizationContext(DispatcherQueue.GetForCurrentThread());
				SynchronizationContext.SetSynchronizationContext(context);
				var app = appFactory();

				app.UnhandledException += (object sender, Microsoft.UI.Xaml.UnhandledExceptionEventArgs e) => {
					logger.LogCritical(e.Exception, "Unhandled exception");
					e.Handled = true;
				};
			}
			catch (Exception ex) {
				logger.LogDebug(ex, "Error application start callback");
			}
		});

		return Task.CompletedTask;
	}
}
