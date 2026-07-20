using System.Diagnostics;
using System.IO;
using Community.PowerToys.Run.Plugin.BrowserConnect.Services;

namespace Community.PowerToys.Run.Plugin.BrowserConnect.Utils;

public static class FileUtils
{
    /// <summary>
    /// Opens the file at specified path.
    /// Creates folder if it doesn't exist.
    /// Creates and Opens the file if it doesn't exist.
    /// In best effort case, opens the folder containing the file instead of the file itself
    /// </summary>
    /// <param name="path"></param>
    public static void OpenFile(string path)
    {
        string? directory = Path.GetDirectoryName(path);
        try
        {
            if (!string.IsNullOrEmpty(directory))
            {
                Directory.CreateDirectory(directory);
            }
            if (!File.Exists(path))
            {
                using var _ = File.Create(path);
            }
            Process.Start(new ProcessStartInfo { FileName = path, UseShellExecute = true });
        }
        catch (Exception)
        {
            try
            {
                if (!string.IsNullOrEmpty(directory))
                {
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = "explorer.exe",
                        Arguments = $"/select,\"{path}\"",
                        UseShellExecute = true
                    });
                }
            }
            catch (Exception ex)
            {
                Logger.Log(ex.Message,"ERROR");
            }
        }
    }
}