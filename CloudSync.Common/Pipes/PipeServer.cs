using Microsoft.Extensions.Logging;
using PrimalZed.CloudSync.Abstractions;
using System.IO.Pipes;
using System.Security.AccessControl;

namespace PrimalZed.CloudSync.Pipes;
public sealed class PipeServer : IPipe, IDisposable {
	private readonly PipeServerInstanceFactory _factory;
	private readonly List<PipeServerInstance> _instances = [];
	private readonly ILogger _logger;
	public bool IsConnected => true;
	public event EventHandler<string>? ReceivedMessage;

	public PipeServer(string name, int maxInstances, ILogger logger) {
		_logger = logger;
		_factory = new(name, maxInstances);
		_factory.Connected += OnConnected;
		Task.Run(_factory.Start);
	}

	private void OnConnected(object? sender, NamedPipeServerStream pipeStream) {
		var instance = new PipeServerInstance(pipeStream, _logger);
		instance.ReceivedMessage += (object? sender, string message) => {
			ReceivedMessage?.Invoke(sender, message);
		};
		_instances.Add(instance);
		CleanInstances();
	}

	private void CleanInstances() {
		foreach (var instance in _instances) {
			if (instance.IsConnected) {
				continue;
			}
			instance.Dispose();
		}
		_instances.RemoveAll((instance) => !instance.IsConnected);
	}

	public async Task SendMessage(byte[] bytes, CancellationToken cancellation = default) {
		var base64 = Convert.ToBase64String(bytes);
		await SendMessage(base64, cancellation);
	}

	public async Task SendMessage(string message, CancellationToken cancellation = default) {
		CleanInstances();
		var sendMessageTasks = _instances
			.Select(async (instance) => {
				try {
					await instance.SendMessage(message, cancellation);
				}
				catch (IOException ex) when (ex.Message == "Pipe is broken.") {
					_logger.LogDebug(ex, "Pipe disconnected");
				}
			})
			.ToArray();
		await Task.WhenAll(sendMessageTasks);
	}

	public void Dispose() {
		_factory.Dispose();
		foreach (var instance in _instances) {
			instance.Dispose();
		}
	}

	public sealed class PipeServerInstanceFactory : IDisposable {
		public string Name { get; init; }
		public int MaxInstances { get; init; }
		private NamedPipeServerStream? _currentWaitingPipe;
		private bool _disposed = false;
		public event EventHandler<NamedPipeServerStream>? Connected;

		public PipeServerInstanceFactory(string name, int maxInstances) {
			Name = name;
			MaxInstances = maxInstances;
		}

		public async Task Start() {
			while (!_disposed) {
				var pipeSecurity = new PipeSecurity();
				pipeSecurity.AddAccessRule(
					new PipeAccessRule(
						"Everyone",
						PipeAccessRights.ReadWrite | PipeAccessRights.CreateNewInstance,
						AccessControlType.Allow
					)
				);
				_currentWaitingPipe = NamedPipeServerStreamAcl.Create(Name, PipeDirection.InOut, MaxInstances, PipeTransmissionMode.Byte, PipeOptions.Asynchronous, 0, 0, pipeSecurity);
				await _currentWaitingPipe.WaitForConnectionAsync();
				Connected?.Invoke(this, _currentWaitingPipe);
			}
		}

		public void Dispose() {
			_currentWaitingPipe?.Dispose();
			_disposed = true;
		}
	}

	public sealed class PipeServerInstance : IDisposable {
		private readonly CancellationTokenSource _cancellationTokenSource = new();
		private readonly NamedPipeServerStream _pipeStream;
		private readonly StreamReader _reader;
		private readonly StreamWriter _writer;
		private readonly ILogger _logger;
		public NamedPipeServerStream PipeStream => _pipeStream;
		public bool IsConnected => _pipeStream.IsConnected;
		public event EventHandler<string>? ReceivedMessage;

		public PipeServerInstance(
			NamedPipeServerStream pipeStream,
			ILogger logger
		) {
			_pipeStream = pipeStream;
			_logger = logger;
			_reader = new StreamReader(pipeStream, leaveOpen: true);
			_writer = new StreamWriter(pipeStream, leaveOpen: true) { AutoFlush = true };
			Task.Run(Listen);
		}

		private async Task Listen() {
			while (!_reader.EndOfStream && !_cancellationTokenSource.IsCancellationRequested) {
				var line = await _reader.ReadLineAsync(_cancellationTokenSource.Token);
				if (line is null) {
					continue;
				}
				ReceivedMessage?.Invoke(this, line);
			}
		}

		public async Task SendMessage(string message, CancellationToken cancellation = default) {
			if (_cancellationTokenSource.IsCancellationRequested) {
				return;
			}
			await _writer.WriteLineAsync(message.AsMemory(), cancellation);
		}

		public void RunAsClient(PipeStreamImpersonationWorker impersonationWorker) {
			_pipeStream.RunAsClient(impersonationWorker);
		}

		public void Dispose() {
			_cancellationTokenSource.Cancel();
			_cancellationTokenSource.Dispose();
			try {
				_writer.Dispose();
				_reader.Dispose();
			}
			catch (IOException ex) when (ex.Message == "Pipe is broken.") {
				_logger.LogDebug(ex, "Pipe disconnected");
			}
			_pipeStream.Dispose();
		}
	}
}
