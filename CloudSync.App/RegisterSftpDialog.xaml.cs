using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml.Controls;
using PrimalZed.CloudSync.App.ViewModels;
using Windows.Globalization.NumberFormatting;

namespace PrimalZed.CloudSync.App {
	/// <summary>
	/// An empty page that can be used on its own or navigated to within a Frame.
	/// </summary>
	public sealed partial class RegisterSftpDialog : ContentDialog {
		public new SftpContextViewModel DataContext {
			get => (SftpContextViewModel)base.DataContext;
			set => base.DataContext = value;
		}

		public readonly DecimalFormatter IntegerFormatter = new DecimalFormatter {
			FractionDigits = 0,
		};

		public RegisterSftpDialog() {
			InitializeComponent();
			DataContext = App.Current.ServiceProvider.GetRequiredService<SftpContextViewModel>();
		}
	}
}
