using System.IO.Ports;
using System.Windows.Threading;
using kiwiprojekt.tourbox.ui.Models;

namespace kiwiprojekt.tourbox.ui.Services;

/// <summary>
/// Manages TourBox device connection lifecycle and event dispatching.
/// Wraps TourBoxHandler and marshals events to the UI thread.
/// </summary>
public class TourBoxService : IDisposable
{
    private readonly Dispatcher _dispatcher;
    private TourBoxHandler? _handler;
    private CancellationTokenSource? _watcherCts;
    private bool _disposed;

    public event Action<ConnectionState, string>? ConnectionChanged;
    public event Action<TourBoxEvent>? ButtonEvent;

    public ConnectionState State { get; private set; } = ConnectionState.Disconnected;
    public string? CurrentPort { get; private set; }

    public TourBoxService(Dispatcher dispatcher)
    {
        _dispatcher = dispatcher;
    }

    public string[] GetAvailablePorts()
    {
        try
        {
            return SerialPort.GetPortNames()
                .OrderBy(p => p, StringComparer.OrdinalIgnoreCase)
                .ToArray();
        }
        catch
        {
            return [];
        }
    }

    public async Task ConnectAsync(string portName, bool requiresInit = true, CancellationToken ct = default)
    {
        Disconnect();

        SetState(ConnectionState.Connecting, $"正在连接 {portName}...");

        try
        {
            _handler = new TourBoxHandler(portName, OnTourBoxEvent, requiresInit);
            CurrentPort = portName;
            SetState(ConnectionState.Connected, $"已连接 ({portName})");
        }
        catch (Exception ex)
        {
            SetState(ConnectionState.Error, $"连接失败: {ex.Message}");
            throw;
        }
    }

    public void Disconnect()
    {
        _watcherCts?.Cancel();
        _watcherCts?.Dispose();
        _watcherCts = null;

        _handler?.Dispose();
        _handler = null;
        CurrentPort = null;
        SetState(ConnectionState.Disconnected, "未连接");
    }

    private void OnTourBoxEvent(TourBoxEvent e)
    {
        // Marshal to UI thread
        _dispatcher.BeginInvoke(() =>
        {
            ButtonEvent?.Invoke(e);
        });
    }

    private void SetState(ConnectionState state, string message)
    {
        State = state;
        _dispatcher.BeginInvoke(() =>
        {
            ConnectionChanged?.Invoke(state, message);
        });
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        _watcherCts?.Cancel();
        _watcherCts?.Dispose();
        _handler?.Dispose();
    }
}
