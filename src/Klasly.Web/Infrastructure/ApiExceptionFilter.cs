using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.ViewFeatures;

namespace Klasly.Web.Infrastructure;

/// <summary>
/// The one place that turns API failures into pages: 401 → log in again, 403 → access denied,
/// 404 → not found, API unreachable → friendly error page. 400/409 are handled on the forms.
/// </summary>
public class ApiExceptionFilter(ILogger<ApiExceptionFilter> logger, ITempDataDictionaryFactory tempDataFactory)
    : IAsyncExceptionFilter
{
    public async Task OnExceptionAsync(ExceptionContext context)
    {
        var http = context.HttpContext;

        switch (context.Exception)
        {
            case ApiException { StatusCode: StatusCodes.Status401Unauthorized }:
                await http.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                tempDataFactory.GetTempData(http)["Error"] = "Your session ended, please log in again.";
                // Only GET pages can be reopened safely after logging in again.
                var returnUrl = HttpMethods.IsGet(http.Request.Method)
                    ? $"{http.Request.PathBase}{http.Request.Path}{http.Request.QueryString}"
                    : null;
                context.Result = new RedirectToActionResult("Login", "Account", new { area = "", returnUrl });
                break;

            case ApiException { StatusCode: StatusCodes.Status403Forbidden } forbidden:
                tempDataFactory.GetTempData(http)["Error"] = forbidden.Message; // the API's reason, e.g. "You can only view your own children"
                context.Result = new RedirectToActionResult("AccessDenied", "Account", new { area = "" });
                break;

            case ApiException { StatusCode: StatusCodes.Status404NotFound }:
                context.Result = new NotFoundResult();
                break;

            case HttpRequestException or TaskCanceledException when !http.RequestAborted.IsCancellationRequested:
                logger.LogError(context.Exception, "The Klasly Web API could not be reached.");
                context.Result = new ViewResult { ViewName = "ApiUnavailable", StatusCode = StatusCodes.Status503ServiceUnavailable };
                break;

            default:
                return; // anything else stays a normal 500
        }

        context.ExceptionHandled = true;
    }
}
