using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;

namespace PrimalZed.CloudSync.App;
public class AppService(IServiceProvider serviceProvider, IOptions<AppOptions> options, ILogger<AppService> logger) : BackgroundService {
	protected override Task ExecuteAsync(CancellationToken stoppingToken) {
		Application.Start(_ => {
			try {
				var context = new DispatcherQueueSynchronizationContext(DispatcherQueue.GetForCurrentThread());
				SynchronizationContext.SetSynchronizationContext(context);
				var app = new App {
					ServiceProvider = serviceProvider,
					Options = options.Value,
				};

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
