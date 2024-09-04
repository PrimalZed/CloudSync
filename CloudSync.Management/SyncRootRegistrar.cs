using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using PrimalZed.CloudSync.Abstractions;
using PrimalZed.CloudSync.Commands;
using PrimalZed.CloudSync.Configuration;
using PrimalZed.CloudSync.Management.Abstractions;
using PrimalZed.CloudSync.Remote.Abstractions;
using Vanara.PInvoke;
using Windows.Storage.Provider;

namespace PrimalZed.CloudSync.Management;
public class SyncRootRegistrar(
	IOptions<ProviderOptions> providerOptions,
	IOptions<ClientOptions> clientOptions,
	IRemoteInfo remoteInfo,
	[FromKeyedServices("registrar")] IPipe registrarPipe
) : ISyncRootRegistrar {
	private readonly ProviderOptions _providerOptions = providerOptions.Value;
	private readonly ClientOptions _clientOptions = clientOptions.Value;

	public async Task RegisterAsync() {
		if (!StorageProviderSyncRootManager.IsSupported()) {
			throw new NotSupportedException("Cloud Storage Provider sync is not supported on this device");
		}
		var registerCommand = new RegisterSyncRootCommand {
			AccountId = remoteInfo.AccountId,
			Directory = _clientOptions.Directory,
			PopulationPolicy = Commands.PopulationPolicy.Full
		};
		await registrarPipe.SendMessage(SyncRootCommand.ToMessage(registerCommand));
	}

	public async Task<bool> IsRegistered() {
		var roots = StorageProviderSyncRootManager.GetCurrentSyncRoots().ToArray();
		return roots.Any((x) => x.Id.StartsWith(providerOptions.Value.ProviderId + "!"));
	}

	public async Task Unregister() {
		var command = new UnregisterSyncRootCommand {
			AccountId = remoteInfo.AccountId,
			Directory = _clientOptions.Directory,
		};
		await registrarPipe.SendMessage(SyncRootCommand.ToMessage(command));
	}
}
