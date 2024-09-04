using System.Runtime.InteropServices;

namespace PrimalZed.CloudSync.Management.Abstractions;
[StructLayout(LayoutKind.Sequential)]
public struct RegisterSyncRootRequest {
	[MarshalAs(UnmanagedType.ByValTStr, SizeConst = 50)]
	public string AccountId;
	[MarshalAs(UnmanagedType.ByValTStr, SizeConst = 500)]
	public string Directory;
}
