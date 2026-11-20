using System.Reflection;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace Klasly.Web.Infrastructure;

public static class ModelStateExtensions
{
    /// <summary>
    /// Shows an API 400/409 on the form: an error whose key is a property of <paramref name="model"/> goes next to
    /// that input (view models use the API's property names), anything else in the summary box at the top.
    /// </summary>
    public static void AddApiErrors(this ModelStateDictionary modelState, ApiException ex, object model)
    {
        var type = model.GetType();
        foreach (var (key, messages) in ex.Errors)
        {
            var isField = type.GetProperty(key, BindingFlags.IgnoreCase | BindingFlags.Public | BindingFlags.Instance) is not null;
            foreach (var message in messages)
                modelState.AddModelError(isField ? key : string.Empty, message);
        }

        if (ex.Errors.Count == 0) modelState.AddModelError(string.Empty, ex.Message);
    }

    /// <summary>
    /// Moves summary errors (key "") under <paramref name="key"/>. A page with two forms (the teacher class page)
    /// then shows each error only in the form that was posted, instead of in both "ModelOnly" summaries.
    /// </summary>
    public static void MoveSummaryErrorsTo(this ModelStateDictionary modelState, string key)
    {
        if (!modelState.TryGetValue(string.Empty, out var entry) || entry.Errors.Count == 0) return;
        foreach (var error in entry.Errors.ToList())
            modelState.AddModelError(key, error.ErrorMessage);
        modelState.Remove(string.Empty);
    }
}
