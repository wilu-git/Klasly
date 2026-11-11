using Klasly.Web.Models.Api;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Klasly.Web.ViewModels;

/// <summary>A parent's linked children plus the active students that can still be linked.</summary>
public record ParentLinksViewModel(int ParentUserId, List<LinkedStudentResponse> Linked, List<SelectListItem> StudentOptions);
