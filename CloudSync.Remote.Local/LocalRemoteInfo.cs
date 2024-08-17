using Microsoft.Extensions.Options;
using PrimalZed.CloudSync.Remote.Abstractions;

namespace PrimalZed.CloudSync.Remote.Local;
public class LocalRemoteInfo(IOptions<LocalRemoteOptions> options) : IRemoteInfo {
	public string AccountId => options.Value.AccountId;
	public string Identity => $"Local directory {options.Value.Directory}";
}
