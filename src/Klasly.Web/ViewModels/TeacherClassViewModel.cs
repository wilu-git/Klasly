using Klasly.Web.Models.Api;

namespace Klasly.Web.ViewModels;

/// <summary>A teacher's class page: the class header, the Start session form and its sessions, newest first.</summary>
public record TeacherClassViewModel(ClassResponse Class, List<SessionResponse> Sessions)
{
    public StartSessionViewModel Start { get; init; } = new();
}
