using System.ComponentModel.DataAnnotations;

namespace PrimalZed.CloudSync.Remote.Local;
public record LocalRemoteOptions {
	[Required]
	public string Directory { get; set; } = string.Empty;
	[Required]
	public string AccountId { get; set; } = string.Empty;
	public bool EnableDeleteDirectoryWhenEmpty { get; set; } = true;
}
