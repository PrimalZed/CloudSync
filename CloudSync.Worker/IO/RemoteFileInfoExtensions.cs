using PrimalZed.CloudSync.Helpers;
using PrimalZed.CloudSync.Remote.Abstractions;
using System.Diagnostics.CodeAnalysis;

namespace PrimalZed.CloudSync.IO;
public static class RemoteFileInfoExtensions {
	public static int GetHashCode([DisallowNull] this RemoteFileInfo obj) =>
		HashCode.Combine(
			obj.Length,
			// ignore sync attributes
			(int)obj.Attributes & ~SyncAttributes.ALL,
			obj.CreationTimeUtc,
			obj.LastWriteTimeUtc
		);
}
