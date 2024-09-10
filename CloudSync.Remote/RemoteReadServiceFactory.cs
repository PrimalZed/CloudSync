using PrimalZed.CloudSync.Abstractions;
using PrimalZed.CloudSync.Remote.Abstractions;

namespace PrimalZed.CloudSync.Remote;
public class RemoteReadServiceFactory(SyncProviderContextAccessor contextAccessor, IEnumerable<LazyRemote<IRemoteReadService>> options)
	: RemoteFactory<IRemoteReadService>(contextAccessor, options) { }
