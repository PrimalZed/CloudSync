using System.Runtime.InteropServices;
using Vanara.PInvoke;

namespace PrimalZed.CloudSync.CldApiExt;
public static class HResultExtensions {
	public static void MyThrowIfFailed(this HRESULT hresult, string? message = null) {
		Exception? exception = MyGetException(hresult, message);
		if (exception is not null) {
				throw exception;
		}
	}

	public static Exception? MyGetException(this HRESULT hresult, string? message = null) {
		if (!hresult.Failed) {
			return null;
		}

		Exception? ex = Marshal.GetExceptionForHR((int)hresult, new IntPtr(-1));
		if (ex is null) {
			return null;
		}
		if (ex.GetType() == typeof(COMException)) {
			if (hresult.Facility != HRESULT.FacilityCode.FACILITY_WIN32) {
				return new COMException(message ?? ex.Message, (int)hresult);
			}

			if (!string.IsNullOrEmpty(message)) {
				return new System.ComponentModel.Win32Exception(hresult.Code, message);
			}

			return new System.ComponentModel.Win32Exception(hresult.Code);
		}

		if (!string.IsNullOrEmpty(message)) {
			var constructor = ex?.GetType().GetConstructor([typeof(string)])!;
			ex = constructor.Invoke([message!]) as Exception;
			ex!.HResult = (int)hresult;
		}

		return ex;
	}
}
