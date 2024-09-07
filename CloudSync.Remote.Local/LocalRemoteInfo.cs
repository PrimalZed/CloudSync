using PrimalZed.CloudSync.Abstractions;

namespace PrimalZed.CloudSync.Remote.Local;
public record LocalRemoteInfo : IRemoteInfo {
	public required string RemoteDirectory { get; init; }
	public string Identity => $"Local directory {RemoteDirectory}";
}
