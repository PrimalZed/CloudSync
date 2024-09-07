namespace PrimalZed.CloudSync.Remote.Abstractions;
public interface IRemoteReadWriteService : IRemoteReadService {
  Task CreateFile(string relativeFile);
  Task UpdateFile(string relativeFile);
  void MoveFile(string oldRelativeFile, string newRelativeFile);
  void DeleteFile(string relativeFile);
  Task CreateDirectory(string relativeDirectory);
  Task UpdateDirectory(string relativeDirectory);
  void MoveDirectory(string oldRelativeDirectory, string newRelativeDirectory);
  void DeleteDirectory(string relativeDirectory);
}
