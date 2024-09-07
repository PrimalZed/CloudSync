using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using PrimalZed.CloudSync;
using PrimalZed.CloudSync.Commands;
using Windows.Storage;

namespace CloudSync.App; 
public partial class SyncRootViewModel(
	SyncRootRegistrar registrar,
	SyncProviderPool syncProviderPool,
	ILogger<MainPage> logger
) : ObservableObject {
	[ObservableProperty]
	private bool isRegistered;
	[ObservableProperty]
	[NotifyPropertyChangedFor(nameof(IsReady))]
	private bool isPending = false;
	[ObservableProperty]
	private string? error;

	public bool IsReady => !IsPending;

	[RelayCommand(AllowConcurrentExecutions = false, CanExecute = nameof(IsReady))]
	private async Task Register() {
		IsPending = true;
		RegisterCommand.NotifyCanExecuteChanged();
		UnregisterCommand.NotifyCanExecuteChanged();
		Error = null;
		try {
			var registerCommand = new RegisterSyncRootCommand {
				AccountId = "TestAccount1",
				Directory = @"C:\SyncTestClient",
				PopulationPolicy = PopulationPolicy.Full,
			};
			var storageFolder = await StorageFolder.GetFolderFromPathAsync(registerCommand.Directory);
			registrar.Register(registerCommand, storageFolder);
			syncProviderPool.Start(registerCommand.Directory, (Windows.Storage.Provider.StorageProviderPopulationPolicy)registerCommand.PopulationPolicy);
		}
		catch (Exception ex) {
			logger.LogError(ex, "Could not register");
			Error = $"Could not register: {ex.GetType()}, {ex.Message}, {ex.HResult}";
		}
		IsPending = false;
		RegisterCommand.NotifyCanExecuteChanged();
		UnregisterCommand.NotifyCanExecuteChanged();
		UpdateIsRegistered();
	}

	[RelayCommand(AllowConcurrentExecutions = false, CanExecute = nameof(IsReady))]
	private async Task Unregister() {
		IsPending = true;
		RegisterCommand.NotifyCanExecuteChanged();
		UnregisterCommand.NotifyCanExecuteChanged();
		Error = null;
		try {
			await syncProviderPool.Stop(@"C:\SyncTestClient");
			registrar.Unregister("TestAccount1");
		}
		catch (Exception ex) {
			logger.LogError(ex, "Could not unregister");
			Error = $"Could not register: {ex.GetType()}, {ex.Message}, {ex.HResult}";
		}
		IsPending = false;
		RegisterCommand.NotifyCanExecuteChanged();
		UnregisterCommand.NotifyCanExecuteChanged();
		UpdateIsRegistered();
	}

	public void UpdateIsRegistered() {
		IsRegistered = registrar.IsRegistered();
	}
}
