
using Vanara.PInvoke;

namespace PrimalZed.CloudSync.CldApiExt;
public delegate void SyncRootCallback(in CldApi.CF_CALLBACK_INFO callbackInfo, in CldApi.CF_CALLBACK_PARAMETERS callbackParameters);
