using Klasly.Web.Models.Api;

namespace Klasly.Web.ViewModels;

/// <summary>The session page: the session's details (and, once it has been opened, its QR and roster).</summary>
public record SessionPageViewModel(SessionResponse Session);
