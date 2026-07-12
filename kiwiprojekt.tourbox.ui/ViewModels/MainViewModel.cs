using System.Collections.ObjectModel;
using System.IO;
using System.Windows;
using System.Windows.Input;
using kiwiprojekt.tourbox.ui.Models;
using kiwiprojekt.tourbox.ui.Services;
using kiwiprojekt.tourbox.ui.Views;

namespace kiwiprojekt.tourbox.ui.ViewModels;

public class MainViewModel : BindableBase
{
    private readonly TourBoxService _service;

    public DeviceState Device { get; } = new();

    public TourBoxVisualState Visual { get; } = new();

    /// <summary>
    /// Control name → action preview string for tooltips.
    /// </summary>
    public Dictionary<string, string> MappingPreviews { get; } = new(StringComparer.OrdinalIgnoreCase);

    public ObservableCollection<TourBoxEvent> EventLog { get; } = new();

    private const int MaxEventLogEntries = 200;

    private string _configPath = "";
    public string ConfigPath
    {
        get => _configPath;
        set => SetProperty(ref _configPath, value);
    }

    private AppConfig _appConfig = new();

    /// <summary>
    /// Control name -> human-readable label for dialogs.
    /// </summary>
    private static readonly Dictionary<string, string> ControlLabels = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Tall"] = "Tall (主按键)",
        ["Short"] = "Short",
        ["Top"] = "Top",
        ["Side"] = "Side",
        ["Up"] = "D-Pad ↑",
        ["Down"] = "D-Pad ↓",
        ["Left"] = "D-Pad ←",
        ["Right"] = "D-Pad →",
        ["C1"] = "C1",
        ["C2"] = "C2",
        ["Tour"] = "Tour",
        ["Knob"] = "Knob (旋钮)",
        ["Scroll"] = "Scroll (滚轮)",
        ["Dial"] = "Dial (拨盘)",
    };

    private static readonly HashSet<string> RotaryControls = new(StringComparer.OrdinalIgnoreCase)
    {
        "Knob", "Scroll", "Dial"
    };

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
            Visual.ApplyEvent(e);
            EventLog.Insert(0, e);
            while (EventLog.Count > MaxEventLogEntries)
                EventLog.RemoveAt(EventLog.Count - 1);
        };

        ConfigPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "appsettings.json");
        _appConfig = AppConfig.Load(ConfigPath);
        RefreshMappingPreviews();
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

    /// <summary>
    /// Open the mapping editor for a clicked control.
    /// </summary>
    public void EditControlMapping(string controlName)
    {
        var label = ControlLabels.GetValueOrDefault(controlName, controlName);

        if (RotaryControls.Contains(controlName))
        {
            EditRotaryMapping(controlName, label);
        }
        else
        {
            EditButtonMapping(controlName, label);
        }
    }

    private void EditButtonMapping(string key, string label)
    {
        _appConfig.Keys.TryGetValue(key, out var existing);
        var entry = MappingEntry.FromKeyBinding(existing);

        var dialog = new MappingEditorDialog(label, entry, showMode: true);
        dialog.Owner = System.Windows.Application.Current.MainWindow;

        if (dialog.ShowDialog() == true)
        {
            _appConfig.Keys[key] = dialog.Result.ToKeyBinding();
            _appConfig.Save(ConfigPath);
            RefreshMappingPreviews();
        }
    }

    private void EditRotaryMapping(string key, string label)
    {
        _appConfig.Rotary.TryGetValue(key, out var existing);
        var cwEntry = MappingEntry.FromKeyBinding(existing?.Clockwise);
        var ccwEntry = MappingEntry.FromKeyBinding(existing?.CounterClockwise);

        // Edit clockwise first
        var cwDialog = new MappingEditorDialog($"{label} — 顺时针 ▲", cwEntry, showMode: false);
        cwDialog.Owner = System.Windows.Application.Current.MainWindow;
        if (cwDialog.ShowDialog() != true) return;

        // Edit counterclockwise
        var ccwDialog = new MappingEditorDialog($"{label} — 逆时针 ▼", ccwEntry, showMode: false);
        ccwDialog.Owner = System.Windows.Application.Current.MainWindow;
        if (ccwDialog.ShowDialog() != true) return;

        _appConfig.Rotary[key] = new RotaryBinding
        {
            Clockwise = cwDialog.Result.ToKeyBinding(),
            CounterClockwise = ccwDialog.Result.ToKeyBinding()
        };
        _appConfig.Save(ConfigPath);
        RefreshMappingPreviews();
    }

    private void RefreshMappingPreviews()
    {
        MappingPreviews.Clear();

        // Single keys
        foreach (var (key, binding) in _appConfig.Keys)
        {
            var entry = MappingEntry.FromKeyBinding(binding);
            MappingPreviews[key] = entry.Preview;
        }

        // Rotary controls
        foreach (var (key, rotary) in _appConfig.Rotary)
        {
            var cw = MappingEntry.FromKeyBinding(rotary.Clockwise);
            var ccw = MappingEntry.FromKeyBinding(rotary.CounterClockwise);
            MappingPreviews[key] = $"▲{cw.Preview} ▼{ccw.Preview}";
        }

        OnPropertyChanged(nameof(MappingPreviews));
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
