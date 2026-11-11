namespace Klasly.API.Services;

/// <summary>
/// Thrown by a service when input breaks a rule and the error belongs to a field or a row, e.g.
/// <c>weightPercent</c> or <c>grades[2].score</c>. Becomes a 400 validation problem with an <c>errors</c> object,
/// so the website can show each message next to its input.
/// </summary>
public class FieldValidationException(IDictionary<string, string[]> errors)
    : Exception(errors.Values.SelectMany(m => m).FirstOrDefault() ?? "Invalid input.")
{
    public IDictionary<string, string[]> Errors { get; } = errors;

    public FieldValidationException(string field, string message) : this(new Dictionary<string, string[]> { [field] = [message] }) { }
}
