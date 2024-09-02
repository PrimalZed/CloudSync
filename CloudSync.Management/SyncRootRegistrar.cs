using System.Security.Principal;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Win32;
using PrimalZed.CloudSync.Configuration;
using PrimalZed.CloudSync.Interop;
using PrimalZed.CloudSync.Management.Abstractions;
using PrimalZed.CloudSync.Remote.Abstractions;
using Vanara.PInvoke;
using Windows.Security.Cryptography;
using Windows.Storage;
using Windows.Storage.Provider;

namespace PrimalZed.CloudSync.Management;
public class SyncRootRegistrar(
	IOptions<ProviderOptions> providerOptions,
	IOptions<ClientOptions> clientOptions,
	IRemoteInfo remoteInfo,
	ILogger<SyncRootRegistrar> logger
) : ISyncRootRegistrar {
	private readonly ProviderOptions _providerOptions = providerOptions.Value;
	private readonly ClientOptions _clientOptions = clientOptions.Value;
	public string Id => $"{_providerOptions.ProviderId}!{WindowsIdentity.GetCurrent().User}!{remoteInfo.AccountId}";

	public async Task RegisterAsync() {
		if (!StorageProviderSyncRootManager.IsSupported()) {
			throw new NotSupportedException("Cloud Storage Provider sync is not supported on this device");
		}
		if (await IsRegistered()) {
			logger.LogWarning("Unexpectedly already registered {syncRootId}", Id);
			Unregister();
		}
		var info = new StorageProviderSyncRootInfo {
			Id = Id,
			ProviderId = new Guid("85950cf5-1dcf-4584-a73c-dca302abf58d"),
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

	public async Task<bool> IsRegistered() {
		using var hklm = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Default);
		using var syncRootManagerKey = hklm.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\SyncRootManager");
		if (syncRootManagerKey is null) {
			return false;
		}
		var syncRootIds = syncRootManagerKey.GetSubKeyNames();
		if (!syncRootIds.Any((x) => x.StartsWith(_providerOptions.ProviderId + "!"))) {
			return false;
		}
		using var syncRootKey = syncRootManagerKey.OpenSubKey(syncRootIds.First((x) => x.StartsWith(_providerOptions.ProviderId + "!")));
		if (!syncRootKey!.GetSubKeyNames().Any()) {
			return false;
		}
		var roots = StorageProviderSyncRootManager.GetCurrentSyncRoots().ToArray();
		//var folder = await StorageFolder.GetFolderFromPathAsync(_clientOptions.Directory);
		//var info = StorageProviderSyncRootManager.GetSyncRootInformationForFolder(folder);
		//var otherInfo = CldApi.CfGetSyncRootInfoByPath<CldApi.CF_SYNC_ROOT_PROVIDER_INFO>(_clientOptions.Directory);
		//return roots.Any((x) => x.Id.StartsWith(providerOptions.Value.ProviderId + "!"));
		return syncRootIds.Any((x) => x.StartsWith(providerOptions.Value.ProviderId + "!"));
	}

	public async Task Unregister() {
		if (await IsRegistered()) {
			logger.LogDebug("Unregistering {syncRootId}", Id);
			//var unregisterResult = CldApi.CfUnregisterSyncRoot(_clientOptions.Directory);
			//// Continue to removing regkey on 390 ERROR_CLOUD_FILE_NOT_UNDER_SYNC_ROOT
			//if (!unregisterResult.Succeeded && unregisterResult.Code != 390) {
			//	unregisterResult.ThrowIfFailed();
			//}
			//using var hklm = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Default);
			//var keyPath = Path.Join(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\SyncRootManager", Id);
			//hklm.DeleteSubKeyTree(Path.Join(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\SyncRootManager", Id));
			StorageProviderSyncRootManager.Unregister(Id);
		}
	}
}
