using System.Runtime.InteropServices;

namespace PrimalZed.CloudSync.Remote.Sftp;
[StructLayout(LayoutKind.Sequential)]
public struct SftpContext {
	[MarshalAs(UnmanagedType.ByValTStr, SizeConst = 50)]
	public string Directory;
	[MarshalAs(UnmanagedType.ByValTStr, SizeConst = 50)]
	public string Host;
	public int Port;
	[MarshalAs(UnmanagedType.ByValTStr, SizeConst = 50)]
	public string Username;
	[MarshalAs(UnmanagedType.ByValTStr, SizeConst = 50)]
	public string Password;
}
