using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using PrimalZed.CloudSync.Async;
using PrimalZed.CloudSync.Helpers;
using PrimalZed.CloudSync.Shell;

namespace PrimalZed.CloudSync.App;
public sealed class ShellWorker(
	ShellRegistrar shellRegistrar,
	ILogger<ShellWorker> logger
) : BackgroundService {
	protected override async Task ExecuteAsync(CancellationToken stoppingToken) {
		logger.LogInformation("Registering Shell class objects");

		// Start up the task that registers and hosts the services for the shell (such as custom states, menus, etc)
		using var disposableShellCookies = new Disposable<IReadOnlyList<uint>>(shellRegistrar.Register(), shellRegistrar.Revoke);

		await stoppingToken;
	}
}
