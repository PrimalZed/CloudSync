using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml.Controls;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.
namespace CloudSync.App;
/// <summary>
/// An empty window that can be used on its own or navigated to within a Frame.
/// </summary>
public sealed partial class MainPage : Page {
	public new SyncRootViewModel DataContext {
		get => (SyncRootViewModel)base.DataContext;
		set => base.DataContext = value;
	}

	public MainPage() {
		InitializeComponent();
		DataContext = App.Current.ServiceProvider.GetRequiredService<SyncRootViewModel>();
		DataContext.UpdateIsRegistered();
	}
}
