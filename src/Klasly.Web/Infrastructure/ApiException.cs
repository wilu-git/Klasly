namespace Klasly.Web.Infrastructure;

/// <summary>A non-success answer from the Web API, with its status code, message and field errors.</summary>
public class ApiException(int statusCode, string message, IReadOnlyDictionary<string, string[]> errors)
    : Exception(message)
{
    public int StatusCode { get; } = statusCode;

    public IReadOnlyDictionary<string, string[]> Errors { get; } = errors;

    /// <summary>400 or 409: the user's input was rejected, so show it on the form instead of an error page.</summary>
    public bool IsFormError => StatusCode is 400 or 409;
}
