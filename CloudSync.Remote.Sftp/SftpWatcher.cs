using PrimalZed.CloudSync.Abstractions;
using PrimalZed.CloudSync.Helpers;
using PrimalZed.CloudSync.Remote.Abstractions;
using Renci.SshNet;

namespace PrimalZed.CloudSync.Remote.Sftp;
public sealed class SftpWatcher(
	ISyncProviderContextAccessor syncContextAccessor,
	ISftpContextAccessor contextAccessor,
	SftpClient client
) : IRemoteWatcher {
	private readonly SyncProviderContext _syncContext = syncContextAccessor.Context;
	private readonly SftpContext _context = contextAccessor.Context;
	private readonly string[] _relativeDirectoryNames = [".", ".."];
	private Dictionary<string, DateTime> _knownFiles = [];
	private bool _running = false;
	private CancellationTokenSource _cancellationTokenSource = new();

	public event RemoteCreateHandler? Created;
	public event RemoteChangeHandler? Changed;
	public event RemoteRenameHandler? Renamed;
	public event RemoteDeleteHandler? Deleted;

	public async void Start(CancellationToken stoppingToken = default) {
		ObjectDisposedException.ThrowIf(_cancellationTokenSource.IsCancellationRequested, this);
		if (_running) {
			throw new Exception("Already running");
		}
		_running = true;

		using var linkedTokenSource = CancellationTokenSource.CreateLinkedTokenSource(stoppingToken);
		while (!linkedTokenSource.Token.IsCancellationRequested) {
			var foundFiles = IsPopulated(_context.Directory)
				? FindFiles(_context.Directory)
				: [];

			var removedFiles = _knownFiles.Keys.Except(foundFiles.Keys).ToArray();
			foreach (var removedFile in removedFiles) {
				Deleted?.Invoke(PathMapper.GetRelativePath(removedFile, _context.Directory));
			}
			var addedFiles = foundFiles.Keys.Except(_knownFiles.Keys).ToArray();
			foreach (var addedFile in addedFiles) {
				Created?.Invoke(PathMapper.GetRelativePath(addedFile, _context.Directory));
			}

			var updatedFiles = foundFiles
				.Where((pair) => _knownFiles.ContainsKey(pair.Key) && _knownFiles[pair.Key] < pair.Value)
				.Select(pair => pair.Key)
				.ToArray();
			foreach (var updatedFile in updatedFiles) {
				Changed?.Invoke(PathMapper.GetRelativePath(updatedFile, _context.Directory));
			}

			_knownFiles = foundFiles;

			try {
				await Task.Delay(_context.WatchPeriodSeconds * 1000, linkedTokenSource.Token);
			}
			catch (TaskCanceledException) { }
		}
	}

	private Dictionary<string, DateTime> FindFiles(string directory) {
		var sftpFiles = client.ListDirectory(directory);

		var subFiles = sftpFiles
			.Where((sftpFile) => sftpFile.IsDirectory && !_relativeDirectoryNames.Contains(sftpFile.Name))
			.Where((sftpFile) => IsPopulated(sftpFile.FullName))
			.SelectMany((sftpFile) => FindFiles(sftpFile.FullName))
			.ToArray();

		var files = sftpFiles
			.Where((sftpFile) => !sftpFile.IsDirectory && sftpFile.IsRegularFile)
			.ToDictionary((sftpFile) => sftpFile.FullName, (sftpFile) => sftpFile.LastWriteTimeUtc);

		return subFiles.Concat(files).ToDictionary();
	}

	private bool IsPopulated(string serverDirectory) {
		var clientPath = PathMapper.ReplaceStart(serverDirectory, _context.Directory, _syncContext.RootDirectory);
		return Directory.Exists(clientPath)
			&& !File.GetAttributes(clientPath).HasAnySyncFlag(SyncAttributes.OFFLINE);
	}

	public void Dispose() {
		_cancellationTokenSource.Cancel();
		_cancellationTokenSource.Dispose();
	}
}
