using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using PrimalZed.CloudSync.Configuration;
using PrimalZed.CloudSync.Interop;
using PrimalZed.CloudSync.Management.Abstractions;
using PrimalZed.CloudSync.Pipes;
using System.Security.Principal;
using Windows.Security.Cryptography;
using Windows.Storage;
using Windows.Storage.Provider;
using static PrimalZed.CloudSync.Pipes.PipeServer;

namespace PrimalZed.CloudSync;
public class PipeServerWorker(
	IOptions<ProviderOptions> providerOptions,
	ILogger<PipeServerWorker> logger
) : BackgroundService {
	protected override async Task ExecuteAsync(CancellationToken stoppingToken) {
		using var pipeServer = new PipeServer(PipeNames.SYNC_ROOT_REGISTRAR, 3, logger);
		pipeServer.ReceivedMessage += PipeServer_ReceivedMessage;

		while (!stoppingToken.IsCancellationRequested) {
			await pipeServer.SendMessage("Hello from server");
			await Task.Delay(3000, stoppingToken);
		}
	}

	private void PipeServer_ReceivedMessage(object? sender, string message) {
		var bytes = Convert.FromBase64String(message);
		var request = StructBytes.FromBytes<RegisterSyncRootRequest>(bytes);
		logger.LogInformation("Received request {accountId} {directory}", request.AccountId, request.Directory);

		if (sender is null || sender is not PipeServerInstance pipeInstance) {
			throw new NotSupportedException("Unexpected type");
		}
		pipeInstance.RunAsClient(async () => {
			var id = $"{providerOptions.Value.ProviderId}!{WindowsIdentity.GetCurrent().User}!{request.AccountId}";
			var info = new StorageProviderSyncRootInfo {
				Id = id,
				Path = await StorageFolder.GetFolderFromPathAsync(request.Directory),
				DisplayNameResource = $"PrimalZed CloudSync - {request.AccountId}",
				IconResource = @"%SystemRoot%\system32\charmap.exe,0",
				HydrationPolicy = StorageProviderHydrationPolicy.Partial,
				HydrationPolicyModifier = StorageProviderHydrationPolicyModifier.AutoDehydrationAllowed,
				PopulationPolicy = StorageProviderPopulationPolicy.Full, // TODO: register options
				// InSyncPolicy = StorageProviderInSyncPolicy.Default, // StorageProviderInSyncPolicy.FileCreationTime | StorageProviderInSyncPolicy.DirectoryCreationTime;
				ShowSiblingsAsGroup = false,
				// TODO: Get version from package (but also don't crash on debug)
				Version = "1.0.0",
				// HardlinkPolicy = StorageProviderHardlinkPolicy.None,
				// RecycleBinUri = new Uri(""),
				Context = CryptographicBuffer.ConvertStringToBinary($"Local directory {request.Directory}", BinaryStringEncoding.Utf8),
			};
			// rootInfo.StorageProviderItemPropertyDefinitions.Add()

			logger.LogDebug("Registering {syncRootId}", id);
			StorageProviderSyncRootManager.Register(info);
		});
	}
}
