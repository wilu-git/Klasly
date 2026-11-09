using System.ComponentModel.DataAnnotations;
using Klasly.Web.Models.Api;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Klasly.Web.ViewModels;

/// <summary>Class list with the term filter. <see cref="SelectedTermId"/> 0 = all terms.</summary>
public record ClassListViewModel(List<ClassResponse> Classes, List<SelectListItem> TermOptions, int SelectedTermId);

/// <summary>Create/edit form for a class. <see cref="Id"/> is null when creating. Option lists are not posted back.</summary>
public class ClassFormViewModel
{
    public int? Id { get; set; }

    [Required(ErrorMessage = "Pick a subject."), Display(Name = "Subject")]
    public int? SubjectId { get; set; }

    [Required(ErrorMessage = "Pick a teacher."), Display(Name = "Teacher")]
    public int? TeacherId { get; set; }

    [Required(ErrorMessage = "Pick a term."), Display(Name = "Term")]
    public int? TermId { get; set; }

    [Required(ErrorMessage = "Enter the section."), StringLength(30)]
    public string Section { get; set; } = string.Empty;

    [StringLength(100)]
    public string? Schedule { get; set; }

    [StringLength(30)]
    public string? Room { get; set; }

    public List<SelectListItem> SubjectOptions { get; set; } = [];
    public List<SelectListItem> TeacherOptions { get; set; } = [];
    public List<SelectListItem> TermOptions { get; set; } = [];
}

/// <summary>A class with its enrollments and the active students who can still be enrolled.</summary>
public record ClassDetailsViewModel(ClassResponse Class, List<EnrollmentResponse> Enrollments, List<SelectListItem> StudentOptions);
