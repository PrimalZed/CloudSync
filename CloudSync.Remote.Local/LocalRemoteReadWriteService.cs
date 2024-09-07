using PrimalZed.CloudSync.Helpers;
using Microsoft.Extensions.Logging;
using PrimalZed.CloudSync.Remote.Abstractions;
using PrimalZed.CloudSync.Abstractions;

namespace PrimalZed.CloudSync.Remote.Local;
public class LocalRemoteReadWriteService(
	ISyncProviderContextAccessor contextAccessor,
	ILogger<LocalRemoteReadWriteService> logger
) : LocalRemoteReadService(contextAccessor, logger), IRemoteReadWriteService {
	private readonly FileEqualityComparer _fileComparer = new ();
	private readonly DirectoryEqualityComparer _directoryComparer = new ();

	public async Task CreateFile(string clientFile) {
		var serverFile = PathMapper.ReplaceStart(clientFile, _context.RootDirectory, _localContext.RemoteDirectory);

		if (Path.Exists(serverFile)) {
			throw new Exception("Conflict: already exists???");
		}

		logger.LogDebug("Create File {clientFile}", clientFile);
		PathMapper.EnsureSubDirectoriesExist(serverFile);

		var clientFileInfo = new FileInfo(clientFile);
		await FileHelper.WaitUntilUnlocked(
			() => CopyFile(clientFileInfo, serverFile),
			logger
		);
	}

	public async Task UpdateFile(string clientFile) {
		var serverFile = PathMapper.ReplaceStart(clientFile, _context.RootDirectory, _localContext.RemoteDirectory);
		// Update only - CreateFile to create!
		if (!Path.Exists(serverFile)) {
			return;
		}

		if (_fileComparer.Compare(new FileInfo(clientFile), new FileInfo(serverFile)) <= 0) {
			logger.LogDebug("Update File - Server more recent or equal {clientFile}", clientFile);
			return;
		}

		logger.LogDebug("Update File {clientFile}", clientFile);
		var clientFileInfo = new FileInfo(clientFile);
		await FileHelper.WaitUntilUnlocked(
			() => CopyFile(clientFileInfo, serverFile),
			logger
		);
	}

	private void CopyFile(FileInfo clientFileInfo, string serverFile) {
		clientFileInfo.CopyTo(serverFile, overwrite: true);
		var serverFileInfo = new FileInfo(serverFile) {
			CreationTimeUtc = clientFileInfo.CreationTimeUtc,
			LastWriteTimeUtc = clientFileInfo.LastWriteTimeUtc,
			LastAccessTimeUtc = clientFileInfo.LastAccessTimeUtc,
		};
		serverFileInfo.Attributes = (FileAttributes)SyncAttributes.Take(
			(int)clientFileInfo.Attributes,
			(int)serverFileInfo.Attributes
		);
	}

	public Task CreateDirectory(string relativeFile) {
		var serverDirectory = Path.Join(_localContext.RemoteDirectory, relativeFile);
		logger.LogDebug("Create Directory {directory}", serverDirectory);

		if (Path.Exists(serverDirectory)) {
			throw new Exception("Conflict: already exists");
		}

		var clientDirectoryInfo = new DirectoryInfo(relativeFile);
		var serverDirectoryInfo = Directory.CreateDirectory(serverDirectory);
		serverDirectoryInfo.CreationTimeUtc = clientDirectoryInfo.CreationTimeUtc;
		serverDirectoryInfo.LastWriteTimeUtc = clientDirectoryInfo.LastWriteTimeUtc;
		serverDirectoryInfo.LastAccessTimeUtc = clientDirectoryInfo.LastAccessTimeUtc;
		serverDirectoryInfo.Attributes = (FileAttributes)SyncAttributes.Take((int)clientDirectoryInfo.Attributes, (int)File.GetAttributes(serverDirectory));
		return Task.CompletedTask;
	}

	public Task UpdateDirectory(string relativeDirectory) {
		var serverDirectory = Path.Join(_localContext.RemoteDirectory, relativeDirectory);
		logger.LogDebug("Update Directory {directory}", serverDirectory);

		if (_directoryComparer.Equals(new DirectoryInfo(relativeDirectory), new DirectoryInfo(serverDirectory))) {
			return Task.CompletedTask;
		}

		var clientDirectoryInfo = new DirectoryInfo(relativeDirectory);
		_ = new DirectoryInfo(serverDirectory) {
			CreationTimeUtc = clientDirectoryInfo.CreationTimeUtc,
			LastWriteTimeUtc = clientDirectoryInfo.LastWriteTimeUtc,
			LastAccessTimeUtc = clientDirectoryInfo.LastAccessTimeUtc,
			Attributes = (FileAttributes)SyncAttributes.Take((int)clientDirectoryInfo.Attributes, (int)File.GetAttributes(serverDirectory))
		};
		return Task.CompletedTask;
	}

	public void MoveFile(string oldRelativeFile, string newRelativeFile) {
		var oldServerFile = Path.Join(_localContext.RemoteDirectory, oldRelativeFile);
		var newServerFile = Path.Join(_localContext.RemoteDirectory, newRelativeFile);
		logger.LogDebug("Move File {old} -> {new}", oldServerFile, newServerFile);
		File.Move(oldServerFile, newServerFile);
	}

	public void MoveDirectory(string oldRelativeDirectory, string newRelativeDirectory) {
		var oldServerDirectory = Path.Join(_localContext.RemoteDirectory, oldRelativeDirectory);
		var newServerDirectory = Path.Join(_localContext.RemoteDirectory, newRelativeDirectory);
		logger.LogDebug("Move Directory {old} -> {new}", oldServerDirectory, newServerDirectory);
		Directory.Move(oldServerDirectory, newServerDirectory);
	}

	public void DeleteFile(string relativeFile) {
		var serverFile = Path.Join(_localContext.RemoteDirectory, relativeFile);
		logger.LogDebug("Delete File {file}", serverFile);
		File.Delete(serverFile);
		DeleteDirectoryIfEmpty(Path.GetDirectoryName(serverFile)!);
	}

	public void DeleteDirectory(string relativeDirectory) {
		var serverDirectory = Path.Join(_localContext.RemoteDirectory, relativeDirectory);
		logger.LogDebug("Delete Directory {directory}", serverDirectory);
		Directory.Delete(serverDirectory, recursive: true);
		DeleteDirectoryIfEmpty(Path.GetDirectoryName(serverDirectory)!);
	}

	private void DeleteDirectoryIfEmpty(string serverPath) {
		if (!_localContext.EnableDeleteDirectoryWhenEmpty) {
			return;
		}
		if (
			serverPath == _localContext.RemoteDirectory
			|| Directory.EnumerateFileSystemEntries(serverPath).Any()
		) {
			return;
		}
		DeleteDirectory(serverPath);
	}
}
