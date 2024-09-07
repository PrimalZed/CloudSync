using Microsoft.Extensions.Logging;
using PrimalZed.CloudSync.Abstractions;
using PrimalZed.CloudSync.Interop;
using PrimalZed.CloudSync.Remote.Abstractions;
using PrimalZed.CloudSync.Helpers;
using Vanara.Extensions;
using Vanara.PInvoke;

namespace PrimalZed.CloudSync;
public class PlaceholdersService(
	ISyncProviderContextAccessor contextAccessor,
	IRemoteReadWriteService remoteService,
	ILogger<PlaceholdersService> logger
) {
	private readonly ILogger _logger = logger;
	private string rootDirectory => contextAccessor.Context.RootDirectory;
	private readonly FileEqualityComparer _fileComparer = new ();
	private readonly DirectoryEqualityComparer _directoryComparer = new ();

	public void CreateBulk(string subpath) {
		using var filePlaceholderCreateInfos = remoteService.EnumerateFiles(subpath)
			.Select(GetFilePlaceholderCreateInfo)
			.ToDisposableArray();

		var remoteSubDirectories = remoteService.EnumerateDirectories(subpath);
		using var directoryPlaceholderCreateInfos = remoteSubDirectories
			.Select(GetDirectoryPlaceholderCreateInfo)
			.ToDisposableArray();

		var placeholderCreateInfos = filePlaceholderCreateInfos.Source
			.Concat(directoryPlaceholderCreateInfos.Source)
			.Select((x) => (CldApi.CF_PLACEHOLDER_CREATE_INFO)x)
			.ToArray();

		if (placeholderCreateInfos.Length == 0) {
			return;
		}

		CldApi.CfCreatePlaceholders(
			Path.Join(rootDirectory, subpath),
			placeholderCreateInfos,
			Convert.ToUInt32(placeholderCreateInfos.Length),
			CldApi.CF_CREATE_FLAGS.CF_CREATE_FLAG_NONE,
			out var entriesProcessed
		).ThrowIfFailed("Create placeholders failed");

		foreach (var remoteSubDirectory in remoteSubDirectories) {
			CreateBulk(remoteSubDirectory.RelativePath);
		}
	}

	public Task CreateOrUpdateFile(string relativeFile) {
		var clientFile = Path.Join(rootDirectory, relativeFile);
		return !File.Exists(clientFile)
			? CreateFile(relativeFile)
			: UpdateFile(relativeFile);
	}

	public Task CreateOrUpdateDirectory(string relativeDirectory) {
		var clientDirectory = Path.Join(rootDirectory, relativeDirectory);
		return !Directory.Exists(clientDirectory)
			? CreateDirectory(relativeDirectory)
			: UpdateDirectory(relativeDirectory);
	}

	public async Task CreateFile(string relativeFile) {
		var fileInfo = remoteService.GetFileInfo(relativeFile);
		using var createInfo = new SafeCreateInfo(fileInfo, fileInfo.RelativePath);
		CldApi.CfCreatePlaceholders(
			Path.Join(rootDirectory, fileInfo.RelativeParentDirectory),
			[createInfo],
			1u,
			CldApi.CF_CREATE_FLAGS.CF_CREATE_FLAG_NONE,
			out var entriesProcessed
		).ThrowIfFailed("Create placeholder failed");
	}

	public async Task CreateDirectory(string relativeDirectory) {
		var directoryInfo = remoteService.GetDirectoryInfo(relativeDirectory);
		using var createInfo = new SafeCreateInfo(directoryInfo, directoryInfo.RelativePath);
		CldApi.CfCreatePlaceholders(
			Path.Join(rootDirectory, directoryInfo.RelativeParentDirectory),
			[createInfo],
			1u,
			CldApi.CF_CREATE_FLAGS.CF_CREATE_FLAG_NONE,
			out var entriesProcessed
		).ThrowIfFailed("Create placeholder failed");
	}

	private SafeCreateInfo GetFilePlaceholderCreateInfo(RemoteFileInfo remoteFileInfo) =>
		new(remoteFileInfo, remoteFileInfo.RelativePath);

	private SafeCreateInfo GetDirectoryPlaceholderCreateInfo(RemoteDirectoryInfo remoteDirectoryInfo) =>
		new(remoteDirectoryInfo, remoteDirectoryInfo.RelativePath, onDemand: false);

	public async Task UpdateFile(string relativeFile, bool force = false) {
		var clientFile = Path.Join(rootDirectory, relativeFile);
		if (!Path.Exists(clientFile)) {
			_logger.LogDebug("Skip update; file does not exist {clientFile}", clientFile);
			return;
		}
		var clientFileInfo = new FileInfo(clientFile);
		var downloaded = !clientFileInfo.Attributes.HasFlag(FileAttributes.Offline);

		using var hfile = downloaded
			? await FileHelper.WaitUntilUnlocked(() => CloudFilter.CreateHFileWithOplock(clientFile, FileAccess.Write), _logger)
			: CloudFilter.CreateHFile(clientFile, FileAccess.Write);
		var placeholderState = CloudFilter.GetPlaceholderState(hfile);
		if (!placeholderState.HasFlag(CldApi.CF_PLACEHOLDER_STATE.CF_PLACEHOLDER_STATE_PLACEHOLDER)) {
			CloudFilter.ConvertToPlaceholder(hfile);
		}

		var remoteFileInfo = remoteService.GetFileInfo(relativeFile);
		if (!force && remoteFileInfo.GetHashCode() == clientFileInfo.GetHashCode()) {
			_logger.LogDebug("UpdateFile - equal, ignoring {relativeFile}", relativeFile);
			if (!placeholderState.HasFlag(CldApi.CF_PLACEHOLDER_STATE.CF_PLACEHOLDER_STATE_IN_SYNC)) {
				CloudFilter.SetInSyncState(hfile);
			}
			return;
		}

			_logger.LogDebug("UpdateFile - update placeholder {relativeFile}", relativeFile);
		var pinned = clientFileInfo.Attributes.HasAnySyncFlag(SyncAttributes.PINNED);
		if (pinned) {
			// Clear Pinned to avoid 392 ERROR_CLOUD_FILE_PINNED
			CloudFilter.SetPinnedState(hfile, 0);
		}
		var redownload = downloaded && !clientFileInfo.Attributes.HasAnySyncFlag(SyncAttributes.UNPINNED);
		var relativePath = PathMapper.GetRelativePath(clientFile, rootDirectory);
		var usn = downloaded
			? CloudFilter.UpdateAndDehydratePlaceholder(hfile, relativePath, remoteFileInfo)
			: CloudFilter.UpdateFilePlaceholder(hfile, relativePath, remoteFileInfo);
		if (pinned) {
			// ClientWatcher calls HydratePlaceholder when both Offline and Pinned are set
			CloudFilter.SetPinnedState(hfile, SyncAttributes.PINNED);
		}
		else if (redownload) {
			CloudFilter.HydratePlaceholder(hfile);
		}
	}

	public Task UpdateDirectory(string relativeDirectory) {
		var clientDirectory = Path.Join(rootDirectory, relativeDirectory);
		if (!Path.Exists(clientDirectory)) {
			_logger.LogDebug("Skip update; directory does not exist {clientDirectory}", clientDirectory);
			return Task.CompletedTask;
		}
		var remoteDirectoryInfo = remoteService.GetDirectoryInfo(relativeDirectory);
		var clientDirectoryInfo = new DirectoryInfo(clientDirectory);

		if (!CloudFilter.IsPlaceholder(clientDirectory)) {
			_logger.LogDebug("Convert Directory to Placeholder");
			CloudFilter.ConvertToPlaceholder(clientDirectory);
		}

		if (remoteDirectoryInfo.GetHashCode() == clientDirectoryInfo.GetHashCode()) {
			_logger.LogDebug("Set Directory In-Sync");
			CloudFilter.SetInSyncState(clientDirectory);
			return Task.CompletedTask;
		}

		_logger.LogDebug("Update Directory Placeholder");
		using var hfile = CloudFilter.CreateHFileWithOplock(clientDirectory, FileAccess.Write);
		var relativePath = PathMapper.GetRelativePath(clientDirectory, rootDirectory);
		CloudFilter.UpdateDirectoryPlaceholder(hfile, remoteDirectoryInfo, relativePath);
		return Task.CompletedTask;
	}

	public async Task RenameFile(string oldRelativeFile, string newRelativeFile) {
		var oldClientFile = Path.Join(rootDirectory, oldRelativeFile);
		if (!Path.Exists(oldClientFile) ) {
			await CreateOrUpdateFile(newRelativeFile);
			return;
		}
		var newClientFile = Path.Join(rootDirectory, newRelativeFile);
		File.Move(oldClientFile, newClientFile);

		CloudFilter.SetInSyncState(newClientFile);
	}

	public async Task RenameDirectory(string oldRelativePath, string newRelativePath) {
		var oldClientDirectory = Path.Join(rootDirectory, oldRelativePath);
		if (!Path.Exists(oldClientDirectory)) {
			await CreateOrUpdateDirectory(newRelativePath);
			return;
		}
		var newClientDirectory = Path.Join(rootDirectory, newRelativePath);
		Directory.Move(oldClientDirectory, newClientDirectory);

		CloudFilter.SetInSyncState(newClientDirectory);
	}

	public void DeleteBulk(string relativeDirectory) {
		var clientDirectory = Path.Join(rootDirectory, relativeDirectory);
		var clientSubDirectories = Directory.EnumerateDirectories(clientDirectory);
		foreach (var clientSubDirectory in clientSubDirectories) {
			Directory.Delete(clientSubDirectory, recursive: true);
		}

		var clientFiles = Directory.EnumerateFiles(clientDirectory);
		foreach (var clientFile in clientFiles) {
			File.Delete(clientFile);
		}
	}

	public void Delete(string relativePath) {
		var clientPath = Path.Join(rootDirectory, relativePath);
		if (!Path.Exists(clientPath)) {
			return;
		}
		if (File.GetAttributes(clientPath).HasFlag(FileAttributes.Directory)) {
			Directory.Delete(clientPath, recursive: true);
		}
		else {
			File.Delete(clientPath);
		}
	}
}
