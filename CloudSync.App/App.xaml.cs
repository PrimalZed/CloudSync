using H.NotifyIcon;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Input;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace CloudSync.App; 
/// <summary>
/// Provides application-specific behavior to supplement the default Application class.
/// </summary>
public partial class App : Application {
	private Window? m_window;
	public bool Exiting { get; set; } = false;
	public TaskbarIcon? TrayIcon { get; private set; }

	/// <summary>
	/// Initializes the singleton application object.  This is the first line of authored code
	/// executed, and as such is the logical equivalent of main() or WinMain().
	/// </summary>
	public App() {
		this.InitializeComponent();
	}

	/// <summary>
	/// Invoked when the application is launched.
	/// </summary>
	/// <param name="args">Details about the launch request and process.</param>
	protected override void OnLaunched(LaunchActivatedEventArgs args) {
		InitializeTrayIcon();
		m_window = new MainWindow();
		m_window.Closed += (sender, args) => {
			if (Exiting) {
				return;
			}
			args.Handled = true;
			m_window.AppWindow.Hide();
		};
		m_window.Activate();
	}

	private void InitializeTrayIcon() {
		var showHideWindowCommand = (XamlUICommand)Resources["ShowHideWindowCommand"];
		showHideWindowCommand.ExecuteRequested += ShowHideWindowCommand_ExecuteRequested;

		var exitApplicationCommand = (XamlUICommand)Resources["ExitApplicationCommand"];
		exitApplicationCommand.ExecuteRequested += ExitApplicationCommand_ExecuteRequested;

		TrayIcon = (TaskbarIcon)Resources["TrayIcon"];
		TrayIcon.ForceCreate();
	}

	private void ShowHideWindowCommand_ExecuteRequested(object? _, ExecuteRequestedEventArgs args) {
		if (m_window!.Visible) {
			m_window.Hide();
		}
		else {
			m_window.Show();
		}
	}

	private void ExitApplicationCommand_ExecuteRequested(object? _, ExecuteRequestedEventArgs args) {
		Exiting = true;
		TrayIcon?.Dispose();
		m_window!.Close();
	}
}
