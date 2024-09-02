namespace PrimalZed.CloudSync.Management.Abstractions; 
public interface ISyncRootRegistrar {
	string Id { get; }
	Task RegisterAsync();
	Task<bool> IsRegistered();
	Task Unregister();
}
