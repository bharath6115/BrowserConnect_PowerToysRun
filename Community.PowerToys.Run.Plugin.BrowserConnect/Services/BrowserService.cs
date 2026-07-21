using Community.PowerToys.Run.Plugin.BrowserConnect.Utils;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using Wox.Plugin;

namespace Community.PowerToys.Run.Plugin.BrowserConnect.Services;

public static class BrowserService
{
    private const string DefaultBrowserRegistryKey = @"Software\Microsoft\Windows\Shell\Associations\UrlAssociations\https\UserChoice";

    /// <summary>
    /// Maps browser executables to their respective private browsing flags.
    /// </summary>
    private static readonly Dictionary<string, string> IncognitoFlags = new(StringComparer.OrdinalIgnoreCase)
    {
        ["chrome.exe"] = "--incognito",
        ["brave.exe"] = "--incognito",
        ["msedge.exe"] = "--inprivate",
        ["firefox.exe"] = "-private-window",
        ["opera.exe"] = "--private",
        ["vivaldi.exe"] = "--incognito",
        ["arc.exe"] = "--incognito",
    };

    /// <summary>
    /// Opens the given URL in the user's default browser.
    /// Falls back to the Windows default URL handler if detection fails.
    /// </summary>
    /// <param name="url">URL or bare domain to open.</param>
    /// <param name="incognito">Whether to request private browsing when the detected browser supports it.</param>
    public static void OpenBrowser(string url, bool incognito = false)
    {
        try
        {
            if (!UrlUtils.TryNormalizeWebUrl(url, out var safeUrl)) return;
            
            string? browserPath = GetDefaultBrowserPath();

            // Couldn't determine the browser executable.
            if (string.IsNullOrWhiteSpace(browserPath))
            {
                OpenUsingWindows(safeUrl);
                return;
            }

            string browserExe = Path.GetFileName(browserPath);

            string arguments = $"\"{safeUrl}\"";

            // Add the browser-specific incognito flag if supported.
            if (incognito && IncognitoFlags.TryGetValue(browserExe, out string? flag))
            {
                arguments = $"{flag} {arguments}";
            }

            Process.Start(new ProcessStartInfo
            {
                FileName = browserPath,
                Arguments = arguments,
                UseShellExecute = true
            });
        }
        catch( Exception ex)
        {
            // Never fail to open a URL because browser detection failed.
            Logger.Log("Error opening Browser: "+ ex.Message, "ERROR");
            OpenUsingWindows(url);
        }
    }

    /// <summary>
    /// Lets Windows open the URL using the registered default application.
    /// This fallback cannot force incognito mode because the target browser is unknown.
    /// </summary>
    /// <param name="url">URL to open through Windows shell execution.</param>
    private static void OpenUsingWindows(string url)
    {
        Process.Start(new ProcessStartInfo
        {
            FileName = url,
            UseShellExecute = true
        });
    }

    /// <summary>
    /// Returns the full path to the user's default browser executable.
    /// Returns null if the browser cannot be determined.
    /// </summary>
    private static string? GetDefaultBrowserPath()
    {
        using RegistryKey? userChoice =
            Registry.CurrentUser.OpenSubKey(DefaultBrowserRegistryKey);

        string? progId = userChoice?.GetValue("ProgId") as string;
        if (progId == null)
            return null;

        using RegistryKey? commandKey =
            Registry.ClassesRoot.OpenSubKey($@"{progId}\shell\open\command");

        string? command = commandKey?.GetValue(null) as string;
        if (string.IsNullOrWhiteSpace(command))
            return null;

        // Executable paths are usually quoted:
        // "C:\Program Files\Google\Chrome\Application\chrome.exe" -- "%1"
        if (command.StartsWith('"'))
        {
            int endQuote = command.IndexOf('"', 1);
            return endQuote > 0 ? command[1..endQuote] : null;
        }

        // Rare case: executable path isn't quoted.
        int firstSpace = command.IndexOf(' ');
        return firstSpace > 0 ? command[..firstSpace] : command;
    }
}
