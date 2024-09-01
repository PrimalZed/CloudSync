using CloudSync.App;
using Microsoft.Extensions.Configuration;
using PrimalZed.CloudSync.Configuration;
using PrimalZed.CloudSync.Management;
using PrimalZed.CloudSync.Management.Abstractions;
using PrimalZed.CloudSync.Remote.Abstractions;
using PrimalZed.CloudSync.Remote.Local;

namespace PrimalZed.CloudSync.Package;
public static class MauiProgram
{
  public static MauiApp CreateMauiApp()
  {
    var builder = MauiApp.CreateBuilder();
    builder.Services
      .AddOptionsWithValidateOnStart<ProviderOptions>()
      .Configure<IConfiguration>((options, config) =>
      {
        options.ProviderId = "PrimalZed:CloudSync";
      })
      .Services
      .AddOptionsWithValidateOnStart<ClientOptions>()
      .Configure<IConfiguration>((options, config) =>
      {
        options.Directory = @"C:\SyncTestClient";
      })
      .Services
      .AddOptionsWithValidateOnStart<LocalRemoteOptions>()
      .Configure((options) =>
      {
        options.AccountId = "TestAccount1";
        options.Directory = @"C:\SyncTestServer";
      })
      .Services
      .AddSingleton<IRemoteInfo, LocalRemoteInfo>()
      .AddSingleton<ISyncRootRegistrar, SyncRootRegistrar>();

    builder
      .UseSharedMauiApp();

    return builder.Build();
  }
}
