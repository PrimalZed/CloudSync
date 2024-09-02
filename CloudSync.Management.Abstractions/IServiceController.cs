namespace PrimalZed.CloudSync.Management.Abstractions;
public interface IServiceController {
  Task Stop();
  Task Restart();
  Task Start();
}
