using Klasly.Web.Models.Api;

namespace Klasly.Web.ViewModels;

/// <summary>The session page: its details, plus the QR and roster once it has been opened (<see cref="Live"/> null while Scheduled).</summary>
public record SessionPageViewModel(SessionResponse Session, SessionLiveViewModel? Live);
