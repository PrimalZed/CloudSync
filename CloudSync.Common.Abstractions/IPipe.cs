namespace PrimalZed.CloudSync.Abstractions; 
public interface IPipe {
	bool IsConnected { get; }
	event EventHandler<string>? ReceivedMessage;
	Task SendMessage(byte[] bytes, CancellationToken cancellation = default);
	Task SendMessage(string message, CancellationToken cancellation = default);
}
