using Klasly.Web.Infrastructure;
using Klasly.Web.Models.Api;
using Klasly.Web.Repositories.Interfaces;
using Klasly.Web.Services.Interfaces;
using Klasly.Web.ViewModels;

namespace Klasly.Web.Services;

/// <summary>Shapes the Teacher pages from the session API calls. No business rules here (the API decides).</summary>
public class SessionService(ISessionRepository sessions) : ISessionService
{
    public Task<List<ClassResponse>> GetMyClassesAsync(CancellationToken ct) => sessions.ListMyClassesAsync(ct);

    public async Task<TeacherClassViewModel> GetClassAsync(int classId, CancellationToken ct)
    {
        // Sessions first: the API answers 403/404 for a class that isn't this teacher's, before we look for the header.
        var list = await sessions.ListByClassAsync(classId, ct);
        var header = (await sessions.ListMyClassesAsync(ct)).FirstOrDefault(c => c.ClassId == classId)
            ?? throw new ApiException(StatusCodes.Status404NotFound, "Class not found.", new Dictionary<string, string[]>());

        var now = ManilaTime.Now;
        var end = now.AddMinutes(90);
        return new TeacherClassViewModel(header, list)
        {
            // Whole minutes, so the datetime-local input shows no seconds.
            Start = new StartSessionViewModel { ClassId = classId, EndTime = end.AddTicks(-(end.Ticks % TimeSpan.TicksPerMinute)) }
        };
    }

    public async Task<(int SessionId, string? OpenError)> StartAsync(StartSessionViewModel model, CancellationToken ct)
    {
        var created = await sessions.CreateAsync(model.ClassId,
            new CreateSessionRequest(DateTimeOffset.UtcNow, ManilaTime.ToOffset(model.EndTime!.Value), model.LateAfterMinutes!.Value), ct);

        // The session exists now; if opening fails it stays Scheduled and its page shows an Open button (plan decision 6).
        try
        {
            await sessions.OpenAsync(created.SessionId, ct);
            return (created.SessionId, null);
        }
        catch (ApiException ex) when (ex.IsFormError)
        {
            return (created.SessionId, ex.Message);
        }
    }

    public async Task<SessionPageViewModel> GetSessionAsync(int sessionId, CancellationToken ct) =>
        new(await sessions.GetAsync(sessionId, ct));

    public Task OpenAsync(int sessionId, CancellationToken ct) => sessions.OpenAsync(sessionId, ct);

    public Task CloseAsync(int sessionId, CancellationToken ct) => sessions.CloseAsync(sessionId, ct);
}
