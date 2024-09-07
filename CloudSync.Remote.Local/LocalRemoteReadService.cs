﻿using Microsoft.Extensions.Logging;
using PrimalZed.CloudSync.Remote.Abstractions;
using PrimalZed.CloudSync.Helpers;
using PrimalZed.CloudSync.Abstractions;

namespace PrimalZed.CloudSync.Remote.Local;
public class LocalRemoteReadService(
	ISyncProviderContextAccessor contextAccessor,
	ILogger<LocalRemoteReadService> logger
) : IRemoteReadService {
	protected readonly SyncProviderContext _context = contextAccessor.Context;
	protected LocalRemoteInfo _localContext => _context.RemoteInfo is LocalRemoteInfo localContext
		? localContext
		: throw new NotSupportedException("Context remote info is not LocalRemoteInfo");

	public bool Exists(string relativePath) {
		var serverPath = Path.Join(_localContext.RemoteDirectory, relativePath);
		return Path.Exists(serverPath);
	}

	public bool IsDirectory(string relativePath) {
		var serverPath = Path.Join(_localContext.RemoteDirectory, relativePath);
		return File.GetAttributes(serverPath).HasFlag(FileAttributes.Directory);
	}

	public IEnumerable<RemoteDirectoryInfo> EnumerateDirectories(string relativeDirectory) =>
		EnumerateDirectories(relativeDirectory, "*");

	public IEnumerable<RemoteDirectoryInfo> EnumerateDirectories(string relativeDirectory, string pattern) =>
		Directory.EnumerateDirectories(Path.Join(_localContext.RemoteDirectory, relativeDirectory), pattern)
			.Select((directory) => new DirectoryInfo(directory))
			.Select(GetRemoteDirectoryInfo)
			.ToArray();

	public IEnumerable<RemoteFileInfo> EnumerateFiles(string relativeDirectory) =>
		EnumerateFiles(relativeDirectory, "*");

	public IEnumerable<RemoteFileInfo> EnumerateFiles(string relativeDirectory, string pattern) =>
		Directory.EnumerateFiles(Path.Join(_localContext.RemoteDirectory, relativeDirectory), pattern)
			.Select((file) => new FileInfo(file))
			.Select(GetRemoteFileInfo)
			.ToArray();

	public RemoteDirectoryInfo GetDirectoryInfo(string relativeDirectory) {
		var directoryInfo = new DirectoryInfo(Path.Join(_localContext.RemoteDirectory, relativeDirectory));
		return GetRemoteDirectoryInfo(directoryInfo);
	}

	public RemoteFileInfo GetFileInfo(string relativeFile) {
		var fileInfo = new FileInfo(Path.Join(_localContext.RemoteDirectory, relativeFile));
		return GetRemoteFileInfo(fileInfo);
	}

	public async Task<Stream> GetFileStream(string relativeFile) {
		var serverFile = Path.Join(_localContext.RemoteDirectory, relativeFile);
		var fs = await FileHelper.WaitUntilUnlocked(() => File.OpenRead(serverFile), logger);
		return fs;
	}

	internal string GetRelativePath(string serverPath) =>
		PathMapper.GetRelativePath(serverPath, _localContext.RemoteDirectory);

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
