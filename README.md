# Cloud Sync in C#

## Windows APIs
* [Cloud Sync Engines](https://learn.microsoft.com/en-us/windows/win32/cfapi/cloud-files-api-portal)
  * [Windows.Storage.Provider](https://learn.microsoft.com/en-us/uwp/api/windows.storage.provider?view=winrt-26100)
    * Registers Sync Root (and some shell extensions)
    * [More registry info](https://learn.microsoft.com/en-us/windows/win32/shell/integrate-cloud-storage)
  * [Cloud Filter API](https://learn.microsoft.com/en-us/windows/win32/api/_cloudapi/)
    * Used in sync provider implementation

## Windows C++ from C#
* .csproj Target OS
* [extern keyword](https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/keywords/extern)
* [P/Invoke](https://learn.microsoft.com/en-us/dotnet/standard/native-interop/pinvoke)
* [COM Interop](https://learn.microsoft.com/en-us/dotnet/standard/native-interop/cominterop)
* [Vanara](https://github.com/dahall/Vanara)

Other options?
* [Source generation](https://learn.microsoft.com/en-us/dotnet/standard/native-interop/comwrappers-source-generation)
* [CsWin32](https://github.com/microsoft/CsWin32)

## Samples/References
* [windows-classic-samples (c++)](https://github.com/microsoft/Windows-classic-samples/tree/main/Samples/CloudMirror)
  * A sample using a local directory as the "cloud"
  * Isn't actually a full implementation: does not move updates between "client" and "server" directories
* [C# with Vanara](https://github.com/dahall/WinClassicSamplesCS/tree/nullableenabled/CloudMirror)
  * a C# version of the windows-classic-samples CloudMirror using Vanara.PInvoke
  * Doesn't include IStorageProviderStatusUISource
* [IT HIT UserFileSystem](https://www.userfilesystem.com/programming/)
  * A product that abstracts Cloud Sync Engine
  * Helpful documentation about some of the Cloud Sync concepts
  * [UserFileSystem sample](https://github.com/ITHit/UserFileSystemSamples/blob/master/Windows/VirtualDrive)

## Architecture
* [BackgroundService](./CloudSync.Console/SyncBackgroundService.cs)
  * Sets client directory as search-indexable
  * Registers class objects for COM
  * Registers sync root at client directory
  * Connects to sync root events (e.g. fetch, rename, delete)
  * Starts watch for changes in client directory (to hydrate/dehydrate on attribute change, and to update remote)
  * Starts watch for changes in remote directory (to update client)
  * Wait for SIGTERM (ctrl+c)
  * Dispose/unregister watchers, sync root events, sync root, COM class objects
* Updating remote: [IRemoteReadWriteService](./CloudSync.Remote.Abstractions/IRemoteReadWriteService.cs)
  * Local implementation: [LocalRemoteReadWriteService](./CloudSync.Remote.Local/LocalRemoteReadWriteService.cs)
* Updating client: [PlaceholderService](./CloudSync.Console/PlaceholdersService.cs)

### Helpers
* [Disposable](./CloudSync.Common/Helpers/Disposable.cs) / [Disposable\<T>](./CloudSync.Common/Helpers/DisposableOf.cs)
  * Do action on dispose without needing to implement a new `IDisoposable` class
* [CancellationTokenAwaiter](./CloudSync.Common/Async/CancellationTokenAwaiter.cs)
  * Support for `await cancellationToken;`

## Shell Extensions and MSIX
* Cloud Files API supports further shell extensions such as custom states, context menu commands, thumbnail provider, quota (preview)
* Shouldn't use [managed code](https://learn.microsoft.com/en-us/dotnet/standard/managed-code) (C#)?
  * [2013: Do not use managed code](https://devblogs.microsoft.com/oldnewthing/?p=5163)
  * [Microsoft recommends against writing managed in-process extensions](https://learn.microsoft.com/en-us/windows/win32/shell/shell-and-managed-code)
    * Not entirely sure what the distinction is between "in-process" and "out-of-process"
  * Seems to technically work, even without Single-Threaded Apartment (STA) and CoInitializeEx
    * Except [haven't been able to get ThumbnailProvider to work](https://github.com/dahall/WinClassicSamplesCS/issues/6)
  * Can register with DI-generated factories!
* Implementing an interface: ComVisible, Guid
* [CoRegisterClassObject](./CloudSync.Shell/ShellRegistrar.cs) and [ClassFactory](./CloudSync.Shell/ClassFactory.cs)
* [Package your app as an MSIX](https://learn.microsoft.com/en-us/windows/msix/desktop/vs-package-overview)
  * [Package.appxmanifest](./CloudSync.Package/Package.appxmanifest)
    * [CloudFiles extensions](https://learn.microsoft.com/en-us/uwp/schemas/appxpackage/uapmanifestschema/element-cloudfiles-cloudfiles)
    * "null" Clsid `20000000-0000-0000-0000-000000000001`
    * Held back for a long time: apparently having empty `DesktopIconOverlayHandlers` broke it
    * [IStorageProviderStatusUISource is in preview](https://learn.microsoft.com/en-us/uwp/api/windows.storage.provider.istorageproviderstatusuisource#windows-requirements)
  * Should use package project as startup project for local debug
* [More shell notes](./CloudSync.Shell/README.md)

# TODO
[x] Allow SyncRoot PopulationPolicy Full (on-demand placeholder creation)
[x] Anticipate file conflict. Is it ok to resolve with newer LastWrittenTime?
  etag (hash) would be better, but would need to maintain state
[] Queue changes until closed
[] Convert to https://github.com/microsoft/CsWin32?

## SyncProvider / ClientWatcher
[] Cancel FetchData/FetchPlaceholders
[x] Create events DTO for ConnectSyncRoot
[x] New client file -> cloud storage provider
  [x] Convert to placeholder
  [x] Mark as in-sync
  [x] Don't dehydrate client
[x] Update - FileSystemWatcher Changed is very chatty, and no way to really check what changed on the file
  Compare LastWritten to server?
[x] Rename
[x] Delete
[] Combine SyncProvider and ClientWatcher?

## Local Server
[x] New server file -> New placeholder
  [x] Don't hydrate client
[x] Update server file -> ?
  [x] Rehydrate downloaded file
  [x] Update Pinned file without unpinning it
  [x] Update offline file (for some reason causing a Fetch that throws a 380 error)
  [] Update downloaded file when can't obtain lock (clear in sync, separate interval process to attempt sync)
[x] Rename server file -> ?
[x] Delete server file -> ?
[] Changes (create, update, rename, delete) while offline -> ?

# Add/Remove sync root through app
[] Install
  [] Register sync root
  [] Bulk load
  [] Start service (background process or windows service?)
  [] Service to sync changes since last time service ran (offline, stopped)
[] Uninstall
  [] Remove service
  [] Unregister sync root
