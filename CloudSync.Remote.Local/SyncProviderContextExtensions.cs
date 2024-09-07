using PrimalZed.CloudSync.Abstractions;

namespace PrimalZed.CloudSync.Remote.Local; 
internal static class SyncProviderContextExtensions {
	public static string GetRemoteDirectory(this SyncProviderContext context) =>
		context.AccountId.Split('!')[1].Replace('|', '\\');
}
