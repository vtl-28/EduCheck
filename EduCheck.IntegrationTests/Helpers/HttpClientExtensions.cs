using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace EduCheck.IntegrationTests.Helpers;

public static class HttpClientExtensions
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    /// <summary>
    /// Set Bearer token for authenticated requests
    /// </summary>
    public static void SetBearerToken(this HttpClient client, string token)
    {
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", token);
    }

    /// <summary>
    /// Clear authentication header
    /// </summary>
    public static void ClearAuthentication(this HttpClient client)
    {
        client.DefaultRequestHeaders.Authorization = null;
    }

    /// <summary>
    /// POST JSON data
    /// </summary>
    public static async Task<HttpResponseMessage> PostAsJsonAsync<T>(
        this HttpClient client,
        string url,
        T data)
    {
        var json = JsonSerializer.Serialize(data);
        var content = new StringContent(json, Encoding.UTF8, "application/json");
        return await client.PostAsync(url, content);
    }

    /// <summary>
    /// GET and deserialize JSON response
    /// </summary>
    public static async Task<T?> GetFromJsonAsync<T>(
        this HttpClient client,
        string url)
    {
        var response = await client.GetAsync(url);
        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<T>(json, JsonOptions);
    }

    /// <summary>
    /// Read response body as JSON
    /// </summary>
    public static async Task<T?> ReadAsJsonAsync<T>(this HttpResponseMessage response)
    {
        var json = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<T>(json, JsonOptions);
    }
}