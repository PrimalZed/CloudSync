using System.Runtime.InteropServices;

namespace PrimalZed.CloudSync.Remote.Local;
[StructLayout(LayoutKind.Sequential)]
public struct LocalContext {
	public bool EnableDeleteDirectoryWhenEmpty;
	[MarshalAs(UnmanagedType.ByValTStr, SizeConst = 50)]
	public string Directory;
}
