using System.IO;
namespace Community.PowerToys.Run.Plugin.BrowserConnect.Services;

public static class Logger{
    private static string? logPath;
    public static void Configure(string path)
    {
        logPath = path;
    }
     public static void Log(string message, string level = "INFO")
        {
            try
            {
                if(string.IsNullOrEmpty(logPath)) return;
                string timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                string logEntry = $"[{timestamp}] [{level}] {message}\n";
                // TODO: Add log rotation once the file grows past a sensible size.
                File.AppendAllText(logPath, logEntry);
            }
            catch { /* Fail silently to not crash the plugin */ }
        }
}
