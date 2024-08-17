namespace PrimalZed.CloudSync.Async;
public static class CancellationTokenExtensions {
	public static CancellationTokenAwaiter GetAwaiter(this CancellationToken cancellationToken) =>
		new(cancellationToken);
}
