namespace PrimalZed.CloudSync.Remote.Abstractions;
public interface IRemoteReadWriteService : IRemoteReadService {
  Task CreateFile(string clientFile);
  Task UpdateFile(string clientFile);
  void MoveFile(string oldClientFile, string newClientFile);
  void DeleteFile(string clientFile);
  Task CreateDirectory(string clientDirectory);
  Task UpdateDirectory(string clientDirectory);
  void MoveDirectory(string oldClientDirectory, string newClientDirectory);
  void DeleteDirectory(string clientDirectory);
}
