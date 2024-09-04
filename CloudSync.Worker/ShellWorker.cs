using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using PrimalZed.CloudSync.Async;
using PrimalZed.CloudSync.Configuration;
using PrimalZed.CloudSync.Helpers;
using PrimalZed.CloudSync.Shell;

namespace PrimalZed.CloudSync;
public sealed class ShellWorker(
	IOptions<ClientOptions> clientOptions,
	ShellRegistrar shellRegistrar,
	ILogger<ShellWorker> logger
) : BackgroundService {
	private readonly ClientOptions _clientOptions = clientOptions.Value;

	protected override async Task ExecuteAsync(CancellationToken stoppingToken) {
		logger.LogInformation("Registering Shell class objects");

		// Start up the task that registers and hosts the services for the shell (such as custom states, menus, etc)
		using var disposableShellCookies = new Disposable<IReadOnlyList<uint>>(shellRegistrar.Register(), shellRegistrar.Revoke);

		await stoppingToken;
	}
}
