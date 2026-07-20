using System.Linq;
using Community.PowerToys.Run.Plugin.BrowserConnect.Services;
using Microsoft.PowerToys.Settings.UI.Library;

namespace Community.PowerToys.Run.Plugin.BrowserConnect.Settings;

public static class SettingsService
{
    public static void Apply(PluginSettings settings, PowerLauncherPluginSettings newSettings)
    {
        Logger.Log("Updating Settings...", "TRACE");
        if (newSettings == null || newSettings.AdditionalOptions == null)
        {
            settings.IsIncognitoDefault = SettingsConsts.IsIncognitoDefault_DV;
            settings.IsHistoryEnabled = SettingsConsts.IsHistoryEnabled_DV;
            settings.RecordIncognitoHistory = SettingsConsts.RecordIncognitoHistory_DV;
            settings.AutoTruncateHistory = SettingsConsts.AutoTruncateHistory_DV;
            settings.HistoryLimit = SettingsConsts.HistoryLimit_DV;
            settings.HistoryCacheSize = SettingsConsts.HistoryCacheSize_DV;
            return;
        }

        settings.IsIncognitoDefault = GetBool(newSettings, nameof(settings.IsIncognitoDefault), SettingsConsts.IsIncognitoDefault_DV);
        settings.IsHistoryEnabled = GetBool(newSettings, nameof(settings.IsHistoryEnabled), SettingsConsts.IsHistoryEnabled_DV);
        settings.RecordIncognitoHistory = GetBool(newSettings, nameof(settings.RecordIncognitoHistory), SettingsConsts.RecordIncognitoHistory_DV);
        settings.AutoTruncateHistory = GetBool(newSettings, nameof(settings.AutoTruncateHistory), SettingsConsts.AutoTruncateHistory_DV);
        settings.HistoryLimit = GetInt(newSettings, nameof(settings.HistoryLimit), SettingsConsts.HistoryLimit_DV);
        settings.HistoryCacheSize = GetInt(newSettings, nameof(settings.HistoryCacheSize), SettingsConsts.HistoryCacheSize_DV);

        LogSettings(settings);
    }

    public static void LogSettings(PluginSettings settings)
    {
        Logger.Log("┌──────────────────────────────┐", "ACTION");
        Logger.Log("│           SETTINGS UPDATED            │", "ACTION");
        Logger.Log("├──────────────────────────────┤", "ACTION");
        Logger.Log($"│      Always Incognito   {settings.IsIncognitoDefault,-14}│", "ACTION");
        Logger.Log($"│       Track History     {settings.IsHistoryEnabled,-14}│", "ACTION");
        Logger.Log($"│ Track Incognito History {settings.RecordIncognitoHistory,-14}│", "ACTION");
        Logger.Log($"│  Auto Truncate History  {settings.AutoTruncateHistory,-14}│", "ACTION");
        Logger.Log($"│       History Limit     {settings.HistoryLimit,-14}│", "ACTION");
        Logger.Log($"│        Cache Size       {settings.HistoryCacheSize,-14}│", "ACTION");
        Logger.Log("└──────────────────────────────┘", "ACTION");        
    }

    private static bool GetBool(PowerLauncherPluginSettings settings, string key, bool defaultValue)
    {
        return settings.AdditionalOptions.FirstOrDefault(x => x.Key == key)?.Value ?? defaultValue;
    }

    private static int GetInt(PowerLauncherPluginSettings settings, string key, int defaultValue)
    {
        return (int)(settings.AdditionalOptions.FirstOrDefault(x => x.Key == key)?.NumberValue ?? defaultValue);
    }
}