namespace PrimalZed.CloudSync.Remote.Abstractions;
public interface IRemoteReadWriteService : IRemoteReadService {
  Task CreateFile(string rootDirectory, string clientFile);
  Task UpdateFile(string rootDirectory, string clientFile);
  void MoveFile(string oldRelativeFile, string newRelativeFile);
  void DeleteFile(string relativeFile);
  Task CreateDirectory(string rootDirectory, string clientDirectory);
  Task UpdateDirectory(string rootDirectory, string clientDirectory);
  void MoveDirectory(string oldRelativeDirectory, string newRelativeDirectory);
  void DeleteDirectory(string relativeDirectory);
}
