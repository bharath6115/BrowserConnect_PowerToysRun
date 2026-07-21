using System.IO;
using Wox.Plugin;
using Community.PowerToys.Run.Plugin.BrowserConnect.Services;
using Community.PowerToys.Run.Plugin.BrowserConnect.Handlers;
using Microsoft.PowerToys.Settings.UI.Library;
using Community.PowerToys.Run.Plugin.BrowserConnect.Utils;
using Community.PowerToys.Run.Plugin.BrowserConnect.Settings;
using Community.PowerToys.Run.Plugin.BrowserConnect.Providers.Youtube;
using Community.PowerToys.Run.Plugin.BrowserConnect.Providers.Anilist;
using Community.PowerToys.Run.Plugin.BrowserConnect.Providers.SeriesGraph;

namespace Community.PowerToys.Run.Plugin.BrowserConnect
{
    public class BrowserPlugin : IPlugin, ISettingProvider, IContextMenu, IDelayedExecutionPlugin
    {
        public static string PluginID => "B8A5C3D2E1F4A6B7C8D9E0F1A2B3C4D5";
        public string Name => "BrowserConnect";
        public string Description => "Browse using custom engines. Use -help flag to view all flags!";

        // The Init() function runs and sets these values to non null. Disabling pragma warning CS8618 isnt best move here.
        private EngineService _engineService = null!;
        private HistoryService _historyService = null!;
        private IconService _iconService = null!;
        private QueryService _queryService = null!;
        private FlagCommandHandler _flagCommandHandler = null!;
        private HistoryCommandHandler _historyCommandHandler = null!;
        private ActionService _actionService = null!;
        private ContextMenuService _contextMenuService = null!;
        private PluginInitContext _context = null!;
        private ThumbnailManager _thumbnailManager = null!;
        private YoutubeService _youtubeService = null!;
        private AnilistService _anilistService = null!;
        private SeriesGraphService _seriesgraphService = null!;
        public PluginSettings Settings {get;} = new();

        //GetExecutingAssembly().Location to get the directory of plugin
        public static readonly string? pluginDir = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
        public static readonly string imagesDir = pluginDir != null ? Path.Combine(pluginDir, "Images") : "";
        public static readonly string searchEnginesPath = pluginDir != null ? Path.Combine(pluginDir, "searchEngines.txt") : "";
        public static readonly string historyPath = pluginDir != null ? Path.Combine(pluginDir, "history.txt") : "";
        public static readonly string thumbnailsPath = pluginDir != null ? Path.Combine(pluginDir, "Thumbnails") : "";
        public static readonly string logPath = pluginDir != null ? Path.Combine(pluginDir,"logs.txt") : "";
        public static readonly bool isDarkTheme = IconService.IsDarkTheme();

        public void Init(PluginInitContext context)
        {
            if(pluginDir is null) return;

            _context = context;
            Logger.Configure(logPath);
            Logger.Log("Plugin Initializing...", "INFO");
            SettingsService.LogSettings(Settings);

            try
            {
                _engineService = new EngineService(searchEnginesPath);
                _historyService = new HistoryService(historyPath, Settings);
                _iconService = new IconService(pluginDir, imagesDir, isDarkTheme, _engineService);
                _thumbnailManager = new ThumbnailManager(thumbnailsPath, _iconService);

                _actionService = new ActionService(_historyService, _engineService);
                _contextMenuService = new ContextMenuService(_historyService, _actionService);

                _youtubeService = new YoutubeService(new YoutubeProvider(), _thumbnailManager, _historyService, _iconService, _actionService);
                _anilistService = new AnilistService(new AnilistProvider(), _thumbnailManager, _historyService, _iconService, _actionService);
                _seriesgraphService = new SeriesGraphService(new SeriesGraphProvider(), _thumbnailManager, _historyService, _iconService, _actionService);
                

                _flagCommandHandler = new FlagCommandHandler(_engineService, _historyService, _iconService, this, historyPath, logPath);
                _historyCommandHandler = new HistoryCommandHandler(_engineService, _historyService, _iconService, _actionService, this, _thumbnailManager);

                _queryService = new QueryService(_engineService, _actionService, _iconService, _youtubeService, _anilistService, _seriesgraphService);

                Logger.Log($"Initialized with {_engineService.Count} engines. History enabled: {Settings.IsHistoryEnabled}.", "INFO");
            }
            catch (Exception ex)
            {
                Logger.Log($"Critical Init Error: {ex.Message}", "ERROR");
            }
        }


