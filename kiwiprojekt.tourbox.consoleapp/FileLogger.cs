namespace kiwiprojekt.tourbox.consoleapp
{
    public static class FileLogger
    {
        private static readonly string LogPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "log.txt");
        private static readonly object LockObj = new object();

        public static void Log(string message)
        {
            try
            {
                lock (LockObj)
                {
                    File.AppendAllText(LogPath, $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {message}{Environment.NewLine}");
                }
            }
            catch
            {
                // Ignite write errors to avoid crashing app
            }
        }
    }
}
