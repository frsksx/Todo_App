using System;
using System.IO;
using System.IO.Pipes;
using System.Threading;
using System.Threading.Tasks;

namespace WindowsTrayTasks.Shell;

public sealed class SingleInstance : IDisposable
{
    private const string MutexName = "Global\\WindowsTrayTasks.SingleInstance";
    private const string PipeName = "WindowsTrayTasks.Pipe";

    private readonly Mutex _mutex;
    private readonly bool _owned;
    private CancellationTokenSource? _cts;

    public bool IsFirstInstance => _owned;

    public event Action<string>? MessageReceived;

    public SingleInstance()
    {
        _mutex = new Mutex(initiallyOwned: true, MutexName, out _owned);
    }

    public static void SendMessage(string message)
    {
        try
        {
            using var client = new NamedPipeClientStream(".", PipeName, PipeDirection.Out);
            client.Connect(500);
            using var writer = new StreamWriter(client) { AutoFlush = true };
            writer.WriteLine(message);
        }
        catch
        {
            // best-effort; if the existing instance is unresponsive, do nothing
        }
    }

    public void StartListener()
    {
        if (!_owned) return;
        _cts = new CancellationTokenSource();
        _ = Task.Run(() => ListenLoop(_cts.Token));
    }

    private async Task ListenLoop(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            try
            {
                using var server = new NamedPipeServerStream(PipeName, PipeDirection.In, 1, PipeTransmissionMode.Byte, PipeOptions.Asynchronous);
                await server.WaitForConnectionAsync(ct).ConfigureAwait(false);
                using var reader = new StreamReader(server);
                var line = await reader.ReadLineAsync(ct).ConfigureAwait(false);
                if (line is not null) MessageReceived?.Invoke(line);
            }
            catch (OperationCanceledException) { return; }
            catch
            {
                await Task.Delay(200, ct).ConfigureAwait(false);
            }
        }
    }

    public void Dispose()
    {
        _cts?.Cancel();
        if (_owned)
        {
            try { _mutex.ReleaseMutex(); } catch { }
        }
        _mutex.Dispose();
    }
}
