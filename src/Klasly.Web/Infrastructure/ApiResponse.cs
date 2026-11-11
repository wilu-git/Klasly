using System.Net.Http.Json;
using Klasly.Web.Models.Api;

namespace Klasly.Web.Infrastructure;

/// <summary>Turns an API response into data, or into an <see cref="ApiException"/> carrying the API's message.</summary>
public static class ApiResponse
{
    public static async Task<T> GetApiAsync<T>(this HttpClient http, string url, CancellationToken ct)
    {
        using var response = await http.GetAsync(url, ct);
        return await ReadAsync<T>(response, ct);
    }

    public static async Task<T> PostApiAsync<T>(this HttpClient http, string url, object? body, CancellationToken ct)
    {
        using var response = await http.PostAsJsonAsync(url, body, ct);
        return await ReadAsync<T>(response, ct);
    }

    public static async Task<T> PutApiAsync<T>(this HttpClient http, string url, object body, CancellationToken ct)
    {
        using var response = await http.PutAsJsonAsync(url, body, ct);
        return await ReadAsync<T>(response, ct);
    }

    public static async Task DeleteApiAsync(this HttpClient http, string url, CancellationToken ct)
    {
        using var response = await http.DeleteAsync(url, ct);
        await EnsureAsync(response, ct);
    }

    public static async Task<T> ReadAsync<T>(HttpResponseMessage response, CancellationToken ct)
    {
        await EnsureAsync(response, ct);
        return await response.Content.ReadFromJsonAsync<T>(ct)
            ?? throw new ApiException((int)response.StatusCode, "The server sent an empty response.", NoErrors);
    }

    public static async Task EnsureAsync(HttpResponseMessage response, CancellationToken ct)
    {
        if (response.IsSuccessStatusCode) return;

        // Some answers have no body (e.g. a 401 from the JWT check), so only parse JSON bodies.
        var problem = response.Content.Headers.ContentType?.MediaType is "application/problem+json" or "application/json"
            ? await response.Content.ReadFromJsonAsync<ProblemDetailsDto>(ct)
            : null;

        var status = (int)response.StatusCode;
        throw new ApiException(
            status,
            problem?.Detail
                ?? problem?.Errors?.Values.SelectMany(messages => messages).FirstOrDefault()
                ?? problem?.Title
                ?? $"The server answered with status {status}.",
            problem?.Errors ?? NoErrors);
    }

    private static readonly Dictionary<string, string[]> NoErrors = [];
}
