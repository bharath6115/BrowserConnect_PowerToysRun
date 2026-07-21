using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace Community.PowerToys.Run.Plugin.BrowserConnect.Providers.GraphQL;

public static class GraphQLClient
{
    private static readonly HttpClient _httpClient = CreateHttpClient();
    private static readonly JsonSerializerOptions _options = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private static HttpClient CreateHttpClient()
    {
        var client = new HttpClient();
        client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        return client;
    }

    public static async Task<T?> QueryAsync<T>(string endpoint, string graphQLQuery, object variables)
    {
        var response = await _httpClient.PostAsJsonAsync(endpoint, new
        {
            query = graphQLQuery,
            variables
        });

        response.EnsureSuccessStatusCode();

        return await response.Content.ReadFromJsonAsync<T>(_options);
    }
}