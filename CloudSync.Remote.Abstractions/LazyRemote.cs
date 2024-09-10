﻿namespace PrimalZed.CloudSync.Remote.Abstractions; 
public class LazyRemote<T>(Func<T> valueFactory, string remoteKind) : Lazy<T>(valueFactory) {
	public string RemoteKind => remoteKind;
}
