using CloudSync.App;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var builder = Host.CreateApplicationBuilder(args);

builder.Services
	.AddTransient<App>()
	.AddSingleton<Func<App>>((sp) => () => sp.GetRequiredService<App>())
	.AddTransient<MainWindow>()
	.AddSingleton<Func<MainWindow>>((sp) => () => sp.GetRequiredService<MainWindow>())
	.AddHostedService<AppService>();

var host = builder.Build();

WinRT.ComWrappersSupport.InitializeComWrappers();

await host.StartAsync();
