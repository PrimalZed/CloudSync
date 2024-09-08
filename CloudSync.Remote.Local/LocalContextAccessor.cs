namespace PrimalZed.CloudSync.Remote.Local;

public interface ILocalContextAccessor {
	LocalContext Context { get; }
}

public class LocalContextAccessor : ILocalContextAccessor {
	private static readonly AsyncLocal<ContextHolder> _localContextCurrent = new();

	/// <inheritdoc/>
	public LocalContext Context {
		get => _localContextCurrent.Value?.Context! ?? throw new NullReferenceException();
		set {
			var holder = _localContextCurrent.Value;
			if (holder != null) {
				// Clear current SyncProviderContext trapped in the AsyncLocals, as its done.
				holder.Context = null;
			}

			// Use an object indirection to hold the SyncProviderContext in the AsyncLocal,
			// so it can be cleared in all ExecutionContexts when its cleared.
			_localContextCurrent.Value = new ContextHolder { Context = value };
		}
	}

	private sealed class ContextHolder {
		public LocalContext? Context;
	}
}
