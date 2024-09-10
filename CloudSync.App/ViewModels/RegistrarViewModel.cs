using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using PrimalZed.CloudSync.Abstractions;
using PrimalZed.CloudSync.Commands;
using PrimalZed.CloudSync.Remote.Sftp;
using Windows.Storage;

namespace PrimalZed.CloudSync.App.ViewModels;
public partial class RegistrarViewModel(
	SyncRootRegistrar registrar,
	SyncProviderPool syncProviderPool,
	ILogger<MainPage> logger
) : ObservableObject {
	[ObservableProperty]
	[NotifyPropertyChangedFor(nameof(IsRegistered))]
	private IReadOnlyList<SyncRootInfo> syncRoots = [];
	[ObservableProperty]
	[NotifyPropertyChangedFor(nameof(IsReady))]
	private bool isPending = false;
	[ObservableProperty]
	private string? error;

	public bool IsRegistered => SyncRoots.Any();
	public bool IsReady => !IsPending;

	[RelayCommand(AllowConcurrentExecutions = false, CanExecute = nameof(IsReady))]
	private Task RegisterSftp(SftpContext context) =>
		Register($"Sftp!{context.Host}:{context.Port}!{context.Directory.Replace("/", "|")}!{context.Username}", context);

	public async Task Register<T>(string accountId, T context) where T : struct {
		IsPending = true;
		RegisterSftpCommand.NotifyCanExecuteChanged();
		UnregisterCommand.NotifyCanExecuteChanged();
		Error = null;
		try {
			var registerCommand = new RegisterSyncRootCommand {
				AccountId = accountId,
				Directory = @"C:\SyncTestClient",
				PopulationPolicy = PopulationPolicy.Full,
			};
			var storageFolder = await StorageFolder.GetFolderFromPathAsync(registerCommand.Directory);
			var info = registrar.Register(registerCommand, storageFolder, context);
			syncProviderPool.Start(info);
		}
		catch (Exception ex) {
			logger.LogError(ex, "Could not register");
			Error = $"Could not register: {ex.GetType()}, {ex.Message}, {ex.HResult}";
		}
		IsPending = false;
		RegisterSftpCommand.NotifyCanExecuteChanged();
		UnregisterCommand.NotifyCanExecuteChanged();
		UpdateSyncRoots();
	}

	[RelayCommand(AllowConcurrentExecutions = false, CanExecute = nameof(CanUnregister))]
	private async Task Unregister(SyncRootInfo syncRoot) {
		IsPending = true;
		RegisterSftpCommand.NotifyCanExecuteChanged();
		UnregisterCommand.NotifyCanExecuteChanged();
		Error = null;
		try {
			await syncProviderPool.Stop(syncRoot.Id);
			registrar.Unregister(syncRoot.Id);
		}
		catch (Exception ex) {
			logger.LogError(ex, "Could not unregister");
			Error = $"Could not register: {ex.GetType()}, {ex.Message}, {ex.HResult}";
		}
		IsPending = false;
		RegisterSftpCommand.NotifyCanExecuteChanged();
		UnregisterCommand.NotifyCanExecuteChanged();
		UpdateSyncRoots();
	}

	public bool CanUnregister(SyncRootInfo? syncRoot) =>
		IsReady && syncRoot is not null;

	public void UpdateSyncRoots() {
		SyncRoots = registrar.GetSyncRoots();
	}
}
