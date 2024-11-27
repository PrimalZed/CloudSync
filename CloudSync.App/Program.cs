using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using PrimalZed.CloudSync;
using PrimalZed.CloudSync.App;
using PrimalZed.CloudSync.App.ViewModels;
using PrimalZed.CloudSync.DependencyInjection;
using PrimalZed.CloudSync.Remote.Local;
using PrimalZed.CloudSync.Remote.Sftp;
using PrimalZed.CloudSync.Shell;
using PrimalZed.CloudSync.Shell.DependencyInjection;
using Serilog;

var builder = Host.CreateApplicationBuilder(args);
builder.Logging.AddSerilog(
	new LoggerConfiguration()
		.MinimumLevel.Information()
		.MinimumLevel.Override("PrimalZed", Serilog.Events.LogEventLevel.Debug)
		.WriteTo.Async(a =>
			a.File(
				Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "PrimalZed", "CloudSync", "log.txt"),
				fileSizeLimitBytes: 10 * 1048576L, // 10 MB
				rollOnFileSizeLimit: true,
				retainedFileCountLimit: 10,
				retainedFileTimeLimit: TimeSpan.FromDays(7)
			)
		)
		.CreateLogger(),
	true
);

builder.Services
	.AddLocalRemoteServices()
	.AddSftpRemoteServices()
	.AddCloudSyncWorker();

// Shell
builder.Services
	.AddCommonClassObjects()
	//.AddLocalClassObjects()
	.AddSingleton<ShellRegistrar>()
	.AddHostedService<ShellWorker>();

builder.Services
	.AddHostedService<SyncProviderWorker>()
	.AddSingleton<LocalContextViewModel>()
	.AddSingleton<SftpContextViewModel>()
	.AddSingleton<RegistrarViewModel>()
	.AddOptions<AppOptions>()
	.Configure<IConfiguration>((options, config) => {
		var silentConfig = config.GetSection("silent");
		options.IsSilentStart = silentConfig.Value is not null;
	})
	.Services
	.AddHostedService<AppService>();

var host = builder.Build();

WinRT.ComWrappersSupport.InitializeComWrappers();
// TODO: Efficiency mode?

await host.StartAsync();
