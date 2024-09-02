using Microsoft.Extensions.Options;
using Microsoft.Win32;
using PrimalZed.CloudSync.Configuration;
using Vanara.PInvoke;
using Windows.Storage;
using Windows.Storage.Provider;

namespace PrimalZed.CloudSync; 
public class SyncRootReader(IOptions<ProviderOptions> providerOptions) {
	public async Task<bool> IsRegistered() {
		using var hklm = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Default);
		using var syncRootManagerKey = hklm.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\SyncRootManager");
		if (syncRootManagerKey is null) {
			return false;
		}
		var syncRootIds = syncRootManagerKey.GetSubKeyNames();
		if (!syncRootIds.Any((x) => x.StartsWith(providerOptions.Value.ProviderId + "!"))) {
			return false;
		}
		using var syncRootKey = syncRootManagerKey.OpenSubKey(syncRootIds.First((x) => x.StartsWith(providerOptions.Value.ProviderId + "!")));
		if (!syncRootKey!.GetSubKeyNames().Any()) {
			return false;
		}
		var roots = StorageProviderSyncRootManager.GetCurrentSyncRoots().ToArray();
		using var userSyncRootKey = syncRootKey.OpenSubKey("UserSyncRoots");
		var valueKeyName = userSyncRootKey!.GetValueNames().First();
		var syncRootDirectory = userSyncRootKey.GetValue(valueKeyName) as string;
		//var folder = await StorageFolder.GetFolderFromPathAsync(syncRootDirectory);
		//var info = StorageProviderSyncRootManager.GetSyncRootInformationForFolder(folder);
		var providerInfo = CldApi.CfGetSyncRootInfoByPath<CldApi.CF_SYNC_ROOT_PROVIDER_INFO>(syncRootDirectory!);
		var basicInfo = CldApi.CfGetSyncRootInfoByPath<CldApi.CF_SYNC_ROOT_BASIC_INFO>(syncRootDirectory!);
		var standardInfo = CldApi.CfGetSyncRootInfoByPath<CldApi.CF_SYNC_ROOT_STANDARD_INFO>(syncRootDirectory!);
		//return roots.Any((x) => x.Id.StartsWith(providerOptions.Value.ProviderId + "!"));
		return syncRootIds.Any((x) => x.StartsWith(providerOptions.Value.ProviderId + "!"));
	}
}
