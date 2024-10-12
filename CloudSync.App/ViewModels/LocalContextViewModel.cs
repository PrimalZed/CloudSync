using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PrimalZed.CloudSync.Remote.Local;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace PrimalZed.CloudSync.App.ViewModels; 
public partial class LocalContextViewModel : ObservableValidator {
	private readonly RegistrarViewModel _registrarViewModel;

	[ObservableProperty]
	[NotifyDataErrorInfo]
	[NotifyPropertyChangedFor(nameof(CanRegister))]
	[Required]
	[MaxLength(50)]
	private string _name = string.Empty;

	[ObservableProperty]
	private bool _nameHasErrors;

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
	[NotifyPropertyChangedFor(nameof(LocalContext))]
	[NotifyPropertyChangedFor(nameof(CanRegister))]
	[Required]
	[MaxLength(50)]
	private string _directory = string.Empty;

	[ObservableProperty]
	private bool _directoryHasErrors;

	public string AccountId => $"{LocalConstants.KIND}!{Directory.Replace(@"\", "|")}";
	public LocalContext LocalContext => new() {
		Directory = Directory,
	};

	public bool CanRegister => _registrarViewModel.IsReady
		&& !HasErrors;

	public LocalContextViewModel(RegistrarViewModel registrarViewModel) {
		_registrarViewModel = registrarViewModel;
		PropertyChanged += LocalContextViewModel_PropertyChanged;
		ErrorsChanged += LocalContextViewModel_ErrorsChanged;
		ValidateAllProperties();
	}

	[RelayCommand(AllowConcurrentExecutions = false, CanExecute = nameof(CanRegister))]
	private Task RegisterLocal() =>
		_registrarViewModel.Register(Name, SyncDirectory, AccountId, LocalContext);

	private void LocalContextViewModel_PropertyChanged(object? sender, PropertyChangedEventArgs e) {
		switch (e.PropertyName) {
			case nameof(HasErrors):
				OnPropertyChanged(nameof(CanRegister));
				RegisterLocalCommand.NotifyCanExecuteChanged();
				break;
		}
	}

	private void LocalContextViewModel_ErrorsChanged(object? sender, DataErrorsChangedEventArgs e) {
		var hasErrors = GetErrors(e.PropertyName).Any();
		switch (e.PropertyName) {
			case nameof(Name):
				NameHasErrors = hasErrors;
				break;
			case nameof(Directory):
				DirectoryHasErrors = hasErrors;
				break;
			case nameof(SyncDirectory):
				SyncDirectoryHasErrors = hasErrors;
				break;
		}
	}
}
