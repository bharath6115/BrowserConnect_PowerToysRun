using System.Windows.Input;
using Community.PowerToys.Run.Plugin.BrowserConnect.Consts;
using Community.PowerToys.Run.Plugin.BrowserConnect.Handlers;
using Community.PowerToys.Run.Plugin.BrowserConnect.Models;
using Community.PowerToys.Run.Plugin.BrowserConnect.Utils;
using Wox.Plugin;

namespace Community.PowerToys.Run.Plugin.BrowserConnect.Services;

public class ContextMenuService
{
    private readonly HistoryService _historyService;
    private readonly ActionService _actionService;

    public ContextMenuService(HistoryService historyService, ActionService actionService)
    {
        _historyService = historyService;
        _actionService = actionService;
    }

    /// <summary>
    /// Creates context menu actions from the metadata stored on a result.
    /// </summary>
    /// <param name="result">PowerToys result whose <see cref="CustomResultContext"/> drives the available actions.</param>
    /// <returns>Context menu entries supported by the selected result.</returns>
    public List<ContextMenuResult> GetContextMenu(Result result)
    {
        var menu = new List<ContextMenuResult>();
        var context = (CustomResultContext)result.ContextData;

        string title = context.Title;
        string url = context.Url;
        string historyLine = context.HistoryLine;

        if (!string.IsNullOrEmpty(url)) menu.Add(CopyUrl(url));
        if(!string.IsNullOrEmpty(title)) menu.Add(CopyTitle(title));
        if(!string.IsNullOrEmpty(url)) menu.Add(BrowseInToggledIncognitoState(context));
        if (!string.IsNullOrEmpty(historyLine)) menu.Add(DeleteHistoryEntry(historyLine));

        if(context.IsFlagToOpenFile && !string.IsNullOrEmpty(context.FilePath)) menu.Add(CopyFilePath(context.FilePath));
        
        return menu;
    } 

    /// <summary>
    /// Creates a context action that copies the result URL to the clipboard.
    /// </summary>
    /// <param name="url">URL to copy.</param>
    /// <returns>A context menu entry bound to Ctrl+C.</returns>
    private static ContextMenuResult CopyUrl(string url)
    {
        return new ContextMenuResult
        {
            PluginName = "BrowserConnect",
            Title = "Copy URL \n(Ctrl+C)",
            Glyph = "\xE71B",
            FontFamily = "Segoe Fluent Icons",
            AcceleratorKey = Key.C,
            AcceleratorModifiers = ModifierKeys.Control,
            Action = _ =>
            {
                Clipboard.SetText(url);
                return true;
            }
        };
    }

    /// <summary>
    /// Creates a context action that copies the result title to the clipboard.
    /// </summary>
    /// <param name="title">Title to copy.</param>
    /// <returns>A context menu entry bound to Ctrl+Shift+C.</returns>
    private static ContextMenuResult CopyTitle(string title)
    {
        return new ContextMenuResult
        {
            PluginName = "BrowserConnect",
            Title = "Copy Title \n(Ctrl+Shift+C)",
            Glyph = "\xE8C8",
            FontFamily = "Segoe Fluent Icons",
            AcceleratorKey = Key.C,
            AcceleratorModifiers = ModifierKeys.Control | ModifierKeys.Shift,
            Action = _ =>
            {
                Clipboard.SetText(title);
                return true;
            }
        };
    }

    /// <summary>
    /// Creates a context action that copies a local file path to the clipboard.
    /// </summary>
    /// <param name="path">File path to copy.</param>
    /// <returns>A context menu entry bound to Ctrl+C.</returns>
    private static ContextMenuResult CopyFilePath(string path)
    {
        return new ContextMenuResult
        {
            PluginName = "BrowserConnect",
            Title = "Copy File Path \n(Ctrl+C)",
            Glyph = "\xE8C8",
            FontFamily = "Segoe Fluent Icons",
            AcceleratorKey = Key.C,
            AcceleratorModifiers = ModifierKeys.Control,
            Action = _ =>
            {
                Clipboard.SetText(path);
                return true;
            }
        };
    }

    /// <summary>
    /// Creates a context action that replays the selected result with the incognito state toggled.
    /// </summary>
    /// <param name="context">Result context containing the URL, search payload, and original incognito state.</param>
    /// <returns>A context menu entry bound to Ctrl+Shift+N.</returns>
    private ContextMenuResult BrowseInToggledIncognitoState(CustomResultContext context)
    {
        bool isIncognito = context.IsIncognito;
        string url = context.Url;
        string searchQuery = context.SearchQuery;
        string searchEngine = context.SearchEngine;

        return new ContextMenuResult
        {
            PluginName = "BrowserConnect",
            Title = $"Browse {(isIncognito ? "Normally" : "in Incognito")} \n(Ctrl+Shift+N)",
            Glyph = isIncognito ? "\xE721" : "\xE727",
            FontFamily = "Segoe Fluent Icons",
            AcceleratorKey = Key.N,
            AcceleratorModifiers = ModifierKeys.Control | ModifierKeys.Shift,
            Action = _ =>
            {
                switch (context.SearchType)
                {
                    case SearchType.URL:
                        _actionService.ExecuteUrl(url, !isIncognito);
                        break;
                    
                    case SearchType.MULTI:
                        var (queryMulti, targetEngines) = InputUtils.ParseMultiEngineHistoryQuery(context.EncodedQuery);
                        _actionService.ExecuteMulti(queryMulti,targetEngines, !isIncognito);
                        break;
                    
                    case SearchType.LIVE:
                        _actionService.ExecuteLive(context.Title,url,context.ThumbnailRef,!isIncognito);
                        break;

                    case SearchType.DEFAULT:
                        _actionService.ExecuteDefault(searchQuery,searchEngine,url,!isIncognito);
                        break;
                }
                
                return true;
            }
        };
    }

    /// <summary>
    /// Creates a context action that removes the selected persisted history entry.
    /// </summary>
    /// <param name="entry">Exact serialized history line to delete.</param>
    /// <returns>A context menu entry bound to Ctrl+Delete.</returns>
    private ContextMenuResult DeleteHistoryEntry(string entry)
    {
        return new ContextMenuResult
        {
            PluginName = "BrowserConnect",
            Title = $"Delete history entry \n(Ctrl+Del)",
            Glyph = "\xE74D",
            FontFamily = "Segoe Fluent Icons",
            AcceleratorKey = Key.Delete,
            AcceleratorModifiers = ModifierKeys.Control,
            Action = _ =>
            {
                _historyService.DeleteEntry(entry);
                return true;
            }
        };       
    }
}
