using Microsoft.Extensions.Logging;
using PrimalZed.CloudSync.Helpers;
using PrimalZed.CloudSync.Interop;
using PrimalZed.CloudSync.Remote.Abstractions;
using System.Threading.Channels;
using Vanara.PInvoke;
using static Vanara.PInvoke.CldApi;

namespace PrimalZed.CloudSync;
public record Callback {
	public required string RootDirectory { get; init; }
	public required CF_CALLBACK_TYPE Type { get; init; }
	public required CF_CALLBACK_INFO Info { get; init; }
	public CallbackParameters Parameters { get; init; } = new CallbackParameters();
}
public record CallbackParameters {}
public record RenameCompletionCallbackParameters : CallbackParameters {
	public required string SourcePath { get; init; }
}
public sealed class SyncRootConnector(
	IRemoteReadWriteService remoteService,
	ILogger<SyncRootConnector> logger
) : IDisposable {
	private readonly Channel<Callback> _channel =
		Channel.CreateUnbounded<Callback>(
			new UnboundedChannelOptions {
				SingleReader = true,
			}
		);
	private readonly CancellationTokenSource _disposeTokenSource = new ();

	public CF_CONNECTION_KEY Connect(string rootDirectory, CancellationToken stoppingToken = default) {
		logger.LogDebug("Connecting sync provider to {syncRootPath}", rootDirectory);
		CloudFilter.ConnectSyncRoot(
			rootDirectory,
			new SyncRootEvents {
				FetchPlaceholders = (in CF_CALLBACK_INFO callbackInfo, in CF_CALLBACK_PARAMETERS callbackParameters) =>
					FetchPlaceholders(rootDirectory, callbackInfo, callbackParameters),
				FetchData = (in CF_CALLBACK_INFO callbackInfo, in CF_CALLBACK_PARAMETERS callbackParameters) =>
					FetchData(rootDirectory, callbackInfo, callbackParameters),
				OnCloseCompletion = OnCloseCompletion,
				OnRenameCompletion = (in CF_CALLBACK_INFO callbackInfo, in CF_CALLBACK_PARAMETERS callbackParameters) => 
					_channel.Writer.WriteAsync(new Callback {
						RootDirectory = rootDirectory,
						Type = CF_CALLBACK_TYPE.CF_CALLBACK_TYPE_NOTIFY_RENAME_COMPLETION,
						Info = callbackInfo,
						Parameters = new RenameCompletionCallbackParameters {
							SourcePath = callbackParameters.RenameCompletion.SourcePath,
						},
					}),
				OnDeleteCompletion = (in CF_CALLBACK_INFO callbackInfo, in CF_CALLBACK_PARAMETERS callbackParameters) =>
					_channel.Writer.WriteAsync(new Callback {
						RootDirectory = rootDirectory,
						Type = CF_CALLBACK_TYPE.CF_CALLBACK_TYPE_NOTIFY_DELETE_COMPLETION,
						Info = callbackInfo,
					}),
			},
			out var connectionKey
		);

		return connectionKey;
	}

	public void Disconnect(CF_CONNECTION_KEY connectionKey) {
		logger.LogDebug("Disconnecting sync provider, {connectionKey}", connectionKey);
		CloudFilter.DisconnectSyncRoot(connectionKey);
	}
		
	public async Task ProcessQueueAsync(CancellationToken stoppingToken = default) {
		var cancellationToken = CancellationTokenSource.CreateLinkedTokenSource(stoppingToken, _disposeTokenSource.Token).Token;
		while (!cancellationToken.IsCancellationRequested) {
			var e = await _channel.Reader.ReadAsync(cancellationToken);
			var task = e.Type switch {
				CF_CALLBACK_TYPE.CF_CALLBACK_TYPE_NOTIFY_RENAME_COMPLETION => OnRenameCompletion(e.RootDirectory, e.Info, e.Parameters),
				CF_CALLBACK_TYPE.CF_CALLBACK_TYPE_NOTIFY_DELETE_COMPLETION => OnDeleteCompletion(e.RootDirectory, e.Info),
				_ => throw new NotImplementedException(),
			};
			await task;
		}
	}

	private void FetchPlaceholders(string rootDirectory, CF_CALLBACK_INFO callbackInfo, CF_CALLBACK_PARAMETERS callbackParameters) {
		logger.LogDebug("Fetch Placeholders '{path}' '{pattern}'", callbackInfo.NormalizedPath, callbackParameters.FetchPlaceholders.Pattern);
		var clientDirectory = Path.Join(callbackInfo.VolumeDosName, callbackInfo.NormalizedPath[1..]);
		var relativeDirectory = PathMapper.GetRelativePath(clientDirectory, rootDirectory);
		var fileInfos = remoteService.EnumerateFiles(relativeDirectory, callbackParameters.FetchPlaceholders.Pattern);
		var directoryInfos = remoteService.EnumerateDirectories(relativeDirectory, callbackParameters.FetchPlaceholders.Pattern);
		var fileSystemInfos = fileInfos.Concat<RemoteFileSystemInfo>(directoryInfos).ToArray();

		CloudFilter.TransferPlaceholders(callbackInfo, fileSystemInfos);
	}

	private async void FetchData(string rootDirectory, CF_CALLBACK_INFO callbackInfo, CF_CALLBACK_PARAMETERS callbackParameters) {
		logger.LogDebug(
			"Fetch data, {file}, fileSize: {fileSize}, offset: {offset}, total: {total}",
			callbackInfo.NormalizedPath,
			callbackInfo.FileSize,
			callbackParameters.FetchData.RequiredFileOffset,
			callbackParameters.FetchData.RequiredLength
		);
		try {
			var clientFile = Path.Join(callbackInfo.VolumeDosName, callbackInfo.NormalizedPath[1..]);

			var buffer = new byte[4096];
			long startOffset = callbackParameters.FetchData.RequiredFileOffset;
			long currentOffset = startOffset;
			long targetOffset = callbackParameters.FetchData.RequiredFileOffset
				+ callbackParameters.FetchData.RequiredLength;
			long readLength = 0;

			var relativeFile = PathMapper.GetRelativePath(clientFile, rootDirectory);
			using var fileStream = await remoteService.GetFileStream(relativeFile);
			fileStream.Seek(currentOffset, SeekOrigin.Begin);
			while (currentOffset <= targetOffset && (readLength = fileStream.Read(buffer, 0, buffer.Length)) > 0) {
				// Update the transfer progress
				CloudFilter.ReportProgress(callbackInfo, fileStream.Length, currentOffset + readLength);
				// TODO: Tell the Shell so File Explorer can display the progress bar in its view

				// This helper function tells the Cloud File API about the transfer,
				// which will copy the data to the local syncroot
				CloudFilter.TransferData(callbackInfo, buffer, currentOffset, readLength);

				currentOffset += readLength;
			}
		}
		catch (Exception ex) {
			logger.LogError(ex, "Failed to transfer server->client");

			CloudFilter.TransferData(
				callbackInfo,
				null,
				callbackParameters.FetchData.RequiredFileOffset,
				callbackParameters.FetchData.RequiredLength,
				success: false
			);
		}
	}

	private void OnCloseCompletion(in CF_CALLBACK_INFO callbackInfo, in CF_CALLBACK_PARAMETERS callbackParameters) {
		logger.LogDebug("SyncRoot CloseCompletion {path} {flags}", callbackInfo.NormalizedPath, callbackParameters.CloseCompletion.Flags);
	}

	private async Task OnRenameCompletion(string rootDirectory, CF_CALLBACK_INFO callbackInfo, CallbackParameters callbackParameters) {
		if (callbackParameters is not RenameCompletionCallbackParameters renameCompletionParameters) {
			throw new ArgumentException($"Unexpected parameters type {callbackParameters.GetType()}", nameof(callbackParameters));
		}
		logger.LogDebug("SyncRoot Rename {old} -> {new}", renameCompletionParameters.SourcePath, callbackInfo.NormalizedPath);
		var oldClientPath = Path.Join(callbackInfo.VolumeDosName, renameCompletionParameters.SourcePath[1..]);
		var oldRelativePath = PathMapper.GetRelativePath(oldClientPath, rootDirectory);
		var newClientPath = Path.Join(callbackInfo.VolumeDosName, callbackInfo.NormalizedPath[1..]);
		try {
			if (!remoteService.Exists(oldRelativePath)) {
				return;
			}
			// If moving outside of sync directory, treat like a delete
			if (!newClientPath.StartsWith(rootDirectory)) {
				if (remoteService.IsDirectory(oldRelativePath)) {
					remoteService.DeleteDirectory(oldClientPath);
				}
				else {
					remoteService.DeleteFile(oldClientPath);
				}
				return;
			}
			if (File.GetAttributes(newClientPath).HasFlag(FileAttributes.Directory)) {
				remoteService.MoveDirectory(oldClientPath, newClientPath);
			}
			else {
				remoteService.MoveFile(oldClientPath, newClientPath);
			}
		}
		catch (Exception ex) {
			logger.LogError(ex, "Rename server object failed");
		}
	}

	private async Task OnDeleteCompletion(string rootDirectory, CF_CALLBACK_INFO callbackInfo) {
		logger.LogDebug("SyncRoot Delete {path}", callbackInfo.NormalizedPath);
		var clientPath = Path.Join(callbackInfo.VolumeDosName, callbackInfo.NormalizedPath[1..]);
		// For files created in client, sometimes it's not actually deleted yet. Wait until it's really gone.
		for (var attempt = 0; attempt < 60 && Path.Exists(clientPath); attempt++) {
			logger.LogDebug("File has not yet been deleted, waiting before retry");
			await Task.Delay(500);
		}
		if (Path.Exists(clientPath)) {
			logger.LogWarning("Received delete completion, but file has not been deleted: {clientPath}", clientPath);
			return;
		}
		var relativePath = PathMapper.GetRelativePath(clientPath, rootDirectory);
		if (!remoteService.Exists(relativePath)) {
			return;
		}
		try {
			if (remoteService.IsDirectory(relativePath)) {
				// serverService.DeleteDirectory(clientPath);
			}
			else {
				remoteService.DeleteFile(clientPath);
			}
		}
		catch (Exception ex) {
			logger.LogError(ex, "Delete server object failed");
		}
	}

	public void Dispose() {
		_disposeTokenSource.Cancel();
		_disposeTokenSource.Dispose();
	}
}
