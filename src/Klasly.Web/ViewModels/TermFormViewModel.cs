using System.ComponentModel.DataAnnotations;

namespace Klasly.Web.ViewModels;

/// <summary>Create/edit form for a term. <see cref="Id"/> is null when creating.</summary>
public class TermFormViewModel
{
    public int? Id { get; set; }

    [Required(ErrorMessage = "Enter the term name."), StringLength(50)]
    public string Name { get; set; } = string.Empty;

    // DataType.Date makes the tag helper render <input type="date"> with a yyyy-MM-dd value.
    [Required(ErrorMessage = "Pick the start date."), Display(Name = "Start date"), DataType(DataType.Date)]
    public DateOnly? StartDate { get; set; }

    [Required(ErrorMessage = "Pick the end date."), Display(Name = "End date"), DataType(DataType.Date)]
    public DateOnly? EndDate { get; set; }
}
