using System.IO.Ports;

namespace kiwiprojekt.tourbox
{
    public class TourBoxHandler : IDisposable
    {
        private readonly SerialPort port;
        private readonly Action<TourBoxEvent> handler;
        private bool disposed;

        /// <summary>
        /// Unlock command sent to authenticate with TourBox Elite and newer models.
        /// </summary>
        private static readonly byte[] UnlockCommand =
            { 0x55, 0x00, 0x07, 0x88, 0x94, 0x00, 0x1a, 0xfe };

        /// <summary>
        /// 94-byte configuration message enabling all controls with haptics off.
        /// Compatible with all models (Elite, Neo, Lite).
        /// Haptic strength/speed bytes are all 0x00 for maximum compatibility.
        /// </summary>
        private static readonly byte[] ConfigMessage =
        {
            0xb5, 0x00, 0x5d, 0x04, 0x00, 0x05, 0x00, 0x06,
            0x00, 0x07, 0x00, 0x08, 0x00, 0x09, 0x00, 0x0b,
            0x00, 0x0c, 0x00, 0x0d, 0x00, 0x0e, 0x00, 0x0f,
            0x00, 0x26, 0x00, 0x27, 0x00, 0x28, 0x00, 0x29,
            0x00, 0x3b, 0x00, 0x3c, 0x00, 0x3d, 0x00, 0x3e,
            0x00, 0x3f, 0x00, 0x40, 0x00, 0x41, 0x00, 0x42,
            0x00, 0x43, 0x00, 0x44, 0x00, 0x45, 0x00, 0x46,
            0x00, 0x47, 0x00, 0x48, 0x00, 0x49, 0x00, 0x4a,
            0x00, 0x4b, 0x00, 0x4c, 0x00, 0x4d, 0x00, 0x4e,
            0x00, 0x4f, 0x00, 0x50, 0x00, 0x51, 0x00, 0x52,
            0x00, 0x53, 0x00, 0x54, 0x00, 0xa8, 0x00, 0xa9,
            0x00, 0xaa, 0x00, 0xab, 0x00, 0xfe
        };

        public TourBoxHandler(SerialPort serialPort, Action<TourBoxEvent> eventHandler, bool requiresInit = true)
        {
            ArgumentNullException.ThrowIfNull(serialPort);
            ArgumentNullException.ThrowIfNull(eventHandler);

            port = serialPort;
            handler = eventHandler;

            if (!port.IsOpen)
            {
                port.Open();
            }

            if (requiresInit)
            {
                InitializeDevice();
            }

            port.DataReceived += SerialDataReceived;
        }

        public TourBoxHandler(string comPortName, Action<TourBoxEvent> eventHandler, bool requiresInit = true)
            : this(new SerialPort(comPortName, 115200, Parity.None, 8, StopBits.One), eventHandler, requiresInit)
        {
        }

        private void SerialDataReceived(object? sender, SerialDataReceivedEventArgs e)
        {
            if (disposed)
            {
                return;
            }

            try
            {
                while (!disposed && port.IsOpen && port.BytesToRead > 0)
                {
                    var rawCode = port.ReadByte();
                    if (rawCode < 0)
                    {
                        break;
                    }

                    var tourBoxEvent = EventParser.Parse((byte)rawCode);
                    if (tourBoxEvent is not null)
                    {
                        handler(tourBoxEvent);
                    }
                }
            }
            catch (ObjectDisposedException) when (disposed)
            {
            }
            catch (InvalidOperationException) when (disposed)
            {
            }
        }

        /// <summary>
        /// Performs the TourBox device initialization handshake.
        /// Sends unlock command and configuration message.
        /// Non-fatal on failure — older models or pre-initialized devices
        /// may not respond but will still function.
        /// </summary>
        private void InitializeDevice()
        {
            try
            {
                // Step 1: Send unlock command (8 bytes)
                port.Write(UnlockCommand, 0, UnlockCommand.Length);

                // Step 2: Read unlock response (expect 26 bytes, starting with 0x07)
                var originalTimeout = port.ReadTimeout;
                port.ReadTimeout = 500;
                try
                {
                    var stopwatch = System.Diagnostics.Stopwatch.StartNew();
                    while (stopwatch.ElapsedMilliseconds < 500)
                    {
                        if (port.BytesToRead >= 26)
                        {
                            var response = new byte[26];
                            port.Read(response, 0, 26);
                            break;
                        }

                        Thread.Sleep(10);
                    }
                }
                catch (TimeoutException)
                {
                    // Device may not need unlock (older model or already initialized)
                }
                finally
                {
                    port.ReadTimeout = originalTimeout;
                }

                // Step 3: Send configuration message (94 bytes, all haptics off)
                port.Write(ConfigMessage, 0, ConfigMessage.Length);

                // Step 4: Clear any leftover init data before normal operation
                port.DiscardInBuffer();
            }
            catch (Exception ex) when (ex is not ObjectDisposedException)
            {
                // Initialization failure is non-fatal;
                // device may already be initialized or is an older model.
                System.Diagnostics.Debug.WriteLine(
                    $"TourBox init warning: {ex.Message}");
            }
        }

        public void Dispose()
        {
            if (disposed)
            {
                return;
            }

            disposed = true;
            port.DataReceived -= SerialDataReceived;

            if (port.IsOpen)
            {
                port.Close();
            }

            port.Dispose();
            GC.SuppressFinalize(this);
        }
    }
}
