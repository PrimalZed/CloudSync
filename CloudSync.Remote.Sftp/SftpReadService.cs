using PrimalZed.CloudSync.Helpers;
using PrimalZed.CloudSync.Remote.Abstractions;
using Renci.SshNet;
using Renci.SshNet.Sftp;

namespace PrimalZed.CloudSync.Remote.Sftp;
public class SftpReadService(
	ISftpContextAccessor contextAccessor,
	SftpClient client
) : IRemoteReadService {
	protected readonly string[] _relativeDirectoryNames = [".", "..", "#Recycle"];
	protected readonly SftpContext _context = contextAccessor.Context;

	public bool Exists(string relativePath) =>
		client.Exists(GetSftpPath(relativePath));

	public bool IsDirectory(string relativePath) =>
		client.Get(GetSftpPath(relativePath)).IsDirectory;

	public IEnumerable<RemoteDirectoryInfo> EnumerateDirectories(string relativeDirectory) =>
		EnumerateDirectories(relativeDirectory, "*");

	public IEnumerable<RemoteDirectoryInfo> EnumerateDirectories(string relativeDirectory, string pattern) {
		if (pattern != "*") {
			throw new NotSupportedException($"Does not support pattern other than \"*\": {pattern}; try https://learn.microsoft.com/en-us/dotnet/core/extensions/file-globbing");
		}
		return client.ListDirectory(GetSftpPath(relativeDirectory))
			.Where((sftpFile) => sftpFile.IsDirectory)
			.Where((sftpFile) => !_relativeDirectoryNames.Contains(sftpFile.Name))
			.Select(GetRemoteDirectoryInfo)
			.ToArray();
	}

	public IEnumerable<RemoteFileInfo> EnumerateFiles(string relativeDirectory) =>
		EnumerateFiles(relativeDirectory, "*");

	public IEnumerable<RemoteFileInfo> EnumerateFiles(string relativeDirectory, string pattern) {
		if (pattern != "*") {
			throw new NotSupportedException($"Does not support pattern other than \"*\": {pattern}; try https://learn.microsoft.com/en-us/dotnet/core/extensions/file-globbing");
		}
		return client.ListDirectory(GetSftpPath(relativeDirectory))
			.Where((sftpFile) => !sftpFile.IsDirectory && sftpFile.IsRegularFile)
			.Select(GetRemoteFileInfo)
			.ToArray();
	}

	public RemoteDirectoryInfo GetDirectoryInfo(string relativeDirectory) {
		var sftpFile = client.Get(GetSftpPath(relativeDirectory));
		return GetRemoteDirectoryInfo(sftpFile);
	}

	public RemoteFileInfo GetFileInfo(string relativeFile) {
		var sftpFile = client.Get(GetSftpPath(relativeFile));
		return GetRemoteFileInfo(sftpFile);
	}

	public async Task<Stream> GetFileStream(string relativeFile) {
		var serverFile = GetSftpPath(relativeFile);
		var stream = new MemoryStream();
		try {
			await Task.Factory.FromAsync(client.BeginDownloadFile(GetSftpPath(relativeFile), stream), client.EndDownloadFile);
		}
		catch {
			stream.Dispose();
			throw;
		}
		return stream;
	}

	protected string GetSftpPath(string relativePath) =>
		Path.Join(_context.Directory, relativePath).Replace(@"\", "/");

	private RemoteDirectoryInfo GetRemoteDirectoryInfo(ISftpFile sftpFile) =>
		new() {
			Name = sftpFile.Name,
			Attributes = FileAttributes.Directory,
			RelativePath = PathMapper.GetRelativePath(sftpFile.FullName, _context.Directory),
			RelativeParentDirectory = PathMapper.GetRelativePath(sftpFile.FullName, _context.Directory).Replace($"/{sftpFile.Name}", ""),
			CreationTimeUtc = sftpFile.LastWriteTimeUtc,
			LastWriteTimeUtc = sftpFile.LastWriteTimeUtc,
			LastAccessTimeUtc = sftpFile.LastAccessTimeUtc,
		};

	private RemoteFileInfo GetRemoteFileInfo(ISftpFile sftpFile) =>
		new() {
			Name = sftpFile.Name,
			Length = sftpFile.Length,
			Attributes = FileAttributes.None,
			RelativePath = PathMapper.GetRelativePath(sftpFile.FullName, _context.Directory),
			RelativeParentDirectory = PathMapper.GetRelativePath(sftpFile.FullName, _context.Directory).Replace($"/{sftpFile.Name}", ""),
			CreationTimeUtc = sftpFile.LastWriteTimeUtc,
			LastWriteTimeUtc = sftpFile.LastWriteTimeUtc,
			LastAccessTimeUtc = sftpFile.LastAccessTimeUtc,
		};
}
