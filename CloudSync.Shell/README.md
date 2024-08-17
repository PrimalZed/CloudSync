# References
https://learn.microsoft.com/en-us/windows/win32/shell/integrate-cloud-storage
Microsoft classic sample c++ https://github.com/microsoft/Windows-classic-samples/blob/main/Samples/CloudMirror/CloudMirrorPackage/Package.appxmanifest
Vanara version of above classic sample https://github.com/dahall/WinClassicSamplesCS/blob/nullableenabled/CloudMirrorPackage/Package.appxmanifest
2009 example (archived, MTAThread, deprecated RegisterTypeForComClients): https://web.archive.org/web/20160704192503/http://www.andymcm.com/blog/2009/10/managed-dcom-server.html
C# POC from 2020: https://github.com/MichaeIDietrich/UwpNotificationNetCoreTest/blob/master/UwpNotificationNetCoreTest/Registration.cs

# TODO
[] Windows Foundation Initialize? (suggested by CoInitializeEx docs)
[] Local Thumbnail Provider
  * Works in C++ sample, but not in Vanara C# sample
  * https://github.com/dahall/WinClassicSamplesCS/issues/6
