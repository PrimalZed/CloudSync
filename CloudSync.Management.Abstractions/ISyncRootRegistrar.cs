namespace PrimalZed.CloudSync.Management.Abstractions; 
public interface ISyncRootRegistrar {
	string Id { get; }
	Task RegisterAsync();
	bool IsRegistered();
	void Unregister();
}
