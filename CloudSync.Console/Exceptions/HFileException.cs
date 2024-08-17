namespace PrimalZed.CloudSync.Exceptions;
public class HFileException : Exception {
	public HFileException(string path) : base("Invalid hfile") {
		Data.Add(nameof(path), path);
	}
}