        /*----------QUERY METHODS----------*/
        public List<Result> Query(Query query)
        {
            try
            {
                string rawInput = query.Search ?? "";
                string trimmedRawInput = rawInput.Trim();

                // Handle utility commands before normal search parsing.
                if (trimmedRawInput.StartsWith("-lo")) return _flagCommandHandler.HandleOpenLogCommand();
                if (trimmedRawInput.StartsWith("-l")) return _flagCommandHandler.HandleOpenListCommand();
                if (trimmedRawInput.StartsWith("-a")) return _flagCommandHandler.HandleAddCommand(rawInput);
                if (trimmedRawInput.StartsWith("-d")) return _flagCommandHandler.HandleDeleteCommand(rawInput);
                if (trimmedRawInput.StartsWith("-r")) return _flagCommandHandler.HandleRefreshCommand();
                if (trimmedRawInput.StartsWith("-hi")) return _flagCommandHandler.HandleOpenHistoryCommand();
                if (trimmedRawInput.StartsWith("-h")) return _flagCommandHandler.HandleHelpCommand();
                if (trimmedRawInput.StartsWith('!')) return _historyCommandHandler.HandleHistoryCommand(rawInput);

                var (cleanInput, inIncognito) = InputUtils.ParseInput(rawInput, Settings.IsIncognitoDefault);

                /// Render ALL search engines when there is no query
                /// `inIncognito | Settings.IsIncognitoDefault` to consider inIncognito flag as well.
                if (string.IsNullOrWhiteSpace(cleanInput))
                {
                    return _engineService.GetAvailableEngines(_iconService, _historyService, inIncognito || Settings.IsIncognitoDefault);
                } 

                /// Handle URL Search
                /// Identifying URL -> A URL will not have spaces and will definetly have a '.'
                /// We do not hardcode "starts with http" so we can just search "youtube.com" instead of "https://youtube.com"
                if (cleanInput.Contains('.') && !cleanInput.Contains(' '))
                {
                    return _queryService.HandleURLSearch(rawInput, Settings.IsIncognitoDefault);
                }

                /// Handle Multi Engine Search
                /// Loose checking -> NO Concerete way to classify multi search engine except use of ':'
                /// Actually safe because no query usually contains ':' except urls which are dealt with before (cope?) 
                /// BUT [TODO if required]. Can make it stricter using "::" for activation. 
                if (cleanInput.Contains(':'))
                {
                    var result = _queryService.HandleMultiEngineSearch(rawInput, Settings.IsIncognitoDefault);
                    if(result != null) return result;
                }

                /// Exact & Prefix search engine results
                return _queryService.HandleSingleEngineSearch(rawInput, Settings.IsIncognitoDefault);
            }
            catch (Exception ex)
            {
                Logger.Log($"Error in Query processing: {ex.Message}", "ERROR");
                return ResultUtils.GetErrorResult(_iconService);
            }
        }

        // Called ~300ms after the user stops typing. Only works if input has keyword - ";" to green-flag the searching using api
        public List<Result> Query(Query query, bool isFullQuery)
        {
            if (!isFullQuery) return ResultUtils.GetEmptyList();

            try
            {
                string rawInput = query.Search ?? "";
                string triggerSymbol = ";";
                return _queryService.HandleDelayedResults(rawInput, Settings.IsIncognitoDefault, triggerSymbol);
            }
            catch (Exception ex)
            {
                Logger.Log($"Delayed query error: {ex.Message}", "ERROR");
                return ResultUtils.GetErrorResult();
            }
        }


        /*----------HELPER METHODS----------*/
        public void ReloadYoutubeApiData() => _youtubeService.ReloadYoutubeApiData();

        /*----------SETTINGS PANEL----------*/
        public System.Windows.Controls.Control CreateSettingPanel()
        {
            return new System.Windows.Controls.Control();
        }
        public IEnumerable<PluginAdditionalOption> AdditionalOptions => Settings.Options;

        public void UpdateSettings(PowerLauncherPluginSettings newSettings)
        {
            SettingsService.Apply(Settings, newSettings);
            _historyService?.LoadHistoryCache();
        }

        /*----------CONTEXT MENU----------*/
        public List<ContextMenuResult> LoadContextMenus(Result selectedResult)
        {
            return _contextMenuService.GetContextMenu(selectedResult);
        }

    }
}
