using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.UI.Xaml;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace CloudSync.App;
/// <summary>
/// An empty window that can be used on its own or navigated to within a Frame.
/// </summary>
[ObservableObject]
public sealed partial class MainWindow : Window {
	[ObservableProperty]
	private bool isRegistered;
	[ObservableProperty]
	private bool isPending = false;
	[ObservableProperty]
	private string? error;

	public bool IsReady => !IsPending;
	public MainWindow() {
		InitializeComponent();
	}

	[RelayCommand(AllowConcurrentExecutions = false, CanExecute = nameof(IsReady))]
	private async Task Register() {
	}

	[RelayCommand(AllowConcurrentExecutions = false, CanExecute = nameof(IsReady))]
	private async Task Unregister() {

	}

	private async void RegistrarPipe_ReceivedMessage(object? sender, string e) {
	}

	public async Task UpdateIsRegistered() {
	}
}
