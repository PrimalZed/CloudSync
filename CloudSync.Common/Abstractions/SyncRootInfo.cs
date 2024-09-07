namespace PrimalZed.CloudSync.Abstractions; 
public record SyncRootInfo {
	public required string Id { get; init; }
	public required string Directory { get; init; }
	public string Label => $"{Id} - {Directory}";
}
