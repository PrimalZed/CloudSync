using System.Security.Principal;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using PrimalZed.CloudSync.Configuration;
using PrimalZed.CloudSync.Remote.Abstractions;
using Windows.Security.Cryptography;
using Windows.Storage;
using Windows.Storage.Provider;

namespace PrimalZed.CloudSync;
public class SyncRootRegistrar(
	IOptions<ClientOptions> clientOptions,
	IRemoteInfo remoteInfo,
	ILogger<SyncRootRegistrar> logger
) {
	public const string PROVIDER_ID = "PrimalZed:CloudSync";
	private readonly ClientOptions _clientOptions = clientOptions.Value;
	public string Id => $"{PROVIDER_ID}!{WindowsIdentity.GetCurrent().User}!{remoteInfo.AccountId}";

	public async Task RegisterAsync() {
		if (!StorageProviderSyncRootManager.IsSupported()) {
			throw new NotSupportedException("Cloud Storage Provider sync is not supported on this device");
		}
		if (IsRegistered()) {
			logger.LogWarning("Unexpectedly already registered {syncRootId}", Id);
			Unregister();
		}
		var info = new StorageProviderSyncRootInfo {
			Id = Id,
			Path = await StorageFolder.GetFolderFromPathAsync(_clientOptions.Directory),
			DisplayNameResource = $"PrimalZed CloudSync - {remoteInfo.AccountId}",
			IconResource = @"%SystemRoot%\system32\charmap.exe,0",
			HydrationPolicy = StorageProviderHydrationPolicy.Partial,
			HydrationPolicyModifier = StorageProviderHydrationPolicyModifier.AutoDehydrationAllowed,
			PopulationPolicy = (StorageProviderPopulationPolicy)_clientOptions.PopulationPolicy,
			// InSyncPolicy = StorageProviderInSyncPolicy.Default, // StorageProviderInSyncPolicy.FileCreationTime | StorageProviderInSyncPolicy.DirectoryCreationTime;
			ShowSiblingsAsGroup = false,
			// TODO: Get version from package (but also don't crash on debug)
			Version = "1.0.0",
			// HardlinkPolicy = StorageProviderHardlinkPolicy.None,
			// RecycleBinUri = new Uri(""),
			Context = CryptographicBuffer.ConvertStringToBinary(remoteInfo.Identity, BinaryStringEncoding.Utf8),
		};
		// rootInfo.StorageProviderItemPropertyDefinitions.Add()

		logger.LogDebug("Registering {syncRootId}", Id);
		StorageProviderSyncRootManager.Register(info);
	}

	public bool IsRegistered() =>
		StorageProviderSyncRootManager.GetCurrentSyncRoots().Any((x) => x.Id == Id);

	public void Unregister() {
		if (IsRegistered()) {
			logger.LogDebug("Unregistering {syncRootId}", Id);
			StorageProviderSyncRootManager.Unregister(Id);
		}
	}
}
