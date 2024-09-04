using PrimalZed.CloudSync.Abstractions;
using System.IO.Pipes;

namespace PrimalZed.CloudSync.Pipes;
public sealed class PipeClient : IPipe, IDisposable {
	private readonly CancellationTokenSource _cancellationTokenSource = new();
	public string Name { get; init; }
	private readonly NamedPipeClientStream _pipeStream;
	private StreamReader? _reader;
	private StreamWriter? _writer;
	public bool IsConnected => _pipeStream.IsConnected;

	public event EventHandler? Connected;
	public event EventHandler<string>? ReceivedMessage;

	public PipeClient(string name) {
		Name = name;
		_pipeStream = new(".", name, PipeDirection.InOut, PipeOptions.Asynchronous & ~PipeOptions.CurrentUserOnly);
		Task.Run(Connect);
	}

	private async Task Connect() {
		await _pipeStream.ConnectAsync(_cancellationTokenSource.Token);
		if (_cancellationTokenSource.IsCancellationRequested) {
			return;
		}
		_reader = new StreamReader(_pipeStream, leaveOpen: true);
		_writer = new StreamWriter(_pipeStream, leaveOpen: true) { AutoFlush = true };
		Connected?.Invoke(this, EventArgs.Empty);
		await Listen();
	}

	private async Task Listen() {
		while (!_reader!.EndOfStream && !_cancellationTokenSource.IsCancellationRequested) {
			var line = await _reader.ReadLineAsync(_cancellationTokenSource.Token);
			if (line is null) {
				continue;
			}
			ReceivedMessage?.Invoke(this, line);
		}
	}

	public async Task SendMessage(byte[] bytes, CancellationToken cancellation = default) {
		var base64 = Convert.ToBase64String(bytes);
		await SendMessage(base64, cancellation);
	}

	public async Task SendMessage(string message, CancellationToken cancellation = default) {
		await (_writer?.WriteLineAsync(message.AsMemory(), cancellation) ?? Task.CompletedTask);
	}

	public void Dispose() {
		_cancellationTokenSource.Cancel();
		_cancellationTokenSource.Dispose();
		_reader?.Dispose();
		_writer?.Dispose();
		_pipeStream.Dispose();
	}
}
