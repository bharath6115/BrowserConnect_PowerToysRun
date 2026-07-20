using Community.PowerToys.Run.Plugin.BrowserConnect.Consts;
using Community.PowerToys.Run.Plugin.BrowserConnect.Services;

namespace Community.PowerToys.Run.Plugin.BrowserConnect.Utils;
public static class InputUtils
{
    /// <summary>
    /// Removes the incognito flag from input and resolves whether incognito mode should be used.
    /// </summary>
    /// <param name="rawInput">Original query text entered by the user.</param>
    /// <param name="IsIncognitoDefault">Whether incognito mode is enabled by default.</param>
    /// <returns>The cleaned input and the resolved incognito value.</returns>
    public static (string cleanInput, bool inIncognito) ParseInput(string rawInput, bool IsIncognitoDefault)
    {
        var tokens = rawInput.Split(' ');
        bool inIncognito = tokens.Contains("-i") || IsIncognitoDefault;
        string cleanInput = string.Join(" ", tokens.Where(t => t != "-i")).Trim();
        return (cleanInput, inIncognito);
    }
    
    /// <summary>
    /// Removes a trailing symbol from input when it is present.
    /// </summary>
    /// <param name="input">Input text to check.</param>
    /// <param name="symbol">Symbol to remove from the end of the input.</param>
    /// <returns>The cleaned input and whether the symbol was found.</returns>
    public static (string cleanInput, bool isSymbolPresent) CheckIfEndsWithAndRemoveSymbol(string input, string symbol)
    {
        if (!input.EndsWith(symbol)) return (input, false);
        return (input[..^symbol.Length].Trim(), true);
    }

    /// <summary>
    /// Parses a multi-engine search query into search text, valid engines, and invalid engine names.
    /// </summary>
    /// <param name="cleanInput">Input in the format "engine engine: search query".</param>
    /// <param name="engines">Available engines keyed by alias.</param>
    /// <returns>The search query, matched engine aliases, and unknown engine aliases.</returns>
    public static (string searchQuery, List<string> targetEngines, List<string> invalidEngines) ParseMultiEngineSearchInput(string cleanInput, IReadOnlyDictionary<string,string> engines)
    {
        var colonParts = cleanInput.Split(':', 2);
        string enginePart = colonParts[0].ToLowerInvariant();
        string searchQuery = colonParts[1].Trim();

        string[] potentialEngines = enginePart.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var targetEngines = new List<string>();
        var invalidEngines = new List<string>();

        foreach (string word in potentialEngines)
        {
            if (word.StartsWith('@') && engines.ContainsKey(word[1..])) targetEngines.Add(word[1..]);
            else if (engines.ContainsKey(word)) targetEngines.Add(word);
            else invalidEngines.Add(word);
        }

        return (searchQuery,targetEngines,invalidEngines);
    }

    /// <summary>
    /// Parses a saved multi-engine history payload into search query and target engines.
    /// </summary>
    /// <param name="encodedQuery">Payload in the format "E1, E2, E3 <RECORD_SEPARATOR> Query".</param>
    /// <returns>The search query and target engine aliases.</returns>
    public static (string searchQuery, List<string> targetEngines) ParseMultiEngineHistoryQuery(string encodedQuery)
    {
        var queryParts = encodedQuery.Split(SymbolConsts.RECORD_SEPERATOR,2);
        if (queryParts.Length != 2)
        {
            Logger.Log($"Error parsing: {encodedQuery}","ERROR");
            return ("", []);
        }
        var targetEngines = queryParts[0]
            .Split(",", StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries).ToList();
        var searchQuery = queryParts[1].Trim();

        return (searchQuery,targetEngines);
    }

    /// <summary>
    /// Parses a saved live-provider history payload that includes title, URL, and thumbnail reference.
    /// </summary>
    /// <param name="input">Saved live-provider payload to parse.</param>
    /// <param name="engineKey">History entry type. Must be "_LIVE".</param>
    /// <param name="query">Parsed live result title.</param>
    /// <param name="url">Parsed URL.</param>
    /// <param name="thumbnailRef">Parsed thumbnail reference.</param>
    /// <returns>True when the input matches the expected saved live-provider format.</returns>
    public static bool TryParseLiveSearchInput(string input, string engineKey, out string query, out string url, out string thumbnailRef)
    {
        var parts = input.Split(SymbolConsts.RECORD_SEPERATOR);

        if (!engineKey.Equals("_LIVE") || parts.Length != 3)
        {
            query = url = thumbnailRef = string.Empty;
            return false;
        }

        query = parts[0];
        url = parts[1];
        thumbnailRef = parts[2];
        return true;
    }

    /// <summary>
    /// Joins payload parts with the given separator.
    /// </summary>
    /// <param name="separator">Separator to place between parts.</param>
    /// <param name="parts">Payload parts to merge.</param>
    /// <returns>Parts joined with the separator.</returns>
    public static string MergeWithSeparator(string separator, params string[] parts)
    {
        if(parts.Length is 1) return parts[0];
        return string.Join(separator, parts);
    }
}
