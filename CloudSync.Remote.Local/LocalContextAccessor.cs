using PrimalZed.CloudSync.Interop;
using PrimalZed.CloudSync.Remote.Abstractions;

namespace PrimalZed.CloudSync.Remote.Local;
public interface ILocalContextAccessor {
	LocalContext Context { get; }
}

public class LocalContextAccessor : IRemoteContextSetter, ILocalContextAccessor {
	private static readonly AsyncLocal<ContextHolder> _localContextCurrent = new();

	/// <inheritdoc/>
	public LocalContext Context {
		get => _localContextCurrent.Value?.Context! ?? throw new NullReferenceException();
		set {
			var holder = _localContextCurrent.Value;
			if (holder != null) {
				// Clear current LocalContext trapped in the AsyncLocals, as its done.
				holder.Context = null;
			}

			// Use an object indirection to hold the LocalContext in the AsyncLocal,
			// so it can be cleared in all ExecutionContexts when its cleared.
			_localContextCurrent.Value = new ContextHolder { Context = value };
		}
	}

	public void SetRemoteContext(byte[] contextBytes) {
		Context = StructBytes.FromBytes<LocalContext>(contextBytes);
	}

	private sealed class ContextHolder {
		public LocalContext? Context;
	}
}
