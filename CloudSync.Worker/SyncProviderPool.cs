using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using PrimalZed.CloudSync.Abstractions;
using PrimalZed.CloudSync.Commands;

namespace PrimalZed.CloudSync;
public class SyncProviderPool(
	IServiceScopeFactory scopeFactory,
	ILogger<SyncProviderPool> logger
) {
	private readonly Dictionary<string, CancellableThread> _threads = [];
	private bool _stopping = false;

	public void Start(string syncRootId, string rootDirectory, PopulationPolicy populationPolicy) {
		if (_stopping) {
			return;
		}
		var context = new SyncProviderContext {
			Id = syncRootId,
			RootDirectory = rootDirectory,
			PopulationPolicy = populationPolicy,
		};
		var thread = new CancellableThread((CancellationToken cancellation) => Run(context, cancellation), logger);
		thread.Stopped += (object? sender, EventArgs e) => {
			_threads.Remove(rootDirectory);
			(sender as CancellableThread)?.Dispose();
		};
		thread.Start();
		_threads.Add(rootDirectory, thread);
	}

	public bool Has(string rootDirectory) => _threads.ContainsKey(rootDirectory);

	public async Task StopAll() {
		_stopping = true;

		var stopTasks = _threads.Values.Select((thread) => thread.Stop()).ToArray();
		await Task.WhenAll(stopTasks);
	}

	public async Task Stop(string rootDirectory) {
		if (!_threads.TryGetValue(rootDirectory, out var thread)) {
			return;
		}
		await thread.Stop();
	}

	private async Task Run(SyncProviderContext context, CancellationToken cancellation) {
		using var scope = scopeFactory.CreateScope();
		var contextAccessor = scope.ServiceProvider.GetRequiredService<SyncProviderContextAccessor>();
		contextAccessor.Context = context;

		var syncProvider = scope.ServiceProvider.GetRequiredService<SyncProvider>();
		await syncProvider.Run(cancellation);
	}

	private sealed class CancellableThread : IDisposable {
		private readonly CancellationTokenSource _cts = new();
		private readonly Task _task;
		public event EventHandler? Stopped;

		public CancellableThread(Func<CancellationToken, Task> action, ILogger logger) {
			_task = new Task(async () => {
				try {
					await action(_cts.Token);
				}
				catch (Exception ex) {
					logger.LogError(ex, "Thread stopped unexpectedly");
				}
				Stopped?.Invoke(this, EventArgs.Empty);
			});
		}

		public static CancellableThread CreateAndStart(Func<CancellationToken, Task> action, ILogger logger) {
			var cans = new CancellableThread(action, logger);
			cans.Start();
			return cans;
		}

		public void Start() {
			_task.Start();
		}

		public async Task Stop() {
			_cts.Cancel();
			await _task;

		}
		public void Dispose() {
			_cts.Cancel();
			_cts.Dispose();
		}
	}
}
