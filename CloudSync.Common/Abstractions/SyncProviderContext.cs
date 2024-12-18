﻿using PrimalZed.CloudSync.Commands;

namespace PrimalZed.CloudSync.Abstractions;
public record SyncProviderContext {
	public required string Id { get; init; }
	public required string RootDirectory { get; init; }
	public required PopulationPolicy PopulationPolicy { get; init; }
	public string AccountId => Id.Split('!', 3)[2];
	public string RemoteKind => AccountId.Split('!')[0];
}
