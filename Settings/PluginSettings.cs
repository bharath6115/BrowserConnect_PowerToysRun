using Microsoft.PowerToys.Settings.UI.Library;

namespace Community.PowerToys.Run.Plugin.BrowserConnect.Settings;

public class PluginSettings
{
    public bool IsIncognitoDefault { get; set; } = SettingsConsts.IsIncognitoDefault_DV;
    public bool IsHistoryEnabled { get; set; } = SettingsConsts.IsHistoryEnabled_DV;
    public bool RecordIncognitoHistory { get; set; } = SettingsConsts.RecordIncognitoHistory_DV;
    public bool AutoTruncateHistory { get; set; } = SettingsConsts.AutoTruncateHistory_DV;
    public int HistoryLimit { get; set; } = SettingsConsts.HistoryLimit_DV;
    public int HistoryCacheSize { get; set; } = SettingsConsts.HistoryCacheSize_DV;

    public IEnumerable<PluginAdditionalOption> Options => [
        new PluginAdditionalOption
        {
            Key = nameof(IsIncognitoDefault),
            DisplayLabel = "Incognito by default",
            DisplayDescription = "Automatically use incognito mode for all searches.",
            Value = false
        },
        new PluginAdditionalOption
        {
            Key = nameof(IsHistoryEnabled),
            DisplayLabel = "Record History",
            DisplayDescription = "Enable or disable recording search history.",
            Value = true
        },
        new PluginAdditionalOption
        {
            Key = nameof(RecordIncognitoHistory),
            DisplayLabel = "Record Incognito History",
            DisplayDescription = "Enable or disable recording search history for searches done in incognito mode.",
            Value = false
        },
        new PluginAdditionalOption
        {
            Key = nameof(AutoTruncateHistory),
            DisplayLabel = "Automatically Truncate History",
            DisplayDescription = "Enable or disable automatic truncation of search history when 'History File Limit' is reached.",
            Value = false
        },
        new PluginAdditionalOption
        {
            Key = nameof(HistoryLimit),
            DisplayLabel = "Max History Entries",
            DisplayDescription = "Maximum number of lines to keep in history.txt.",
            PluginOptionType = PluginAdditionalOption.AdditionalOptionType.Numberbox,
            NumberValue = 3000
        },
        new PluginAdditionalOption
        {
            Key = nameof(HistoryCacheSize),
            DisplayLabel = "History Results Count",
            DisplayDescription = "How many recent unique results to show in history list.",
            PluginOptionType = PluginAdditionalOption.AdditionalOptionType.Numberbox,
            NumberValue = 1500
        }
    ];
}