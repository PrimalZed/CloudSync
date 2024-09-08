using Microsoft.Extensions.Logging;
using PrimalZed.CloudSync.Helpers;
using PrimalZed.CloudSync.Remote.Abstractions;

namespace PrimalZed.CloudSync.Remote.Local;
public class LocalRemoteReadService(
	ILocalContextAccessor contextAccessor,
	ILogger<LocalRemoteReadService> logger
) : IRemoteReadService {
	protected readonly LocalContext _context = contextAccessor.Context;

	public bool Exists(string relativePath) {
		var serverPath = Path.Join(_context.Directory, relativePath);
		return Path.Exists(serverPath);
	}

	public bool IsDirectory(string relativePath) {
		var serverPath = Path.Join(_context.Directory, relativePath);
		return File.GetAttributes(serverPath).HasFlag(FileAttributes.Directory);
	}

	public IEnumerable<RemoteDirectoryInfo> EnumerateDirectories(string relativeDirectory) =>
		EnumerateDirectories(relativeDirectory, "*");

	public IEnumerable<RemoteDirectoryInfo> EnumerateDirectories(string relativeDirectory, string pattern) =>
		Directory.EnumerateDirectories(Path.Join(_context.Directory, relativeDirectory), pattern)
			.Select((directory) => new DirectoryInfo(directory))
			.Select(GetRemoteDirectoryInfo)
			.ToArray();

	public IEnumerable<RemoteFileInfo> EnumerateFiles(string relativeDirectory) =>
		EnumerateFiles(relativeDirectory, "*");

	public IEnumerable<RemoteFileInfo> EnumerateFiles(string relativeDirectory, string pattern) =>
		Directory.EnumerateFiles(Path.Join(_context.Directory, relativeDirectory), pattern)
			.Select((file) => new FileInfo(file))
			.Select(GetRemoteFileInfo)
			.ToArray();

	public RemoteDirectoryInfo GetDirectoryInfo(string relativeDirectory) {
		var directoryInfo = new DirectoryInfo(Path.Join(_context.Directory, relativeDirectory));
		return GetRemoteDirectoryInfo(directoryInfo);
	}

	public RemoteFileInfo GetFileInfo(string relativeFile) {
		var fileInfo = new FileInfo(Path.Join(_context.Directory, relativeFile));
		return GetRemoteFileInfo(fileInfo);
	}

	public async Task<Stream> GetFileStream(string relativeFile) {
		var serverFile = Path.Join(_context.Directory, relativeFile);
		var fs = await FileHelper.WaitUntilUnlocked(() => File.OpenRead(serverFile), logger);
		return fs;
	}

	internal string GetRelativePath(string serverPath) =>
		PathMapper.GetRelativePath(serverPath, _context.Directory);

	private RemoteDirectoryInfo GetRemoteDirectoryInfo(DirectoryInfo directoryInfo) =>
		new() {
			Name = directoryInfo.Name,
			Attributes = directoryInfo.Attributes,
			RelativePath = GetRelativePath(directoryInfo.FullName),
			RelativeParentDirectory = GetRelativePath(directoryInfo.Parent!.FullName),
			CreationTimeUtc = directoryInfo.CreationTimeUtc,
			LastWriteTimeUtc = directoryInfo.LastWriteTimeUtc,
			LastAccessTimeUtc = directoryInfo.LastAccessTimeUtc,
		};

	private RemoteFileInfo GetRemoteFileInfo(FileInfo fileInfo) =>
		new() {
			Name = fileInfo.Name,
			Attributes = fileInfo.Attributes,
			Length = fileInfo.Length,
			RelativePath = GetRelativePath(fileInfo.FullName),
			RelativeParentDirectory = GetRelativePath(fileInfo.DirectoryName!),
			CreationTimeUtc = fileInfo.CreationTimeUtc,
			LastWriteTimeUtc = fileInfo.LastWriteTimeUtc,
			LastAccessTimeUtc = fileInfo.LastAccessTimeUtc,
		};
}
