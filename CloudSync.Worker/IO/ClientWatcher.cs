using Microsoft.Extensions.Logging;
using PrimalZed.CloudSync.Interop;
using PrimalZed.CloudSync.Remote.Abstractions;
using PrimalZed.CloudSync.Helpers;
using PrimalZed.CloudSync.Abstractions;

namespace PrimalZed.CloudSync.IO;
public class ClientWatcher : IDisposable {
	private readonly ISyncProviderContextAccessor _contextAccessor;
	private readonly IRemoteReadWriteService _remoteService;
	private readonly ILogger _logger;
	private readonly FileSystemWatcher _watcher;

	private string _rootDirectory => _contextAccessor.Context.RootDirectory;

	public ClientWatcher(
		ISyncProviderContextAccessor contextAccessor,
		IRemoteReadWriteService serverService,
		ILogger<ClientWatcher> logger
	) {
		_contextAccessor = contextAccessor;
		_remoteService = serverService;
		_logger = logger;
		_watcher = CreateWatcher();
	}

	private FileSystemWatcher CreateWatcher() {
		var watcher = new FileSystemWatcher(_rootDirectory) {
			IncludeSubdirectories = true,
			NotifyFilter = NotifyFilters.FileName
				| NotifyFilters.DirectoryName
				| NotifyFilters.Attributes
				| NotifyFilters.LastWrite,
		};

		watcher.Changed += async (object sender, FileSystemEventArgs e) => {
			if (e.ChangeType != WatcherChangeTypes.Changed || !Path.Exists(e.FullPath)) {
				return;
			}
			var fileInfo = new FileInfo(e.FullPath);
			if (fileInfo.Attributes.HasFlag(FileAttributes.Directory)) {
				return;
			}
			_logger.LogDebug("{changeType} {path}", e.ChangeType, e.FullPath);
			try {
				if (fileInfo.Attributes.HasAllSyncFlags(SyncAttributes.PINNED | (int)FileAttributes.Offline)) {
					CloudFilter.HydratePlaceholder(e.FullPath);
				}
				else if (
					fileInfo.Attributes.HasAnySyncFlag(SyncAttributes.UNPINNED)
					&& !fileInfo.Attributes.HasFlag(FileAttributes.Offline)
				) {
					var relativePath = PathMapper.GetRelativePath(e.FullPath, _rootDirectory);
					CloudFilter.DehydratePlaceholder(e.FullPath, relativePath, fileInfo.Length);
				}
			}
			catch(Exception ex) {
				_logger.LogError(ex, "Hydrate/dehydrate failed");
			}

			if (fileInfo.Attributes.HasFlag(FileAttributes.Directory)) {
				// await _serverService.UpdateDirectory(directory, e.FullPath);
			}
			else {
				await _remoteService.UpdateFile(e.FullPath);
			}
		};
		watcher.Created += async (object sender, FileSystemEventArgs e) => {
			_logger.LogDebug("Created {path}", e.FullPath);
			if (File.GetAttributes(e.FullPath).HasFlag(FileAttributes.Directory)) {
				await _remoteService.CreateDirectory(e.FullPath);
			}
			else {
				await _remoteService.CreateFile(e.FullPath);
			}
		};
		watcher.Error += (object sender, ErrorEventArgs e) => {
			var ex = e.GetException();
			_logger.LogError(ex, "Client file watcher error");
		};

		return watcher;
	}

	public void Start() {
		_watcher.EnableRaisingEvents = true;
	}

	public void Dispose() {
		_watcher.Dispose();
	}
}
