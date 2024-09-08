using Microsoft.Extensions.Logging;
using PrimalZed.CloudSync.Helpers;
using PrimalZed.CloudSync.Remote.Abstractions;

namespace PrimalZed.CloudSync.Remote.Local;
public class LocalRemoteReadWriteService(
	ILocalContextAccessor contextAccessor,
	ILogger<LocalRemoteReadWriteService> logger
) : LocalRemoteReadService(contextAccessor, logger), IRemoteReadWriteService {
	private readonly FileEqualityComparer _fileComparer = new ();
	private readonly DirectoryEqualityComparer _directoryComparer = new ();

	public async Task CreateFile(FileInfo sourceFileInfo, string relativeFile) {
		var serverFile = Path.Join(_context.Directory, relativeFile);

		if (Path.Exists(serverFile)) {
			throw new Exception("Conflict: already exists???");
		}

		logger.LogDebug("Create File {relativeFile}", relativeFile);
		PathMapper.EnsureSubDirectoriesExist(serverFile);

		await FileHelper.WaitUntilUnlocked(
			() => CopyFile(sourceFileInfo, serverFile),
			logger
		);
	}

	public async Task UpdateFile(FileInfo sourceFileInfo, string relativeFile) {
		var serverFile = Path.Join(_context.Directory, relativeFile);
		// Update only - CreateFile to create!
		if (!Path.Exists(serverFile)) {
			return;
		}

		if (_fileComparer.Compare(sourceFileInfo, new FileInfo(serverFile)) <= 0) {
			logger.LogDebug("Update File - Server more recent or equal {relativeFile}", relativeFile);
			return;
		}

		logger.LogDebug("Update File {relativeFile}", relativeFile);
		await FileHelper.WaitUntilUnlocked(
			() => CopyFile(sourceFileInfo, serverFile),
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

	public Task CreateDirectory(DirectoryInfo sourceDirectoryInfo, string relativeDirectory) {
		var serverDirectory = Path.Join(_context.Directory, relativeDirectory);

		if (Path.Exists(serverDirectory)) {
			throw new Exception("Conflict: already exists");
		}

		logger.LogDebug("Create Directory {relativeDirectory}", relativeDirectory);
		var serverDirectoryInfo = Directory.CreateDirectory(serverDirectory);
		serverDirectoryInfo.CreationTimeUtc = sourceDirectoryInfo.CreationTimeUtc;
		serverDirectoryInfo.LastWriteTimeUtc = sourceDirectoryInfo.LastWriteTimeUtc;
		serverDirectoryInfo.LastAccessTimeUtc = sourceDirectoryInfo.LastAccessTimeUtc;
		serverDirectoryInfo.Attributes = (FileAttributes)SyncAttributes.Take((int)sourceDirectoryInfo.Attributes, (int)File.GetAttributes(serverDirectory));
		return Task.CompletedTask;
	}

	public Task UpdateDirectory(DirectoryInfo sourceDirectoryInfo, string relativeDirectory) {
		var serverDirectory = Path.Join(_context.Directory, relativeDirectory);

		if (_directoryComparer.Equals(new DirectoryInfo(relativeDirectory), new DirectoryInfo(serverDirectory))) {
			return Task.CompletedTask;
		}

		logger.LogDebug("Update Directory {relativeDirectory}", relativeDirectory);
		_ = new DirectoryInfo(serverDirectory) {
			CreationTimeUtc = sourceDirectoryInfo.CreationTimeUtc,
			LastWriteTimeUtc = sourceDirectoryInfo.LastWriteTimeUtc,
			LastAccessTimeUtc = sourceDirectoryInfo.LastAccessTimeUtc,
			Attributes = (FileAttributes)SyncAttributes.Take((int)sourceDirectoryInfo.Attributes, (int)File.GetAttributes(serverDirectory))
		};
		return Task.CompletedTask;
	}

	public void MoveFile(string oldRelativeFile, string newRelativeFile) {
		var oldServerFile = Path.Join(_context.Directory, oldRelativeFile);
		var newServerFile = Path.Join(_context.Directory, newRelativeFile);
		logger.LogDebug("Move File {old} -> {new}", oldServerFile, newServerFile);
		File.Move(oldServerFile, newServerFile);
	}

	public void MoveDirectory(string oldRelativeDirectory, string newRelativeDirectory) {
		var oldServerDirectory = Path.Join(_context.Directory, oldRelativeDirectory);
		var newServerDirectory = Path.Join(_context.Directory, newRelativeDirectory);
		logger.LogDebug("Move Directory {old} -> {new}", oldServerDirectory, newServerDirectory);
		Directory.Move(oldServerDirectory, newServerDirectory);
	}

	public void DeleteFile(string relativeFile) {
		var serverFile = Path.Join(_context.Directory, relativeFile);
		logger.LogDebug("Delete File {file}", serverFile);
		File.Delete(serverFile);
		DeleteDirectoryIfEmpty(Path.GetDirectoryName(serverFile)!);
	}

	public void DeleteDirectory(string relativeDirectory) {
		var serverDirectory = Path.Join(_context.Directory, relativeDirectory);
		logger.LogDebug("Delete Directory {directory}", serverDirectory);
		Directory.Delete(serverDirectory, recursive: true);
		DeleteDirectoryIfEmpty(Path.GetDirectoryName(serverDirectory)!);
	}

	private void DeleteDirectoryIfEmpty(string serverPath) {
		if (!_context.EnableDeleteDirectoryWhenEmpty) {
			return;
		}
		if (
			serverPath == _context.Directory
			|| Directory.EnumerateFileSystemEntries(serverPath).Any()
		) {
			return;
		}
		DeleteDirectory(serverPath);
	}
}
