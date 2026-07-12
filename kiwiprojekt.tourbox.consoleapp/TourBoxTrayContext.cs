using System.Diagnostics;
using System.IO.Ports;
using System.Text.Json;
using WindowsInput;

namespace kiwiprojekt.tourbox.consoleapp
{
    public class TourBoxTrayContext : ApplicationContext
    {
        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            AllowTrailingCommas = true,
            PropertyNameCaseInsensitive = true,
            ReadCommentHandling = JsonCommentHandling.Skip
        };

        private readonly NotifyIcon _trayIcon;
        private readonly ContextMenuStrip _contextMenu;
        private readonly ToolStripMenuItem _statusItem;
        private TourBoxHandler? _handler;
        private TourBoxController? _controller;
        private bool _disposed;

        public TourBoxTrayContext()
        {
            // Initialize Tray Icon
            _trayIcon = new NotifyIcon
            {
                Icon = System.Drawing.SystemIcons.Application, // Use system default icon for now
                Visible = true,
                Text = "TourBox Helper"
            };

            // Context Menu
            _contextMenu = new ContextMenuStrip();
            _statusItem = new ToolStripMenuItem("Status: Starting") { Enabled = false };
            _contextMenu.Items.Add(_statusItem);
            _contextMenu.Items.Add("Reload", null, Reload);
            _contextMenu.Items.Add("Show Dir", null, ShowDir);
            _contextMenu.Items.Add("-");
            _contextMenu.Items.Add("Exit", null, Exit);
            _trayIcon.ContextMenuStrip = _contextMenu;
            
            // Handle double click to open dir too
            _trayIcon.DoubleClick += ShowDir;

            // Load Logic
            InitializeTourBox();
        }

        private void InitializeTourBox()
        {
            Disconnect();

            try 
            {
                FileLogger.Log("Loading config...");
                var config = LoadConfig();
                var port = ResolvePortName(config);

                var inputSimulator = new InputSimulator();
                _controller = new TourBoxController(config, inputSimulator);

                _handler = new TourBoxHandler(port, _controller.HandleEvent, config.RequiresInit);
                FileLogger.Log($"Connected to {port}.");
                UpdateStatus($"Connected ({port})");
            }
            catch (Exception ex)
            {
                FileLogger.Log($"Initialization Error: {ex}");
                UpdateStatus("Error");
            }
        }

        private TourBoxConfig LoadConfig()
        {
            var configPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "appsettings.json");
            if (!File.Exists(configPath))
            {
                throw new FileNotFoundException("Config file appsettings.json was not found.", configPath);
            }

            var json = File.ReadAllText(configPath);
            var config = JsonSerializer.Deserialize<TourBoxConfig>(json, JsonOptions);
            if (config is null)
            {
                throw new InvalidOperationException("Configuration file is empty or invalid.");
            }

            return config;
        }

        private static string ResolvePortName(TourBoxConfig config)
        {
            if (!string.IsNullOrWhiteSpace(config.PortName))
            {
                return config.PortName.Trim();
            }

            var availablePorts = SerialPort.GetPortNames()
                .OrderBy(static port => port, StringComparer.OrdinalIgnoreCase)
                .ToArray();

            return availablePorts.Length switch
            {
                0 => throw new InvalidOperationException("No serial ports detected. Set PortName in appsettings.json."),
                1 => availablePorts[0],
                _ => throw new InvalidOperationException($"Multiple serial ports detected ({string.Join(", ", availablePorts)}). Set PortName in appsettings.json.")
            };
        }

        private void Reload(object? sender, EventArgs e)
        {
            FileLogger.Log("Reload requested.");
            InitializeTourBox();
        }

        private void Exit(object? sender, EventArgs e)
        {
            ExitThread();
        }

        private void Disconnect()
        {
            _handler?.Dispose();
            _handler = null;
            _controller = null;
        }

        private void ShowDir(object? sender, EventArgs e)
        {
            try
            {
                var path = AppDomain.CurrentDomain.BaseDirectory;
                Process.Start(new ProcessStartInfo
                {
                    FileName = path,
                    UseShellExecute = true,
                    Verb = "open"
                });
            }
            catch (Exception ex)
            {
                FileLogger.Log($"Error opening dir: {ex.Message}");
                MessageBox.Show($"Could not open directory: {ex.Message}");
            }
        }

        private void UpdateStatus(string status)
        {
            _statusItem.Text = $"Status: {status}";
            _trayIcon.Text = TrimTrayText($"TourBox Helper - {status}");
        }

        private static string TrimTrayText(string text)
        {
            const int maxTooltipLength = 63;
            return text.Length <= maxTooltipLength ? text : text[..maxTooltipLength];
        }

        protected override void Dispose(bool disposing)
        {
            if (_disposed)
            {
                base.Dispose(disposing);
                return;
            }

            if (disposing)
            {
                Disconnect();
                _trayIcon.DoubleClick -= ShowDir;
                _trayIcon.Visible = false;
                _trayIcon.Dispose();
                _contextMenu.Dispose();
            }

            _disposed = true;
            base.Dispose(disposing);
        }
    }
}
