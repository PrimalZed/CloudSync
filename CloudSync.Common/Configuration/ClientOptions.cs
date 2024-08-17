using System.ComponentModel.DataAnnotations;

namespace PrimalZed.CloudSync.Configuration;
public record ClientOptions {
	[Required]
	public string Directory { get; set; } = string.Empty;
	public PopulationPolicy PopulationPolicy { get; set; } = PopulationPolicy.Full;
}

public enum PopulationPolicy {
	Full = 1,
	AlwaysFull
}
