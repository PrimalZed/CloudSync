using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using PrimalZed.CloudSync.Commands;
using PrimalZed.CloudSync.Configuration;
using System.Security.Principal;
using Windows.Security.Cryptography;
using Windows.Storage;
using Windows.Storage.Provider;

namespace PrimalZed.CloudSync;
public class SyncRootRegistrar(
	IOptions<ProviderOptions> providerOptions,
	ILogger<SyncRootRegistrar> logger
) {
	public bool IsRegistered() {
		var roots = StorageProviderSyncRootManager.GetCurrentSyncRoots();
		return roots.Any((x) => x.Id.StartsWith(providerOptions.Value.ProviderId + "!"));
	}

	public void Register(RegisterSyncRootCommand command, IStorageFolder directory) {
		// Stage 1: Setup
		//--------------------------------------------------------------------------------------------
		// The client folder (syncroot) must be indexed in order for states to properly display
		var clientDirectory = new DirectoryInfo(command.Directory);
		clientDirectory.Attributes &= ~System.IO.FileAttributes.NotContentIndexed;

		var id = $"{providerOptions.Value.ProviderId}!{WindowsIdentity.GetCurrent().User}!{command.AccountId}";
		if (IsRegistered()) {
			logger.LogWarning("Unexpectedly already registered {syncRootId}", id);
			Unregister(command.AccountId);
		}
		var info = new StorageProviderSyncRootInfo {
			Id = id,
			Path = directory,
			DisplayNameResource = $"PrimalZed CloudSync - {command.AccountId}",
			IconResource = @"%SystemRoot%\system32\charmap.exe,0",
			HydrationPolicy = StorageProviderHydrationPolicy.Partial,
			HydrationPolicyModifier = StorageProviderHydrationPolicyModifier.AutoDehydrationAllowed,
			PopulationPolicy = (StorageProviderPopulationPolicy)command.PopulationPolicy,
			// InSyncPolicy = StorageProviderInSyncPolicy.Default, // StorageProviderInSyncPolicy.FileCreationTime | StorageProviderInSyncPolicy.DirectoryCreationTime;
			ShowSiblingsAsGroup = false,
			// TODO: Get version from package (but also don't crash on debug)
			Version = "1.0.0",
			// HardlinkPolicy = StorageProviderHardlinkPolicy.None,
			// RecycleBinUri = new Uri(""),
			Context = CryptographicBuffer.ConvertStringToBinary($"Local directory {command.Directory}", BinaryStringEncoding.Utf8),
		};
		// rootInfo.StorageProviderItemPropertyDefinitions.Add()

		logger.LogDebug("Registering {syncRootId}", id);
		StorageProviderSyncRootManager.Register(info);
	}

	public void Unregister(string accountId) {
		var id = $"{providerOptions.Value.ProviderId}!{WindowsIdentity.GetCurrent().User}!{accountId}";
		logger.LogDebug("Unregistering {syncRootId}", id);
		StorageProviderSyncRootManager.Unregister(id);
	}
}
