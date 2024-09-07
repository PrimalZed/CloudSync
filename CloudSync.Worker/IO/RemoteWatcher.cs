using Microsoft.Extensions.Logging;
using PrimalZed.CloudSync.Remote.Abstractions;

namespace PrimalZed.CloudSync.IO; 
public sealed class RemoteWatcher(
	string rootDirectory,
	IRemoteReadService remoteReadService,
	IRemoteWatcher remoteWatcher,
	PlaceholdersService placeholderService,
	ILogger<RemoteWatcher> logger
) : IDisposable {
	public string RootDirectory { get; init; } = rootDirectory;
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
				await placeholderService.CreateOrUpdateDirectory(rootDirectory, relativePath);
			}
			else {
				await placeholderService.CreateOrUpdateFile(rootDirectory, relativePath);
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
				await placeholderService.UpdateDirectory(rootDirectory, relativePath);
			}
			else {
				await placeholderService.UpdateFile(rootDirectory, relativePath);
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
				await placeholderService.RenameDirectory(rootDirectory, oldRelativePath, newRelativePath);
			}
			else {
				await placeholderService.RenameFile(rootDirectory, oldRelativePath, newRelativePath);
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
			placeholderService.Delete(rootDirectory, relativePath);
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

public delegate RemoteWatcher CreateRemoteWatcher(string rootDirectory);

public class RemoteWatcherFactory(CreateRemoteWatcher create) {
	public RemoteWatcher CreateAndStart(string rootDirectory, CancellationToken stoppingToken = default) {
		var watcher = create(rootDirectory);
		watcher.Start(stoppingToken);

		return watcher;
	}
}
