namespace PrimalZed.CloudSync.Remote.Abstractions; 
public interface IRemoteContextSetter {
	string RemoteKind { get; }
	void SetRemoteContext(byte[] contextBytes);
}
