namespace Klasly.Web.Models.Api;

public record TermRequest(string Name, DateOnly? StartDate, DateOnly? EndDate);

public record TermResponse(int TermId, string Name, DateOnly StartDate, DateOnly EndDate, bool IsActive);
