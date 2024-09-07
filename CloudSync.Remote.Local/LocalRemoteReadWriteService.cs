using PrimalZed.CloudSync.Helpers;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using PrimalZed.CloudSync.Remote.Abstractions;

namespace PrimalZed.CloudSync.Remote.Local;
public class LocalRemoteReadWriteService(
	IOptions<LocalRemoteOptions> options,
	ILogger<LocalRemoteReadWriteService> logger
) : LocalRemoteReadService(options, logger), IRemoteReadWriteService {
	private readonly FileEqualityComparer _fileComparer = new ();
	private readonly DirectoryEqualityComparer _directoryComparer = new ();

	public async Task CreateFile(string rootDirectory, string clientFile) {
		var serverFile = PathMapper.ReplaceStart(clientFile, rootDirectory, _options.Directory);

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

	public async Task UpdateFile(string rootDirectory, string clientFile) {
		var serverFile = PathMapper.ReplaceStart(clientFile, rootDirectory, _options.Directory);
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

	public Task CreateDirectory(string rootDirectory, string clientDirectory) {
		var serverDirectory = PathMapper.ReplaceStart(clientDirectory, rootDirectory, _options.Directory);
		logger.LogDebug("Create Directory {directory}", serverDirectory);

		if (Path.Exists(serverDirectory)) {
			throw new Exception("Conflict: already exists");
		}

		var clientDirectoryInfo = new DirectoryInfo(clientDirectory);
		var serverDirectoryInfo = Directory.CreateDirectory(serverDirectory);
		serverDirectoryInfo.CreationTimeUtc = clientDirectoryInfo.CreationTimeUtc;
		serverDirectoryInfo.LastWriteTimeUtc = clientDirectoryInfo.LastWriteTimeUtc;
		serverDirectoryInfo.LastAccessTimeUtc = clientDirectoryInfo.LastAccessTimeUtc;
		serverDirectoryInfo.Attributes = (FileAttributes)SyncAttributes.Take((int)clientDirectoryInfo.Attributes, (int)File.GetAttributes(serverDirectory));
		return Task.CompletedTask;
	}

	public Task UpdateDirectory(string rootDirectory, string clientDirectory) {
		var serverDirectory = PathMapper.ReplaceStart(clientDirectory, rootDirectory, _options.Directory);
		logger.LogDebug("Update Directory {directory}", serverDirectory);

		if (_directoryComparer.Equals(new DirectoryInfo(clientDirectory), new DirectoryInfo(serverDirectory))) {
			return Task.CompletedTask;
		}

		var clientDirectoryInfo = new DirectoryInfo(clientDirectory);
		_ = new DirectoryInfo(serverDirectory) {
			CreationTimeUtc = clientDirectoryInfo.CreationTimeUtc,
			LastWriteTimeUtc = clientDirectoryInfo.LastWriteTimeUtc,
			LastAccessTimeUtc = clientDirectoryInfo.LastAccessTimeUtc,
			Attributes = (FileAttributes)SyncAttributes.Take((int)clientDirectoryInfo.Attributes, (int)File.GetAttributes(serverDirectory))
		};
		return Task.CompletedTask;
	}

	public void MoveFile(string oldRelativeFile, string newRelativeFile) {
		var oldServerFile = Path.Join(_options.Directory, oldRelativeFile);
		var newServerFile = Path.Join(_options.Directory, newRelativeFile);
		logger.LogDebug("Move File {old} -> {new}", oldServerFile, newServerFile);
		File.Move(oldServerFile, newServerFile);
	}

	public void MoveDirectory(string oldClientDirectory, string newClientDirectory) {
		var oldServerDirectory = Path.Join(_options.Directory, oldClientDirectory);
		var newServerDirectory = Path.Join(_options.Directory, newClientDirectory);
		logger.LogDebug("Move Directory {old} -> {new}", oldServerDirectory, newServerDirectory);
		Directory.Move(oldServerDirectory, newServerDirectory);
	}

	public void DeleteFile(string clientFile) {
		var serverFile = Path.Join(_options.Directory, clientFile);
		logger.LogDebug("Delete File {file}", serverFile);
		File.Delete(serverFile);
		DeleteDirectoryIfEmpty(Path.GetDirectoryName(serverFile)!);
	}

	public void DeleteDirectory(string clientDirectory) {
		var serverDirectory = Path.Join(_options.Directory, clientDirectory);
		logger.LogDebug("Delete Directory {directory}", serverDirectory);
		Directory.Delete(serverDirectory, recursive: true);
		DeleteDirectoryIfEmpty(Path.GetDirectoryName(serverDirectory)!);
	}

	private void DeleteDirectoryIfEmpty(string serverPath) {
		if (!_options.EnableDeleteDirectoryWhenEmpty) {
			return;
		}
		if (
			serverPath == _options.Directory
			|| Directory.EnumerateFileSystemEntries(serverPath).Any()
		) {
			return;
		}
		DeleteDirectory(serverPath);
	}
}
