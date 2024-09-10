using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using PrimalZed.CloudSync.App.ViewModels;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.
namespace PrimalZed.CloudSync.App;
/// <summary>
/// An empty window that can be used on its own or navigated to within a Frame.
/// </summary>
public sealed partial class MainPage : Page {
	public new RegistrarViewModel DataContext {
		get => (RegistrarViewModel)base.DataContext;
		set => base.DataContext = value;
	}

	public MainPage() {
		InitializeComponent();
		DataContext = App.Current.ServiceProvider.GetRequiredService<RegistrarViewModel>();
		DataContext.UpdateSyncRoots();
	}

	private async void RegisterLocal_Click(object sender, RoutedEventArgs e) {
		var dialog = new RegisterLocalDialog {
			XamlRoot = XamlRoot,
		};
		await dialog.ShowAsync();
	}

	private async void RegisterSftp_Click(object sender, RoutedEventArgs e) {
		var dialog = new RegisterSftpDialog {
			XamlRoot = XamlRoot,
		};
		await dialog.ShowAsync();
	}

	private void IdListBox_SelectionChanged(object sender, SelectionChangedEventArgs e) {
		DataContext.UnregisterCommand.NotifyCanExecuteChanged();
	}
}
