using PrimalZed.CloudSync.Abstractions;
using PrimalZed.CloudSync.Remote.Abstractions;

namespace PrimalZed.CloudSync.Remote;
public class RemoteFactory<T>(SyncProviderContextAccessor contextAccessor, IEnumerable<LazyRemote<T>> options) {
	public T Create() =>
		options.Single(lazy => lazy.RemoteKind == contextAccessor.Context.RemoteKind).Value;
}
