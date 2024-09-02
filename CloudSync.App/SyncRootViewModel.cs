using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using PrimalZed.CloudSync.Management.Abstractions;

namespace CloudSync.App;

public partial class SyncRootViewModel : ObservableObject {
  private readonly ISyncRootRegistrar _registrar;
  private readonly IServiceController _serviceController;
  private readonly ILogger _logger;

  [ObservableProperty]
  private bool isRegistered;
  //public bool IsRegistered => _registrar.IsRegistered();
  [ObservableProperty]
  private bool isPending = false;
  [ObservableProperty]
  private string? error;

  public bool IsReady => !IsPending;

  public SyncRootViewModel(
    ISyncRootRegistrar registrar,
    IServiceController serviceController,
    ILogger<SyncRootViewModel> logger
  ) {
    _registrar = registrar;
    _serviceController = serviceController;
    _logger = logger;
    Task.Run(UpdateIsRegistered);
  }

  [RelayCommand(AllowConcurrentExecutions = false, CanExecute = nameof(IsReady))]
  private async Task Register() {
    IsPending = true;
    RegisterCommand.NotifyCanExecuteChanged();
    UnregisterCommand.NotifyCanExecuteChanged();
    Error = null;
    await _serviceController.Stop();
    try {
      await _registrar.RegisterAsync();
    }
    catch (Exception ex) {
      _logger.LogError(ex, "Could not register");
      Error = $"Could not unregister: {ex.GetType()}, {ex.Message}, {ex.HResult}";
    }
    try {
      await _serviceController.Start();
    }
    catch (Exception ex) {
      _logger.LogError(ex, "Could not start service");
    }
    IsPending = false;
    RegisterCommand.NotifyCanExecuteChanged();
    UnregisterCommand.NotifyCanExecuteChanged();
    await UpdateIsRegistered();
  }

  [RelayCommand(AllowConcurrentExecutions = false, CanExecute = nameof(IsReady))]
  private async Task Unregister() {
    IsPending = true;
    RegisterCommand.NotifyCanExecuteChanged();
    UnregisterCommand.NotifyCanExecuteChanged();
    Error = null;
    await _serviceController.Stop();
    try {
      await _registrar.Unregister();
    }
    catch (Exception ex) {
      _logger.LogError(ex, "Could not unregister");
      Error = $"Could not unregister: {ex.GetType()}, {ex.Message}, {ex.HResult}";
    }
    try {
      await _serviceController.Start();
    }
    catch (Exception ex) {
      _logger.LogError(ex, "Could not start service");
    }
    IsPending = false;
    RegisterCommand.NotifyCanExecuteChanged();
    UnregisterCommand.NotifyCanExecuteChanged();
    await UpdateIsRegistered();
  }

public async Task UpdateIsRegistered() {
    IsRegistered = await _registrar.IsRegistered();
  }
}
