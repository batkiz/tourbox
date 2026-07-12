using System.Drawing;
using System.Windows;
using kiwiprojekt.tourbox.ui.Services;
using kiwiprojekt.tourbox.ui.ViewModels;
using Timer = System.Windows.Forms.Timer;

namespace kiwiprojekt.tourbox.ui;

public partial class App : System.Windows.Application
{
    private NotifyIcon? _trayIcon;
    private MainWindow? _mainWindow;
    private TourBoxService? _service;
    private MainViewModel? _viewModel;
    private Timer? _portScanTimer;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        _service = new TourBoxService(Dispatcher);
        _viewModel = new MainViewModel(_service);

        _mainWindow = new MainWindow(_viewModel);

        CreateTrayIcon();

        // Periodically scan for new COM ports
        _portScanTimer = new Timer { Interval = 2000 };
        _portScanTimer.Tick += (_, _) => _viewModel.RefreshPorts();
        _portScanTimer.Start();
    }

    private void CreateTrayIcon()
    {
        _trayIcon = new NotifyIcon
        {
            Icon = SystemIcons.Application,
            Visible = true,
            Text = "TourBox Helper"
        };

        var menu = new System.Windows.Forms.ContextMenuStrip();

        var statusItem = new ToolStripMenuItem("状态: 未连接") { Enabled = false };
        _service!.ConnectionChanged += (state, msg) =>
        {
            Dispatcher.Invoke(() => statusItem.Text = $"状态: {msg}");
        };

        var showItem = new ToolStripMenuItem("显示配置窗口", null, (_, _) => ShowMainWindow());
        var reloadItem = new ToolStripMenuItem("重新加载配置", null, (_, _) =>
        {
            _service?.Disconnect();
            _viewModel?.ConnectCommand.Execute(null);
        });
        var exitItem = new ToolStripMenuItem("退出", null, (_, _) => ShutdownApp());

        menu.Items.Add(statusItem);
        menu.Items.Add(showItem);
        menu.Items.Add(reloadItem);
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add(exitItem);

        _trayIcon.ContextMenuStrip = menu;
        _trayIcon.DoubleClick += (_, _) => ShowMainWindow();
    }

    private void ShowMainWindow()
    {
        _mainWindow?.Show();
        _mainWindow?.Activate();
    }

    private void ShutdownApp()
    {
        _portScanTimer?.Stop();
        _portScanTimer?.Dispose();
        _service?.Dispose();
        _trayIcon?.Dispose();

        // Allow the window to actually close
        if (_mainWindow != null)
            _mainWindow.ForcesClose = true;

        _mainWindow?.Close();
        Shutdown();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _portScanTimer?.Dispose();
        _viewModel?.Dispose();
        _service?.Dispose();
        _trayIcon?.Dispose();
        base.OnExit(e);
    }
}
