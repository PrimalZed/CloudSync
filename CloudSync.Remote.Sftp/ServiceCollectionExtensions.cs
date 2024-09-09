using Microsoft.Extensions.DependencyInjection;
using PrimalZed.CloudSync.Remote.Abstractions;
using Renci.SshNet;

namespace PrimalZed.CloudSync.Remote.Sftp; 
public static class ServiceCollectionExtensions {
	public static IServiceCollection AddSftpRemoteServices(this IServiceCollection services) =>
		services
			.AddSingleton<SftpContextAccessor>()
			.AddSingleton<IRemoteContextSetter>((sp) => sp.GetRequiredService<SftpContextAccessor>())
			.AddSingleton<ISftpContextAccessor>((sp) => sp.GetRequiredService<SftpContextAccessor>())
			.AddScoped((sp) => {
				var contextAccessor = sp.GetRequiredService<ISftpContextAccessor>();
				var client = new SftpClient(
					contextAccessor.Context.Host,
					contextAccessor.Context.Port,
					contextAccessor.Context.Username,
					contextAccessor.Context.Password
				);
				client.Connect();
				return client;
			})
			.AddScoped<IRemoteReadWriteService, SftpReadWriteService>()
			.AddScoped<IRemoteReadService>((sp) => sp.GetRequiredService<IRemoteReadWriteService>())
			.AddScoped<IRemoteWatcher, SftpWatcher>();
}
