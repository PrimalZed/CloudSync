namespace PrimalZed.CloudSync.Remote.Abstractions; 
public interface IRemoteContextSetter {
	void SetRemoteContext(byte[] contextBytes);
}
