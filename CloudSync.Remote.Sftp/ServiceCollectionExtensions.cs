using Microsoft.Extensions.DependencyInjection;
using PrimalZed.CloudSync.Abstractions;
using PrimalZed.CloudSync.Remote.Abstractions;
using Renci.SshNet;

namespace PrimalZed.CloudSync.Remote.Sftp; 
public static class ServiceCollectionExtensions {
	public static IServiceCollection AddSftpRemoteServices(this IServiceCollection services) =>
		services
			.AddSingleton<SftpContextAccessor>()
			.AddKeyedSingleton<IRemoteContextSetter>("sftp", (sp, key) => sp.GetRequiredService<SftpContextAccessor>())
			.AddSingleton((sp) => sp.GetRequiredKeyedService<IRemoteContextSetter>("sftp"))
			.AddSingleton<ISftpContextAccessor>((sp) => sp.GetRequiredService<SftpContextAccessor>())
			.AddScoped((sp) => {
				var context = sp.GetRequiredService<SyncProviderContextAccessor>();
				if (context.Context.RemoteKind != SftpConstants.KIND) {
					return new SftpClient("fakehost", "fakeuser", "fakepassword");
				}
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
			.AddKeyedScoped<IRemoteReadWriteService, SftpReadWriteService>("sftp")
			.AddScoped((sp) => new LazyRemote<IRemoteReadWriteService>(() => sp.GetRequiredKeyedService<IRemoteReadWriteService>("sftp"), SftpConstants.KIND))
			.AddKeyedScoped<IRemoteReadService>("sftp", (sp, key) => sp.GetRequiredService<IRemoteReadWriteService>())
			.AddScoped((sp) => new LazyRemote<IRemoteReadService>(() => sp.GetRequiredKeyedService<IRemoteReadService>("sftp"), SftpConstants.KIND))
			.AddKeyedScoped<IRemoteWatcher, SftpWatcher>("sftp")
			.AddScoped((sp) => new LazyRemote<IRemoteWatcher>(() => sp.GetRequiredKeyedService<IRemoteWatcher>("sftp"), SftpConstants.KIND));
}
