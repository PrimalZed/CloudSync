using System.Runtime.InteropServices;

namespace PrimalZed.CloudSync.Commands;
[StructLayout(LayoutKind.Sequential)]
public struct RegisterSyncRootCommand {
  [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 50)]
  public string AccountId;
  [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 500)]
  public string Directory;
  public PopulationPolicy PopulationPolicy;
}

public enum PopulationPolicy {
  Full = 1,
  AlwaysFull,
}