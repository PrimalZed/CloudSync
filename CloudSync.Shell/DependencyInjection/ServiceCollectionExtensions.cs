using Microsoft.Extensions.DependencyInjection;
using PrimalZed.CloudSync.Shell.Local;

namespace PrimalZed.CloudSync.Shell.DependencyInjection; 
public static class ServiceCollectionExtensions {
	public static IServiceCollection AddClassObject<T>(this IServiceCollection services) where T : class =>
		services
			.AddTransient<T>()
			.AddSingleton<ClassFactory<T>.Generator>((sp) => () => sp.GetRequiredService<T>())
			.AddSingleton<IClassFactoryOf, ClassFactory<T>>();

	public static IServiceCollection AddLocalClassObjects(this IServiceCollection services) =>
		services
			.AddClassObject<LocalThumbnailProvider>()
			.AddClassObject<TestContextMenuCommand>()
			.AddTransient<LocalStatusUiSource>()
			.AddSingleton<CreateStatusUiSource<LocalStatusUiSource>>((sp) => (string syncRootId) => sp.GetRequiredService<LocalStatusUiSource>())
			.AddClassObject<LocalStatusUiSourceFactory>();
}
