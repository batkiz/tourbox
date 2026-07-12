namespace kiwiprojekt.tourbox.ui.Models;

public enum ConnectionState
{
    Disconnected,
    Connecting,
    Connected,
    Error
}

public class DeviceState : BindableBase
{
    private ConnectionState _connectionState = ConnectionState.Disconnected;
    public ConnectionState ConnectionState
    {
        get => _connectionState;
        set => SetProperty(ref _connectionState, value);
    }

    private string _statusMessage = "未连接";
    public string StatusMessage
    {
        get => _statusMessage;
        set => SetProperty(ref _statusMessage, value);
    }

    private string _portName = "";
    public string PortName
    {
        get => _portName;
        set => SetProperty(ref _portName, value);
    }

    private string _lastEvent = "";
    public string LastEvent
    {
        get => _lastEvent;
        set => SetProperty(ref _lastEvent, value);
    }

    private string[] _availablePorts = [];
    public string[] AvailablePorts
    {
        get => _availablePorts;
        set => SetProperty(ref _availablePorts, value);
    }
}
