using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using PrimalZed.CloudSync.Configuration;
using PrimalZed.CloudSync.Helpers;
using PrimalZed.CloudSync.Remote.Local;
using System.Runtime.InteropServices;
using Vanara.InteropServices;
using Vanara.PInvoke;
using static Vanara.PInvoke.Gdi32;
using static Vanara.PInvoke.Shell32;

namespace PrimalZed.CloudSync.Shell.Local;
[ComVisible(true), Guid("703e61b4-f4a4-4803-b824-9d23dad651bc")]
public class LocalThumbnailProvider(
	IOptions<ClientOptions> clientOptions,
	IOptions<LocalRemoteOptions> localRemoteOptions,
	ILogger<LocalThumbnailProvider> logger
) : IThumbnailProvider, IInitializeWithItem {
	private IShellItem2? _clientItem, _serverItem;

	public HRESULT Initialize(IShellItem psi, STGM grfMode) {
		try {
			_clientItem = (IShellItem2)psi;
			// We want to identify the original item in the source folder that we're mirroring, based on the placeholder item that we
			// get initialized with. There's probably a way to do this based on the file identity blob but this just uses path manipulation.
			var clientPath = _clientItem.GetDisplayName(SIGDN.SIGDN_FILESYSPATH);
			if (!clientPath.StartsWith(clientOptions.Value.Directory)) {
				return HRESULT.E_UNEXPECTED;
			}

			var remotePath = PathMapper.ReplaceStart(clientPath, clientOptions.Value.Directory, localRemoteOptions.Value.Directory);
			_serverItem = SHCreateItemFromParsingName<IShellItem2>(remotePath);
		}
		catch (Exception ex) {
			return ex.HResult;
		}
		return HRESULT.S_OK;
	}

	public HRESULT GetThumbnail(uint cx, out SafeHBITMAP phbmp, out WTS_ALPHATYPE pdwAlpha) {
		logger.LogDebug("Get thumbnail for {path}", _serverItem!.GetDisplayName(SIGDN.SIGDN_FILESYSPATH));
		try {
			using var tps = ComReleaserFactory.Create(_serverItem!.BindToHandler<IThumbnailProvider>(default, BHID.BHID_ThumbnailHandler.Guid()));
			tps.Item.GetThumbnail(cx, out phbmp, out pdwAlpha).ThrowIfFailed();
		}
		catch (Exception ex) {
			logger.LogWarning(ex, "Failed to get thumbnail");
			phbmp = new SafeHBITMAP(IntPtr.Zero, false);
			pdwAlpha = WTS_ALPHATYPE.WTSAT_UNKNOWN;
			return ex.HResult;
		}
		return HRESULT.S_OK;
	}
}
