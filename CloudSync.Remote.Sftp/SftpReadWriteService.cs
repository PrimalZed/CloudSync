using Microsoft.Extensions.Logging;
using PrimalZed.CloudSync.Helpers;
using PrimalZed.CloudSync.Remote.Abstractions;
using Renci.SshNet;
using Renci.SshNet.Sftp;

namespace PrimalZed.CloudSync.Remote.Sftp;
#pragma warning disable CS9107 // Parameter is captured into the state of the enclosing type and its value is also passed to the base constructor. The value might be captured by the base class as well.
public class SftpReadWriteService(
	ISftpContextAccessor contextAccessor,
	SftpClient client,
	ILogger<SftpReadWriteService> logger
) : SftpReadService(contextAccessor, client), IRemoteReadWriteService {
#pragma warning restore CS9107
	public async Task CreateFile(FileInfo sourceFileInfo, string relativeFile) {
		var serverFile = GetSftpPath(relativeFile);

		if (Exists(serverFile)) {
			throw new Exception("Conflict: already exists???");
		}

		logger.LogDebug("Create File {relativeFile}", relativeFile);
		PathMapper.EnsureSubDirectoriesExist(serverFile);

		using (var sourceStream = await FileHelper.WaitUntilUnlocked(sourceFileInfo.OpenRead, logger)) {
			await Task.Factory.FromAsync(client.BeginUploadFile(sourceStream, serverFile), client.EndUploadFile);
		}

		var sftpFile = client.Get(serverFile);
		sftpFile.LastWriteTimeUtc = sourceFileInfo.LastWriteTimeUtc;
		sftpFile.LastAccessTimeUtc = sourceFileInfo.LastAccessTimeUtc;
		sftpFile.UpdateStatus();
	}

	public async Task UpdateFile(FileInfo sourceFileInfo, string relativeFile) {
		var serverFile = GetSftpPath(relativeFile);
		// Update only - CreateFile to create!
		if (!Exists(serverFile)) {
			return;
		}

		var sftpFile = client.Get(serverFile);
		if (sourceFileInfo.LastWriteTimeUtc < sftpFile.LastWriteTimeUtc) {
			logger.LogDebug("Update File - Server more recent or equal {relativeFile}", relativeFile);
			return;
		}

		logger.LogDebug("Update File {relativeFile}", relativeFile);
		using (var sourceStream = sourceFileInfo.OpenRead()) {
			await Task.Factory.FromAsync(client.BeginUploadFile(sourceStream, serverFile), client.EndUploadFile);
		}

		sftpFile.LastWriteTimeUtc = sourceFileInfo.LastWriteTimeUtc;
		sftpFile.LastAccessTimeUtc = sourceFileInfo.LastAccessTimeUtc;
		sftpFile.UpdateStatus();
	}

	public void MoveFile(string oldRelativeFile, string newRelativeFile) {
		var oldServerFile = GetSftpPath(oldRelativeFile);
		var newServerFile = GetSftpPath(newRelativeFile);

		var sftpFile = client.Get(oldServerFile);
		sftpFile.MoveTo(newServerFile);
	}

	public void DeleteFile(string relativeFile) {
		var serverFile = GetSftpPath(relativeFile);
		logger.LogDebug("Delete File {file}", serverFile);
		client.DeleteFile(serverFile);
		//DeleteDirectoryIfEmpty(serverFile);
	}

	public Task CreateDirectory(DirectoryInfo sourceDirectoryInfo, string relativeDirectory) {
		var serverDirectory = GetSftpPath(relativeDirectory);

		if (Exists(serverDirectory)) {
			throw new Exception("Conflict: already exists");
		}

		client.CreateDirectory(serverDirectory);
		var sftpFile = client.Get(serverDirectory);
		sftpFile.LastWriteTimeUtc = sourceDirectoryInfo.LastWriteTimeUtc;
		sftpFile.LastAccessTimeUtc = sourceDirectoryInfo.LastAccessTimeUtc;
		sftpFile.UpdateStatus();

		return Task.CompletedTask;
	}

	public Task UpdateDirectory(DirectoryInfo sourceDirectoryInfo, string relativeDirectory) {
		throw new NotImplementedException();
	}

	public void MoveDirectory(string oldRelativeDirectory, string newRelativeDirectory) {
		var oldServerDirectory = GetSftpPath(oldRelativeDirectory);
		var newServerDirectory = GetSftpPath(newRelativeDirectory);
		logger.LogDebug("Move Directory {old} -> {new}", oldServerDirectory, newServerDirectory);
		var sftpFile = client.Get(oldServerDirectory);
		sftpFile.MoveTo(newServerDirectory);
	}

	public void DeleteDirectory(string relativeDirectory) {
		var serverDirectory = GetSftpPath(relativeDirectory);
		logger.LogDebug("Delete Directory {directory}", serverDirectory);

		foreach (ISftpFile sftpFile in client.ListDirectory(serverDirectory)) {
			if (_relativeDirectoryNames.Contains(sftpFile.Name)) {
				continue;
			}
			if (sftpFile.IsDirectory) {
				var relativePath = PathMapper.GetRelativePath(sftpFile.FullName, _context.Directory);
				DeleteDirectory(relativePath);
			}
			else {
				sftpFile.Delete();
			}
		}

		client.DeleteDirectory(serverDirectory);
	}
}
