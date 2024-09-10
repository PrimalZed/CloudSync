using H.NotifyIcon;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace PrimalZed.CloudSync.App; 
/// <summary>
/// Provides application-specific behavior to supplement the default Application class.
/// </summary>
public partial class App : Application {
	/// <summary>
	/// Gets the current <see cref="App"/> instance in use
	/// </summary>
	public new static App Current => (App)Application.Current;
	/// <summary>
	/// Gets the <see cref="IServiceProvider"/> instance to resolve application services.
	/// </summary>
	public required IServiceProvider ServiceProvider { get; init; }
	public bool Exiting { get; private set; } = false;
	public Window? Window { get; private set; }
	private TaskbarIcon? taskbarIcon;

	/// <summary>
	/// Initializes the singleton application object.  This is the first line of authored code
	/// executed, and as such is the logical equivalent of main() or WinMain().
	/// </summary>
	public App() {
		InitializeComponent();
	}

	/// <summary>
	/// Invoked when the application is launched.
	/// </summary>
	/// <param name="args">Details about the launch request and process.</param>
	protected override void OnLaunched(LaunchActivatedEventArgs args) {
		InitializeTrayIcon();
		Window = new Window {
			Content = new Frame {
				Content = new MainPage(),
			},
		};
		Window.Closed += (sender, args) => {
			if (Exiting) {
				return;
			}
			args.Handled = true;
			Window.AppWindow.Hide();
		};
		Window.Activate();
	}

	private void InitializeTrayIcon() {
		var showHideWindowCommand = (XamlUICommand)Resources["ShowHideWindowCommand"];
		showHideWindowCommand.ExecuteRequested += ShowHideWindowCommand_ExecuteRequested;

		var exitApplicationCommand = (XamlUICommand)Resources["ExitApplicationCommand"];
		exitApplicationCommand.ExecuteRequested += ExitApplicationCommand_ExecuteRequested;

		taskbarIcon = (TaskbarIcon)Resources["TrayIcon"];
		taskbarIcon.TrayIcon.Create();
	}

	private void ShowHideWindowCommand_ExecuteRequested(object? _, ExecuteRequestedEventArgs args) {
		if (Window!.Visible) {
			Window.Hide();
		}
		else {
			Window.Show();
		}
	}

	private void ExitApplicationCommand_ExecuteRequested(object? _, ExecuteRequestedEventArgs args) {
		Exiting = true;
		taskbarIcon?.Dispose();
		Window!.Close();
	}
}
