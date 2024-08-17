using Windows.ApplicationModel;
using Windows.Foundation;
using Windows.Storage.Provider;
using Windows.UI;

namespace PrimalZed.CloudSync.Shell.Local;
/// <remarks>
/// <see href="https://learn.microsoft.com/en-us/uwp/api/windows.storage.provider.istorageproviderstatusuisource#windows-requirements">Windows 11 Insider Preview only</see>
/// </remarks>
public class LocalStatusUiSource : IStorageProviderStatusUISource {
	public event TypedEventHandler<IStorageProviderStatusUISource, object>? StatusUIChanged;

	public StorageProviderStatusUI GetStatusUI() {
		return new StorageProviderStatusUI {
			ProviderState = StorageProviderState.InSync,
			ProviderStateLabel = "This is all hard-coded info for testing",
			ProviderStateIcon = new Uri(Path.Combine(Package.Current.InstalledPath, @"Images\StoreLogo.png")),
			QuotaUI = new StorageProviderQuotaUI {
				QuotaUsedColor = Color.FromArgb(a: 255, r: 200, g: 50, b: 255),
				QuotaTotalInBytes = 5000000,
				QuotaUsedInBytes = 1000000,
				QuotaUsedLabel = "Yummy Bites"
			}
		};
	}
}
