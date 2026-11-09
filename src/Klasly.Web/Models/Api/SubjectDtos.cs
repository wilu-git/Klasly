namespace Klasly.Web.Models.Api;

public record SubjectRequest(string Code, string Title, int Units);

public record SubjectResponse(int SubjectId, string Code, string Title, int Units);
