using System.Threading.Channels;
using Microsoft.Extensions.Logging;
using PrimalZed.CloudSync.Helpers;
using PrimalZed.CloudSync.Remote.Abstractions;

namespace PrimalZed.CloudSync.Remote.Local;
public sealed class LocalRemoteWatcher : IRemoteWatcher {
	private readonly LocalContext _context;
	private readonly ILogger _logger;

	private readonly Channel<FileSystemEventArgs> _channel =
		Channel.CreateUnbounded<FileSystemEventArgs>(
			new UnboundedChannelOptions {
				SingleReader = true,
			}
		);
	private readonly FileSystemWatcher _watcher;

	public LocalRemoteWatcher(
		ILocalContextAccessor localContextAccessor,
		ILogger<LocalRemoteWatcher> logger
	) {
		_context = localContextAccessor.Context;
		_logger = logger;
		
		_watcher = CreateWatcher();
	}

	private FileSystemWatcher CreateWatcher() {
		var watcher = new FileSystemWatcher(_context.Directory) {
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
				WatcherChangeTypes.Created => Created?.Invoke(PathMapper.GetRelativePath(e.FullPath, _context.Directory)) ?? Task.CompletedTask,
				WatcherChangeTypes.Changed => (e.ChangeType != WatcherChangeTypes.Changed || !Path.Exists(e.FullPath))
					? Task.CompletedTask
					: Changed?.Invoke(PathMapper.GetRelativePath(e.FullPath, _context.Directory)) ?? Task.CompletedTask,
				WatcherChangeTypes.Renamed =>
					e is RenamedEventArgs renamedArgs
						? Renamed?.Invoke(PathMapper.GetRelativePath(renamedArgs.OldFullPath, _context.Directory), PathMapper.GetRelativePath(renamedArgs.FullPath, _context.Directory)) ?? Task.CompletedTask
						: throw new Exception("Unexpected type"),
				WatcherChangeTypes.Deleted => Deleted?.Invoke(PathMapper.GetRelativePath(e.FullPath, _context.Directory)) ?? Task.CompletedTask,
				_ => throw new NotImplementedException(),
			};
			await task;
		}
	}

	public void Dispose() {
		_watcher.Dispose();
	}
}
