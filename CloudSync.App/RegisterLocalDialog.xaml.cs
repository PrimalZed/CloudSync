using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using PrimalZed.CloudSync.App.ViewModels;
using Windows.Storage;
using Windows.Storage.Pickers;

namespace PrimalZed.CloudSync.App {
	/// <summary>
	/// An empty page that can be used on its own or navigated to within a Frame.
	/// </summary>
	public sealed partial class RegisterLocalDialog : ContentDialog {
		public new LocalContextViewModel DataContext {
			get => (LocalContextViewModel)base.DataContext;
			set => base.DataContext = value;
		}

		public RegisterLocalDialog() {
			InitializeComponent();
			DataContext = App.Current.ServiceProvider.GetRequiredService<LocalContextViewModel>();
		}

		private async void SelectSyncDirectory_Click(object sender, RoutedEventArgs e) {
			var folderPicker = new FolderPicker();
			var windowHandle = WinRT.Interop.WindowNative.GetWindowHandle(App.Current.Window);
			WinRT.Interop.InitializeWithWindow.Initialize(folderPicker, windowHandle);
			folderPicker.SuggestedStartLocation = PickerLocationId.ComputerFolder;
			folderPicker.FileTypeFilter.Add("*");

			// Open the picker for the user to pick a folder
			StorageFolder folder = await folderPicker.PickSingleFolderAsync();
			DataContext.SyncDirectory = folder?.Path ?? string.Empty;
		}

		private async void SelectDirectory_Click(object sender, RoutedEventArgs e) {
			var folderPicker = new FolderPicker();
			var windowHandle = WinRT.Interop.WindowNative.GetWindowHandle(App.Current.Window);
			WinRT.Interop.InitializeWithWindow.Initialize(folderPicker, windowHandle);
			folderPicker.SuggestedStartLocation = PickerLocationId.ComputerFolder;
			folderPicker.FileTypeFilter.Add("*");

			// Open the picker for the user to pick a folder
			StorageFolder folder = await folderPicker.PickSingleFolderAsync();
			DataContext.Directory = folder?.Path ?? string.Empty;
		}
	}
}
