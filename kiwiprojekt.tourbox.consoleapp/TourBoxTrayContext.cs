using System.Text.Json;
using WindowsInput;

namespace kiwiprojekt.tourbox.consoleapp
{
    public class TourBoxTrayContext : ApplicationContext
    {
        private NotifyIcon _trayIcon;
        private TourBoxHandler _handler;
        private TourBoxController _controller;

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
            var contextMenu = new ContextMenuStrip();
            contextMenu.Items.Add("Show Dir", null, ShowDir);
            contextMenu.Items.Add("-"); // Separator
            contextMenu.Items.Add("Exit", null, Exit);
            _trayIcon.ContextMenuStrip = contextMenu;
            
            // Handle double click to open dir too
            _trayIcon.DoubleClick += ShowDir;

            // Load Logic
            InitializeTourBox();
        }

        private void InitializeTourBox()
        {
            try 
            {
                var configPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "appsettings.json");
                if (!File.Exists(configPath))
                {
                    FileLogger.Log("Error: appsettings.json not found.");
                    MessageBox.Show("Config file not found!", "TourBox Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                FileLogger.Log("Loading config...");
                var json = File.ReadAllText(configPath);
                var config = JsonSerializer.Deserialize<TourBoxConfig>(json);

                if (config == null)
                {
                    FileLogger.Log("Error: Configuration is empty.");
                    return;
                }

                var inputSimulator = new InputSimulator();
                _controller = new TourBoxController(config, inputSimulator);

                // Use a background thread or asynchronous task if needed, but Handler might be async?
                // The original code used new TourBoxHandler("COM3", ...) which presumably opens the port.
                // SerialPort events are usually on a separate thread anyway.
                
                string port = "COM3"; // Hardcoded
                _handler = new TourBoxHandler(port, _controller.HandleEvent);
                FileLogger.Log($"Connected to {port}.");
                _trayIcon.Text = $"TourBox Helper - Connected ({port})";
            }
            catch (Exception ex)
            {
                FileLogger.Log($"Initialization Error: {ex.Message}");
                _trayIcon.Text = "TourBox Helper - Error";
                // Don't show MessageBox on every restart if it's auto-start, but useful for now
            }
        }

        private void Exit(object sender, EventArgs e)
        {
            _trayIcon.Visible = false;
            _handler?.Dispose();
            Application.Exit();
        }
        private void ShowDir(object sender, EventArgs e)
        {
            try
            {
                var path = AppDomain.CurrentDomain.BaseDirectory;
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo()
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
    }
}
