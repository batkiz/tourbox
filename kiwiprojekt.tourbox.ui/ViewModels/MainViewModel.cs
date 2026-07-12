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
    private readonly InputService _input;

    public DeviceState Device { get; } = new();

    public TourBoxVisualState Visual { get; } = new();

    /// <summary>
    /// Control name → action preview string for tooltips.
    /// </summary>
    public Dictionary<string, string> MappingPreviews { get; } = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// All current mappings as a flat list for display.
    /// </summary>
    public ObservableCollection<MappingRow> MappingRows { get; } = new();

    public EditorViewModel Editor { get; } = new();

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
        _input = new InputService(service);

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

        ConfigPath = GetDefaultConfigPath();
        _appConfig = AppConfig.Load(ConfigPath);
        RefreshMappingPreviews();
        RefreshPorts();
    }

    private static string GetDefaultConfigPath()
    {
        var dir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "kiwiprojekt.tourbox");

        Directory.CreateDirectory(dir);

        var path = Path.Combine(dir, "appsettings.json");

        // Migrate from old location (next to exe) if it exists and new doesn't
        var oldPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "appsettings.json");
        if (!File.Exists(path) && File.Exists(oldPath))
        {
            File.Copy(oldPath, path);
        }

        // If neither exists, copy the default template from the app directory
        if (!File.Exists(path))
        {
            var template = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "appsettings.json");
            if (File.Exists(template))
                File.Copy(template, path);
        }

        return path;
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
    /// Called when a control is selected (from device click or list click).
    /// </summary>
    public void SelectControl(string controlName)
    {
        if (RotaryControls.Contains(controlName))
        {
            _appConfig.Rotary.TryGetValue(controlName, out var rotary);
            Editor.LoadRotary(controlName, rotary);
        }
        else
        {
            _appConfig.Keys.TryGetValue(controlName, out var binding);
            Editor.LoadButton(controlName, binding);
        }
    }

    public void SaveCurrentEditor()
    {
        if (string.IsNullOrEmpty(Editor.ControlName)) return;

        Editor.SaveTo(_appConfig);
        _appConfig.Save(ConfigPath);
        _input.UpdateConfig(_appConfig);
        RefreshMappingPreviews();
    }

    private void RefreshMappingPreviews()
    {
        _input.UpdateConfig(_appConfig);

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

        // Populate mapping rows for the list
        MappingRows.Clear();
        foreach (var (key, binding) in _appConfig.Keys.OrderBy(k => k.Key))
        {
            var entry = MappingEntry.FromKeyBinding(binding);
            MappingRows.Add(new MappingRow
            {
                ControlName = key,
                Action = entry.Preview,
                Mode = entry.Mode,
                IsRotary = false
            });
        }
        foreach (var (key, rotary) in _appConfig.Rotary.OrderBy(k => k.Key))
        {
            var cw = MappingEntry.FromKeyBinding(rotary.Clockwise);
            var ccw = MappingEntry.FromKeyBinding(rotary.CounterClockwise);
            MappingRows.Add(new MappingRow
            {
                ControlName = key,
                Action = $"▲ {cw.Preview}  ▼ {ccw.Preview}",
                Mode = "Rotary",
                IsRotary = true
            });
        }

        OnPropertyChanged(nameof(MappingPreviews));
    }

    public void Dispose()
    {
        _input.Dispose();
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
