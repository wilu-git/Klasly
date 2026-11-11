namespace Klasly.Web.Models.Api;

/// <summary>The parts of an API problem-details body the website shows: a message and per-field errors.</summary>
public record ProblemDetailsDto(string? Title, string? Detail, Dictionary<string, string[]>? Errors);
