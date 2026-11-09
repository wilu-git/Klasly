using System.ComponentModel.DataAnnotations;

namespace Klasly.Web.ViewModels;

/// <summary>Create/edit form for a subject. <see cref="Id"/> is null when creating.</summary>
public class SubjectFormViewModel
{
    public int? Id { get; set; }

    [Required(ErrorMessage = "Enter the subject code."), StringLength(20)]
    public string Code { get; set; } = string.Empty;

    [Required(ErrorMessage = "Enter the subject title."), StringLength(150)]
    public string Title { get; set; } = string.Empty;

    [Range(1, 10, ErrorMessage = "Units must be from 1 to 10.")]
    public int Units { get; set; } = 3;
}
