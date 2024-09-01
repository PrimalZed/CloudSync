using Microsoft.Extensions.Logging;
using PrimalZed.CloudSync.Management.Abstractions;
using System.ComponentModel;
using System.Windows.Input;

namespace CloudSync.App;

public class SyncRootViewModel : INotifyPropertyChanged {
  private readonly ISyncRootRegistrar _registrar;
  public event PropertyChangedEventHandler? PropertyChanged;

  public bool IsRegistered => _registrar.IsRegistered();

  public ICommand RegisterCommand { get; init; }
  public ICommand UnregisterCommand { get; init; }

  public SyncRootViewModel(ISyncRootRegistrar registrar, ILogger<SyncRootViewModel> logger) {
    _registrar = registrar;
    RegisterCommand = new Command(async () => {
      try {
        await registrar.RegisterAsync();
      }
      catch (Exception ex) {
        logger.LogError(ex, "Could not register");
      }
      OnPropertyChanged(nameof(IsRegistered));
    });
    UnregisterCommand = new Command(() => {
      registrar.Unregister();
      OnPropertyChanged(nameof(IsRegistered));
    });
  }

  public void OnPropertyChanged(string propertyName) {
    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
  }
}
