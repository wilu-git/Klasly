using Klasly.Web.Models.Api;

namespace Klasly.Web.ViewModels;

/// <summary>A parent's page for one child, plus the other linked children for the switcher.</summary>
public record ChildOverviewViewModel(StudentOverviewResponse Overview, List<LinkedStudentResponse> Children);
