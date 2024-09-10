using PrimalZed.CloudSync.Abstractions;
using PrimalZed.CloudSync.Remote.Abstractions;

namespace PrimalZed.CloudSync.Remote;
public class RemoteWatcherFactory(SyncProviderContextAccessor contextAccessor, IEnumerable<LazyRemote<IRemoteWatcher>> options)
	: RemoteFactory<IRemoteWatcher>(contextAccessor, options) { }
