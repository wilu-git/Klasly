using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace Klasly.Web.Infrastructure;

public static class ModelStateExtensions
{
    /// <summary>
    /// Shows an API 400/409 on the form: each field error next to its input (view models use the API's
    /// property names), anything else in the summary box at the top.
    /// </summary>
    public static void AddApiErrors(this ModelStateDictionary modelState, ApiException ex)
    {
        foreach (var (key, messages) in ex.Errors)
            foreach (var message in messages)
                modelState.AddModelError(modelState.ContainsKey(key) ? key : string.Empty, message);

        if (ex.Errors.Count == 0) modelState.AddModelError(string.Empty, ex.Message);
    }
}
