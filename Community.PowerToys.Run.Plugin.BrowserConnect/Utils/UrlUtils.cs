using Community.PowerToys.Run.Plugin.BrowserConnect.Consts;

namespace Community.PowerToys.Run.Plugin.BrowserConnect.Utils;
public static class UrlUtils
{ 
    /// <summary>
    /// Returns the scheme and host part of a URL.
    /// </summary>
    /// <param name="url">URL to read.</param>
    /// <returns>The base URL, or the original value when it cannot be parsed.</returns>
    public static string GetBaseUrl(string url)
    {
        try
        {
            Uri myUri = new(url);
            return $"{myUri.Scheme}://{myUri.Authority}";
        }
        catch
        {
            return url;
        }
    }
    
    /// <summary>
    /// Returns the host name from a URL.
    /// </summary>
    /// <param name="url">URL to read.</param>
    /// <returns>The URL host, or an empty string when it cannot be parsed.</returns>
    public static string ExtractDomain(string url)
    {
        try
        {
            Uri uri = new(url);
            return uri.Host;
        }
        catch
        {
            return "";
        }
    }

    /// <summary>
    /// Builds a search URL from a query and URL template.
    /// </summary>
    /// <param name="searchQuery">Search text to place into the template.</param>
    /// <param name="urlTemplate">URL template, optionally containing "%s".</param>
    /// <returns>The generated search URL, or the base URL when the query is empty and the template contains "%s".</returns>
    public static string BuildSearchUrl(string searchQuery, string urlTemplate)
    {
        return 
        string.IsNullOrWhiteSpace(searchQuery)
        ? urlTemplate.Contains("%s") 
            ? GetBaseUrl(urlTemplate)
            : urlTemplate
        : urlTemplate.Replace("%s", Uri.EscapeDataString(searchQuery));
    }

    /// <summary>
    /// Removes an incognito flag from the start or end of a URL input.
    /// </summary>
    /// <param name="rawInput">Raw URL input entered by the user.</param>
    /// <returns>The cleaned URL and whether the incognito flag was present.</returns>
    public static (string URL, bool isIncognitoMentioned) ParseURL(string rawInput)
    {
        bool isIncognitoMentioned = rawInput.StartsWith("-i") || rawInput.EndsWith(" -i");
        string URL = isIncognitoMentioned ?
            rawInput.StartsWith("-i") ? rawInput[2..].Trim() : rawInput[..^3].Trim()
            : rawInput;
        return (URL, isIncognitoMentioned);
    }

    /// <summary>
    /// Normalizes bare domains to HTTPS and returns true only for HTTP/HTTPS URLs.
    /// </summary>
    /// <param name="input">URL or domain text to normalize.</param>
    /// <param name="normalized">Normalized URL when parsing succeeds.</param>
    /// <returns>True when the input can be opened as a web URL; otherwise false.</returns>
    public static bool TryNormalizeWebUrl(string input, out string normalized)
    {
        normalized = input.Trim();

        if (!Uri.TryCreate(normalized, UriKind.Absolute, out var uri))
        {
            normalized = "https://" + normalized;
            return Uri.TryCreate(normalized, UriKind.Absolute, out uri)
                && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps);
        }

        return uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps;
    }

    public static string GetCopyUrlForMultiEngineSearch(string searchQuery, List<string> targetEngines, IReadOnlyDictionary<string,string> engines)
    {
        return string.Join(", ", targetEngines.Select(engine => BuildSearchUrl(searchQuery,engines[engine])));
    }
}
