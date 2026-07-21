using Community.PowerToys.Run.Plugin.BrowserConnect.Models;

namespace Community.PowerToys.Run.Plugin.BrowserConnect.Interfaces;

public interface ISearchProvider
{
    Task<List<CustomSearchResult>> SearchAsync(string query);
}