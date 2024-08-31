using CloudSync.App;

namespace CloudSync.Package {
	public static class MauiProgram {
		public static MauiApp CreateMauiApp() {
			var builder = MauiApp.CreateBuilder();

			builder
				.UseSharedMauiApp();

			return builder.Build();
		}
	}
}
