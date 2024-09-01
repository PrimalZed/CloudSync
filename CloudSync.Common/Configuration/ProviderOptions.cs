using System.ComponentModel.DataAnnotations;

namespace PrimalZed.CloudSync.Configuration;
public record ProviderOptions {
	[Required]
	public string ProviderId { get; set; } = string.Empty;
}
