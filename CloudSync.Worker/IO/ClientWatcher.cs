using Microsoft.Extensions.Logging;
using PrimalZed.CloudSync.Interop;
using PrimalZed.CloudSync.Remote.Abstractions;
using PrimalZed.CloudSync.Helpers;
using PrimalZed.CloudSync.Abstractions;
using System.Threading.Channels;

namespace PrimalZed.CloudSync.IO;
public class ClientWatcher : IDisposable {
	private readonly ISyncProviderContextAccessor _contextAccessor;
	private readonly ChannelWriter<Func<Task>> _taskWriter;
	private readonly FileLocker _fileLocker;
	private readonly IRemoteReadWriteService _remoteService;
	private readonly ILogger _logger;
	private readonly FileSystemWatcher _watcher;

	private string _rootDirectory => _contextAccessor.Context.RootDirectory;

	public ClientWatcher(
		ISyncProviderContextAccessor contextAccessor,
		ChannelWriter<Func<Task>> taskWriter,
		FileLocker fileLocker,
		IRemoteReadWriteService remoteService,
		ILogger<ClientWatcher> logger
	) {
		_contextAccessor = contextAccessor;
		_taskWriter = taskWriter;
		_fileLocker = fileLocker;
		_remoteService = remoteService;
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
			await _taskWriter.WriteAsync(async () => {
				var relativePath = PathMapper.GetRelativePath(e.FullPath, _rootDirectory);
				try {
					if (fileInfo.Attributes.HasAllSyncFlags(SyncAttributes.PINNED | (int)FileAttributes.Offline)) {
						CloudFilter.HydratePlaceholder(e.FullPath);
					}
					else if (
						fileInfo.Attributes.HasAnySyncFlag(SyncAttributes.UNPINNED)
						&& !fileInfo.Attributes.HasFlag(FileAttributes.Offline)
					) {
						CloudFilter.DehydratePlaceholder(e.FullPath, relativePath, fileInfo.Length);
					}
				}
				catch (Exception ex) {
					_logger.LogError(ex, "Hydrate/dehydrate failed");
				}

				using var locker = await _fileLocker.Lock(relativePath);
				if (fileInfo.Attributes.HasFlag(FileAttributes.Directory)) {
					//var directoryInfo = new DirectoryInfo(e.FullPath);
					//await _serverService.UpdateDirectory(directoryInfo, relativePath);
				}
				else {
					await _remoteService.UpdateFile(fileInfo, relativePath);
				}
			});
		};

		watcher.Created += async (object sender, FileSystemEventArgs e) => {
			_logger.LogDebug("Created {path}", e.FullPath);
			await _taskWriter.WriteAsync(async () => {
				var relativePath = PathMapper.GetRelativePath(e.FullPath, _rootDirectory);

				using var locker = await _fileLocker.Lock(relativePath);
				if (File.GetAttributes(e.FullPath).HasFlag(FileAttributes.Directory)) {
					var directoryInfo = new DirectoryInfo(e.FullPath);
					await _remoteService.CreateDirectory(directoryInfo, relativePath);
				}
				else {
					var fileInfo = new FileInfo(e.FullPath);
					await _remoteService.CreateFile(fileInfo, relativePath);
				}
			});
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
