using System.Collections.Generic;
using Wox.Plugin;
using Community.PowerToys.Run.Plugin.BrowserConnect.Services;
using Community.PowerToys.Run.Plugin.BrowserConnect.Consts;

namespace Community.PowerToys.Run.Plugin.BrowserConnect.Utils;

public static class ResultUtils
{
    public static List<Result> GetErrorResult(IconService? iconService = null)
    {
        return new List<Result>
        {
            new() {
                Title = "Plugin Query Error",
                SubTitle = "Check logs.txt for details.",
                IcoPath = iconService?.GetIconPath(IconConsts.ERROR) ?? "Images\\browserConnect.light.png"
            }
        };
    }

    public static List<Result> GetEmptyList()
    {
        return new List<Result>();
    }

    public static List<Result> GetAddNewUsageResult(IconService iconService, string input){
        return
        new List<Result> {
            new Result {
                Title = "Usage: -add @alias URL",
                SubTitle = "MUST include https:// in the URL. Add %s where the search query should be inserted.",
                IcoPath = iconService.GetIconPath(IconConsts.ADD_NEW),
                QueryTextDisplay = $"{input}"
            }
        };
    }

    public static List<Result> GetDeleteUsageResult(IconService iconService, string input){
        return
        new List<Result> {
            new Result {
                Title = "Usage: -d @alias",
                SubTitle = "Delete an existing search engine",
                IcoPath = iconService.GetIconPath(IconConsts.DELETE),
                QueryTextDisplay = $"{input}"
            }
        };
    }

    public static List<Result> GetDeleteErrorResult(IconService iconService, string input, string aliasToDelete){
        return
        new List<Result> {
            new Result {
                Title = "Alias not found!",
                SubTitle = $"Cannot delete '{aliasToDelete}' - it doesn't exist",
                IcoPath = iconService.GetIconPath(IconConsts.DELETE),
                QueryTextDisplay = $"{input}"
            }
        };
    }

    public static List<Result> GetEmptyHistoryResult(IconService iconService)
    {
        return
        new List<Result> {
            new Result {
                Title = "No history yet!",
                SubTitle = "Start searching to build your history.",
                IcoPath = iconService.GetIconPath(IconConsts.HISTORY)
            }
        };
    }
    public static List<Result> GetHistoryErrorResult(IconService iconService, string error)
    {
        return
        new List<Result> {
            new Result { 
                Title = "Error displaying history!",
                SubTitle = error, 
                IcoPath = iconService.GetIconPath(IconConsts.ERROR)
            }
        };
    }
    public static List<Result> GetSearchProviderEmptyResult(IconService iconService)
    {
        return
        new List<Result> {
            new Result { 
                Title = "No results found!",
                SubTitle = "Search query using search engine instead", 
                IcoPath = iconService.GetIconPath(IconConsts.ERROR)
            }
        };
    }
    public static List<Result> GetSearchProviderErrorResult(IconService iconService, string error)
    {
        return
        new List<Result> {
            new Result { 
                Title = "Error occurred while searching!",
                SubTitle = error, 
                IcoPath = iconService.GetIconPath(IconConsts.ERROR)
            }
        };
    }
}
