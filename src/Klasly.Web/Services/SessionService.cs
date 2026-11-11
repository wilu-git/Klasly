using Klasly.Web.Infrastructure;
using Klasly.Web.Models.Api;
using Klasly.Web.Repositories.Interfaces;
using Klasly.Web.Services.Interfaces;
using Klasly.Web.ViewModels;
using QRCoder;

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

    public async Task<SessionPageViewModel> GetSessionAsync(int sessionId, CancellationToken ct)
    {
        var session = await sessions.GetAsync(sessionId, ct);
        // Scheduled or Cancelled (never opened): no roster. Closed/ended: the final roster, no QR call needed.
        // Corrections only once the API says the session ended (Closed/Finalized), not merely past its end time.
        var live = session.Status is "Scheduled" or "Cancelled"
            ? null
            : await BuildLiveAsync(sessionId, tryQr: session.IsLive, ct, canCorrect: session.Status is "Closed" or "Finalized");
        return new SessionPageViewModel(session, live);
    }

    public Task<SessionLiveViewModel> GetLiveAsync(int sessionId, CancellationToken ct) => BuildLiveAsync(sessionId, tryQr: true, ct);

    public Task<SessionAttendanceRow> CorrectAttendanceAsync(int sessionId, int studentId, string? status, CancellationToken ct) =>
        sessions.CorrectAttendanceAsync(sessionId, studentId, new CorrectAttendanceRequest(status), ct);

    private async Task<SessionLiveViewModel> BuildLiveAsync(int sessionId, bool tryQr, CancellationToken ct, bool canCorrect = false)
    {
        QrTokenResponse? qr = null;
        if (tryQr)
        {
            try { qr = await sessions.GetQrAsync(sessionId, ct); }
            catch (ApiException ex) when (ex.StatusCode == StatusCodes.Status409Conflict) { } // closed or past its end
        }

        var roster = await sessions.GetAttendanceAsync(sessionId, ct);
        return new SessionLiveViewModel(sessionId, qr is null ? null : QrSvg(qr.Token), roster, canCorrect);
    }

    // Drawn on the server so the token never reaches page JavaScript. White background with the quiet zone,
    // so phones can read it whatever the page around it looks like; the viewBox lets CSS size it.
    private static string QrSvg(string token)
    {
        using var generator = new QRCodeGenerator();
        using var data = generator.CreateQrCode(token, QRCodeGenerator.ECCLevel.M);
        return new SvgQRCode(data).GetGraphic(10, "#000000", "#ffffff", drawQuietZones: true, SvgQRCode.SizingMode.ViewBoxAttribute);
    }

    public Task OpenAsync(int sessionId, CancellationToken ct) => sessions.OpenAsync(sessionId, ct);

    public Task CloseAsync(int sessionId, CancellationToken ct) => sessions.CloseAsync(sessionId, ct);
}
