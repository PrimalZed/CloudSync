using System.Threading.Channels;
using Microsoft.Extensions.Logging;
using PrimalZed.CloudSync.Abstractions;
using PrimalZed.CloudSync.Remote.Abstractions;

namespace PrimalZed.CloudSync.Remote.Local;
public sealed class LocalRemoteWatcher : IRemoteWatcher {
	private readonly ISyncProviderContextAccessor _contextAccessor;
	private readonly LocalRemoteReadService _readService;
	private readonly ILogger _logger;

	private readonly Channel<FileSystemEventArgs> _channel =
		Channel.CreateUnbounded<FileSystemEventArgs>(
			new UnboundedChannelOptions {
				SingleReader = true,
			}
		);
	private readonly FileSystemWatcher _watcher;

	public LocalRemoteWatcher(
		ISyncProviderContextAccessor contextAccessor,
		IRemoteReadService readService,
		ILogger<LocalRemoteWatcher> logger
	) {
		_contextAccessor = contextAccessor;
		if (readService is not LocalRemoteReadService localReadService) {
			throw new ArgumentException($"Must be {nameof(LocalRemoteReadService)}", nameof(readService));
		}
		_readService = localReadService;
		_logger = logger;
		
		_watcher = CreateWatcher();
	}

	private FileSystemWatcher CreateWatcher() {
		var remoteDirectory = _contextAccessor.Context.GetRemoteDirectory();
		var watcher = new FileSystemWatcher(remoteDirectory) {
			IncludeSubdirectories = true,
			NotifyFilter = NotifyFilters.FileName
				| NotifyFilters.DirectoryName
				| NotifyFilters.Attributes
				| NotifyFilters.LastWrite,
		};

		watcher.Changed += async (object sender, FileSystemEventArgs e) => await _channel.Writer.WriteAsync(e);
		watcher.Created += async (object sender, FileSystemEventArgs e) => await _channel.Writer.WriteAsync(e);
		watcher.Renamed += async (object sender, RenamedEventArgs e) => await _channel.Writer.WriteAsync(e);
		watcher.Deleted += async (object sender, FileSystemEventArgs e) => await _channel.Writer.WriteAsync(e);
		watcher.Error += (object sender, ErrorEventArgs e) => {
			var ex = e.GetException();
			_logger.LogError(ex, "Local server file watcher error");
		};

		return watcher;
	}

	public void Start(CancellationToken stoppingToken = default) {
		_watcher.EnableRaisingEvents = true;
		Task.Run(() => ProcessQueueAsync(stoppingToken), stoppingToken);
	}

	public event RemoteCreateHandler? Created;
	public event RemoteChangeHandler? Changed;
	public event RemoteRenameHandler? Renamed;
	public event RemoteDeleteHandler? Deleted;

	private async Task ProcessQueueAsync(CancellationToken stoppingToken = default) {
		while (!stoppingToken.IsCancellationRequested) {
			var e = await _channel.Reader.ReadAsync(stoppingToken);
			var task = e.ChangeType switch {
				WatcherChangeTypes.Created => Created?.Invoke(_readService.GetRelativePath(e.FullPath)) ?? Task.CompletedTask,
				WatcherChangeTypes.Changed => (e.ChangeType != WatcherChangeTypes.Changed || !_readService.Exists(_readService.GetRelativePath(e.FullPath)))
					? Task.CompletedTask
					: Changed?.Invoke(_readService.GetRelativePath(e.FullPath)) ?? Task.CompletedTask,
				WatcherChangeTypes.Renamed =>
					e is RenamedEventArgs renamedArgs
						? Renamed?.Invoke(_readService.GetRelativePath(renamedArgs.OldFullPath), _readService.GetRelativePath(renamedArgs.FullPath)) ?? Task.CompletedTask
						: throw new Exception("Unexpected type"),
				WatcherChangeTypes.Deleted => Deleted?.Invoke(_readService.GetRelativePath(e.FullPath)) ?? Task.CompletedTask,
				_ => throw new NotImplementedException(),
			};
			await task;
		}
	}

	public void Dispose() {
		_watcher.Dispose();
	}
}
