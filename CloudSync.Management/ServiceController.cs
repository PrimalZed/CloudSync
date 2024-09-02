using Microsoft.Extensions.Logging;
using PrimalZed.CloudSync.Management.Abstractions;
using System.ServiceProcess;

namespace PrimalZed.CloudSync.Management; 
public class ServiceController(ILogger<ServiceController> logger) : IServiceController {
  private readonly System.ServiceProcess.ServiceController _base = new System.ServiceProcess.ServiceController("CloudSync");

  public Task Stop() {
    //logger.LogDebug("Stopping service");
    //if (_base.Status != ServiceControllerStatus.Stopped) {
    //  _base.Stop();
    //  _base.WaitForStatus(ServiceControllerStatus.Stopped);
    //}
    return Task.CompletedTask;
  }

  public Task Start() {
    //logger.LogDebug("Starting service");
    //if (_base.Status != ServiceControllerStatus.Running) {
    //  _base.Start();
    //  _base.WaitForStatus(ServiceControllerStatus.Running);
    //}
    return Task.CompletedTask;
  }

  public async Task Restart() {
    await Stop();
    await Start();
  }
}
