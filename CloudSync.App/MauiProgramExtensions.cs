using CommunityToolkit.Maui;
using Microsoft.Extensions.Logging;
using System.Threading.Channels;

namespace CloudSync.App;

public static class MauiProgramExtensions
{
	public static MauiAppBuilder UseSharedMauiApp(this MauiAppBuilder builder)
	{
		builder
			.UseMauiApp<App>()
			.UseMauiCommunityToolkit()
			.ConfigureFonts(fonts => {
				fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
				fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
			})
			.Services
			.AddSingleton<MainPage>()
			.AddSingleton<SyncRootViewModel>();

#if DEBUG
		builder.Logging.AddDebug();
#endif

		return builder;
	}
}
