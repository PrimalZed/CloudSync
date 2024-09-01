using PrimalZed.CloudSync.Helpers;
using PrimalZed.CloudSync.Remote.Abstractions;
using System.Diagnostics.CodeAnalysis;

namespace PrimalZed.CloudSync.IO;
public static class RemoteDirectoryInfoExtensions {
	public static int GetHashCode([DisallowNull] this RemoteDirectoryInfo obj) =>
		HashCode.Combine(
			// ignore sync attributes
			(int)obj.Attributes & ~SyncAttributes.ALL,
			obj.CreationTimeUtc,
			obj.LastWriteTimeUtc
		);
}
