using System;
using System.Diagnostics;

namespace Community.PowerToys.Run.Plugin.BrowserConnect.Services
{
    public static class BrowserHelper
    {
        public static void OpenBrowser(string url, bool incognito = false)
        {
            try //try to open brave
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = "brave.exe",
                    Arguments = (incognito ? "--incognito " : "") + $"\"{url}\"",
                    UseShellExecute = true
                });
            }
            catch //fallback to default browser set
            {
                Process.Start(new ProcessStartInfo { FileName = url, UseShellExecute = true });
            }
        }

        public static string GetBaseUrl(string url)
        {
            Uri myUri = new Uri(url);
            return $"{myUri.Scheme}://{myUri.Authority}";
        }

        public static string ExtractDomain(string url)
        {
            try
            {
                Uri uri = new Uri(url);
                return uri.Host;
            }
            catch
            {
                return string.Empty;
            }
        }
    }
}
