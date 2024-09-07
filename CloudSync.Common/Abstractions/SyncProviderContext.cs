using PrimalZed.CloudSync.Commands;

namespace PrimalZed.CloudSync.Abstractions;
public record SyncProviderContext {
	public required string Id { get; init; }
	public required string RootDirectory { get; init; }
	public required PopulationPolicy PopulationPolicy { get; init; }
	public string AccountId => Id.Split('!', 3)[2];
	public RemoteKind RemoteKind => Enum.Parse<RemoteKind>(AccountId.Split('!')[0]);
}

public enum RemoteKind {
	Local = 1,
}
