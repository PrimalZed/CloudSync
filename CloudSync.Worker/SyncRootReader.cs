using Microsoft.Extensions.Options;
using PrimalZed.CloudSync.Configuration;
using Windows.Storage.Provider;

namespace PrimalZed.CloudSync; 
public class SyncRootReader(IOptions<ProviderOptions> providerOptions) {
	public bool IsRegistered() =>
		StorageProviderSyncRootManager.GetCurrentSyncRoots()
			.Any((x) => x.Id.StartsWith(providerOptions.Value.ProviderId + "!"));
}
