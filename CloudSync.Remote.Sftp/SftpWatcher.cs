using PrimalZed.CloudSync.Remote.Abstractions;

namespace PrimalZed.CloudSync.Remote.Sftp;
public class SftpWatcher : IRemoteWatcher {
	public event RemoteCreateHandler? Created;
	public event RemoteChangeHandler? Changed;
	public event RemoteRenameHandler? Renamed;
	public event RemoteDeleteHandler? Deleted;

	public void Start(CancellationToken stoppingToken = default) {
	}

	public void Dispose() {
	}
}
