using Wox.Plugin;

namespace Community.PowerToys.Run.Plugin.BrowserConnect.Interfaces;

public interface ISearchService
{
    List<Result> GetResults(List<Result> defaultResult, string engineKey, string query, bool inIncognito);
}