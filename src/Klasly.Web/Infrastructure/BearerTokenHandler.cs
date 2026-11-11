using System.Net.Http.Headers;
using Microsoft.AspNetCore.Authentication;

namespace Klasly.Web.Infrastructure;

/// <summary>Adds the signed-in user's JWT (stored inside the auth cookie) to every Web API call.</summary>
public class BearerTokenHandler(IHttpContextAccessor accessor) : DelegatingHandler
{
    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
    {
        if (accessor.HttpContext is { } context && await context.GetTokenAsync("access_token") is { } token)
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        return await base.SendAsync(request, ct);
    }
}
