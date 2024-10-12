using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PrimalZed.CloudSync.Remote.Sftp;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace PrimalZed.CloudSync.App.ViewModels; 
public partial class SftpContextViewModel : ObservableValidator {
	private readonly RegistrarViewModel _registrarViewModel;

	[ObservableProperty]
	[NotifyDataErrorInfo]
	[NotifyPropertyChangedFor(nameof(CanRegister))]
	[Required]
	[MaxLength(50)]
	private string _syncDirectory = string.Empty;

	[ObservableProperty]
	private bool _syncDirectoryHasErrors;

	[ObservableProperty]
	[NotifyDataErrorInfo]
	[NotifyPropertyChangedFor(nameof(AccountId))]
	[NotifyPropertyChangedFor(nameof(SftpContext))]
	[NotifyPropertyChangedFor(nameof(CanRegister))]
	[Required]
	[MaxLength(50)]
	private string _host = string.Empty;

	[ObservableProperty]
	private bool _hostHasErrors;

	[ObservableProperty]
	[NotifyDataErrorInfo]
	[NotifyPropertyChangedFor(nameof(AccountId))]
	[NotifyPropertyChangedFor(nameof(SftpContext))]
	[NotifyPropertyChangedFor(nameof(CanRegister))]
	[Range(0, int.MaxValue)]
	private int _port = 22;

	[ObservableProperty]
	private bool _portHasErrors;

	[ObservableProperty]
	[NotifyDataErrorInfo]
	[NotifyPropertyChangedFor(nameof(AccountId))]
	[NotifyPropertyChangedFor(nameof(SftpContext))]
	[NotifyPropertyChangedFor(nameof(CanRegister))]
	[Required]
	[MaxLength(50)]
	private string _directory = string.Empty;

	[ObservableProperty]
	private bool _directoryHasErrors;

	[ObservableProperty]
	[NotifyDataErrorInfo]
	[NotifyPropertyChangedFor(nameof(AccountId))]
	[NotifyPropertyChangedFor(nameof(SftpContext))]
	[NotifyPropertyChangedFor(nameof(CanRegister))]
	[Required]
	[MaxLength(50)]
	private string _username = string.Empty;

	[ObservableProperty]
	private bool _usernameHasErrors;

	[ObservableProperty]
	[NotifyDataErrorInfo]
	[NotifyPropertyChangedFor(nameof(SftpContext))]
	[NotifyPropertyChangedFor(nameof(CanRegister))]
	[Required]
	[MaxLength(50)]
	private string _password = string.Empty;

	[ObservableProperty]
	private bool _passwordHasErrors;

	[ObservableProperty]
	[NotifyDataErrorInfo]
	[NotifyPropertyChangedFor(nameof(SftpContext))]
	[NotifyPropertyChangedFor(nameof(CanRegister))]
	[Range(1, int.MaxValue)]
	private int _watchPeriodSeconds = 2;

	public string AccountId => $"{SftpConstants.KIND}!{SftpContext.Host}:{SftpContext.Port}!{SftpContext.Directory.Replace("/", "|")}!{SftpContext.Username}";

	public SftpContext SftpContext => new() {
		Host = Host.Trim(),
		Port = Port,
		Directory = Directory.Trim(),
		Username = Username.Trim(),
		Password = Password,
		WatchPeriodSeconds = WatchPeriodSeconds,
	};

	public bool CanRegister => _registrarViewModel.IsReady
		&& !HasErrors;

	public SftpContextViewModel(RegistrarViewModel registrarViewModel) {
		_registrarViewModel = registrarViewModel;
		PropertyChanged += SftpContextViewModel_PropertyChanged;
		ErrorsChanged += SftpContextViewModel_ErrorsChanged;
		ValidateAllProperties();
	}

	[RelayCommand(AllowConcurrentExecutions = false, CanExecute = nameof(CanRegister))]
	private Task RegisterSftp() =>
		_registrarViewModel.Register(SyncDirectory, AccountId, SftpContext);

	private void Registrar_PropertyChanged(object? sender, PropertyChangedEventArgs e) {
		switch (e.PropertyName) {
			case nameof(RegistrarViewModel.IsReady):
				OnPropertyChanged(nameof(CanRegister));
				RegisterSftpCommand.NotifyCanExecuteChanged();
				break;
		}
	}

	private void SftpContextViewModel_PropertyChanged(object? sender, PropertyChangedEventArgs e) {
		switch (e.PropertyName) {
			case nameof(HasErrors):
				OnPropertyChanged(nameof(CanRegister));
				RegisterSftpCommand.NotifyCanExecuteChanged();
				break;
		}
	}

	private void SftpContextViewModel_ErrorsChanged(object? sender, DataErrorsChangedEventArgs e) {
		var hasErrors = GetErrors(e.PropertyName).Any();
		switch (e.PropertyName) {
			case nameof(SyncDirectory):
				SyncDirectoryHasErrors = hasErrors;
				break;
			case nameof(Host):
				HostHasErrors = hasErrors;
				break;
			case nameof(Port):
				PortHasErrors = hasErrors;
				break;
			case nameof(Directory):
				DirectoryHasErrors = hasErrors;
				break;
			case nameof(Username):
				UsernameHasErrors = hasErrors;
				break;
			case nameof(Password):
				PasswordHasErrors = hasErrors;
				break;
			case nameof(WatchPeriodSeconds):
				break;
		}
	}
}
