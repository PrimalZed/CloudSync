namespace PrimalZed.CloudSync.Management.Abstractions; 
public interface ISyncRootRegistrar {
	Task RegisterAsync();
	Task<bool> IsRegistered();
	Task Unregister();
}
