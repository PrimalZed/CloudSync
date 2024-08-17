using Microsoft.Extensions.Logging;
using PrimalZed.CloudSync.Remote.Abstractions;
using PrimalZed.CloudSync.Remote.Local;

namespace PrimalZed.CloudSync.IO; 
public sealed class RemoteWatcher(
	IRemoteReadService remoteReadService,
	IRemoteWatcher remoteWatcher,
	PlaceholdersService placeholderService,
	ILogger<LocalRemoteWatcher> logger
) : IDisposable {
	public void Start(CancellationToken stoppingToken) {
		remoteWatcher.Created += HandleCreated;
		remoteWatcher.Changed += HandleChanged;
		remoteWatcher.Renamed += HandleRenamed;
		remoteWatcher.Deleted += HandleDeleted;
		remoteWatcher.Start(stoppingToken);
	}

	private async Task HandleCreated(string relativePath) {
		logger.LogDebug("Created {path}", relativePath);
		try {
			if (remoteReadService.IsDirectory(relativePath)) {
				await placeholderService.CreateOrUpdateDirectory(relativePath);
			}
			else {
				await placeholderService.CreateOrUpdateFile(relativePath);
			}
		}
		catch (Exception ex) {
			logger.LogError(ex, "Handle Created failed");
		}
	}

	private async Task HandleChanged(string relativePath) {
		logger.LogDebug("Changed {path}", relativePath);
		try {
			if (remoteReadService.IsDirectory(relativePath)) {
				await placeholderService.UpdateDirectory(relativePath);
			}
			else {
				await placeholderService.UpdateFile(relativePath);
			}
		}
		catch (Exception ex) {
			logger.LogError(ex, "Handle Changed failed");
		}
	}

	private async Task HandleRenamed(string oldRelativePath, string newRelativePath) {
		// Brief pause to let client rename finish before reflecting it back
		// await Task.Delay(1000);
		logger.LogDebug("Changed {oldPath} -> {path}", oldRelativePath, newRelativePath);
		try {
			if (remoteReadService.IsDirectory(newRelativePath)) {
				await placeholderService.RenameDirectory(oldRelativePath, newRelativePath);
			}
			else {
				await placeholderService.RenameFile(oldRelativePath, newRelativePath);
			}
		}
		catch (Exception ex) {
			logger.LogError(ex, "Rename placeholder failed");
		}
	}

	private async Task HandleDeleted(string relativePath) {
		// Brief pause to let client rename finish before reflecting it back
		// await Task.Delay(1000);
		logger.LogDebug("Deleted {path}", relativePath);
		try {
			placeholderService.Delete(relativePath);
		}
		catch (Exception ex) {
			logger.LogError(ex, "Delete placeholder failed");
		}
	}

	public void Dispose() {
		remoteWatcher.Created -= HandleCreated;
		remoteWatcher.Changed -= HandleChanged;
		remoteWatcher.Renamed -= HandleRenamed;
		remoteWatcher.Deleted -= HandleDeleted;
	}
}

public delegate RemoteWatcher CreateRemoteWatcher();

public class RemoteWatcherFactory(CreateRemoteWatcher create) {
	public RemoteWatcher CreateAndStart(CancellationToken stoppingToken = default) {
		var watcher = create();
		watcher.Start(stoppingToken);

		return watcher;
	}
}
