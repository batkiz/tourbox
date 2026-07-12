using System.Collections.ObjectModel;
using System.IO;
using System.Windows;
using System.Windows.Input;
using kiwiprojekt.tourbox.ui.Models;
using kiwiprojekt.tourbox.ui.Services;

namespace kiwiprojekt.tourbox.ui.ViewModels;

public class MainViewModel : BindableBase
{
    private readonly TourBoxService _service;

    public DeviceState Device { get; } = new();

    public ObservableCollection<TourBoxEvent> EventLog { get; } = new();

    private const int MaxEventLogEntries = 200;

    private string _configPath = "";
    public string ConfigPath
    {
        get => _configPath;
        set => SetProperty(ref _configPath, value);
    }

    public ICommand ConnectCommand { get; }
    public ICommand DisconnectCommand { get; }
    public ICommand RefreshPortsCommand { get; }

    public MainViewModel(TourBoxService service)
    {
        _service = service;

        ConnectCommand = new RelayCommand(async _ => await ConnectAsync(), _ => Device.ConnectionState == ConnectionState.Disconnected);
        DisconnectCommand = new RelayCommand(_ => Disconnect(), _ => Device.ConnectionState == ConnectionState.Connected);
        RefreshPortsCommand = new RelayCommand(_ => RefreshPorts());

        _service.ConnectionChanged += (state, msg) =>
        {
            Device.ConnectionState = state;
            Device.StatusMessage = msg;
            CommandManager.InvalidateRequerySuggested();
        };

        _service.ButtonEvent += e =>
        {
            Device.LastEvent = e.ToString() ?? "";
            EventLog.Insert(0, e);
            while (EventLog.Count > MaxEventLogEntries)
                EventLog.RemoveAt(EventLog.Count - 1);
        };

        ConfigPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "appsettings.json");
        RefreshPorts();
    }

    private async Task ConnectAsync()
    {
        var port = Device.PortName;
        if (string.IsNullOrWhiteSpace(port))
        {
            System.Windows.MessageBox.Show("请先选择一个串口。", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        try
        {
            var config = AppConfig.Load(ConfigPath);
            await _service.ConnectAsync(port, config.RequiresInit);
        }
        catch (Exception ex)
        {
            System.Windows.MessageBox.Show($"连接失败:\n{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void Disconnect()
    {
        _service.Disconnect();
    }

    public void RefreshPorts()
    {
        Device.AvailablePorts = _service.GetAvailablePorts();
        if (Device.AvailablePorts.Length == 1 && string.IsNullOrWhiteSpace(Device.PortName))
        {
            Device.PortName = Device.AvailablePorts[0];
        }
    }
}

/// <summary>
/// Simple ICommand implementation for ViewModels.
/// </summary>
public class RelayCommand : ICommand
{
    private readonly Action<object?> _execute;
    private readonly Predicate<object?>? _canExecute;

    public RelayCommand(Action<object?> execute, Predicate<object?>? canExecute = null)
    {
        _execute = execute;
        _canExecute = canExecute;
    }

    public event EventHandler? CanExecuteChanged
    {
        add => CommandManager.RequerySuggested += value;
        remove => CommandManager.RequerySuggested -= value;
    }

    public bool CanExecute(object? parameter) => _canExecute?.Invoke(parameter) ?? true;
    public void Execute(object? parameter) => _execute(parameter);
}
