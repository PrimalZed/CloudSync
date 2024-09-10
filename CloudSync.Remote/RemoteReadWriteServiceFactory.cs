using PrimalZed.CloudSync.Abstractions;
using PrimalZed.CloudSync.Remote.Abstractions;

namespace PrimalZed.CloudSync.Remote;
public class RemoteReadWriteServiceFactory(SyncProviderContextAccessor contextAccessor, IEnumerable<LazyRemote<IRemoteReadWriteService>> options)
	: RemoteFactory<IRemoteReadWriteService>(contextAccessor, options) { }
