using PrimalZed.CloudSync.Interop;

namespace PrimalZed.CloudSync.Commands;
public static class SyncRootCommand {
	public static string ToMessage<T>(T command) where T : struct {
		var kind = command switch {
			RegisterSyncRootCommand => SyncRootCommandKind.Register,
			UnregisterSyncRootCommand => SyncRootCommandKind.Unregister,
			_ => throw new NotSupportedException($"Unexpected type {typeof(T)}"),
		};
		var bytes = StructBytes.ToBytes(command);
		var base64 = Convert.ToBase64String(bytes);
		return $"{kind}|{base64}";
	}

	public static object FromMessage(string message) {
		var messageParts = message.Split('|', 2);
		var type = Enum.Parse<SyncRootCommandKind>(messageParts[0]);
		var data = messageParts[1];
		var bytes = Convert.FromBase64String(data);
		return type switch {
			SyncRootCommandKind.Register => StructBytes.FromBytes<RegisterSyncRootCommand>(bytes),
			SyncRootCommandKind.Unregister => StructBytes.FromBytes<UnregisterSyncRootCommand>(bytes),
			_ => throw new NotSupportedException($"Unexpected kind"),
		};
	}

	internal enum SyncRootCommandKind {
		Register = 1,
		Unregister,
	}
}
