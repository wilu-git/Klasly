using System.ComponentModel.DataAnnotations;

namespace Klasly.Web.ViewModels;

/// <summary>
/// Create/edit form for an account. Property names match the API request so its field errors land next to
/// the right input. Which role fields are required is decided by the API, not repeated here.
/// </summary>
public class AccountFormViewModel
{
    public int? Id { get; set; }

    [Required]
    public string Role { get; set; } = string.Empty;

    [Required(ErrorMessage = "Enter the full name."), StringLength(150), Display(Name = "Full name")]
    public string FullName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Enter the email."), EmailAddress(ErrorMessage = "Enter a valid email address."), StringLength(256)]
    public string Email { get; set; } = string.Empty;

    // Create only. Required is left to the API, whose "Password" error shows next to this field.
    [StringLength(100, MinimumLength = 8, ErrorMessage = "Use at least 8 characters."), DataType(DataType.Password)]
    public string? Password { get; set; }

    // Edit only.
    [Display(Name = "Account is active")]
    public bool IsActive { get; set; } = true;

    [StringLength(100, MinimumLength = 8, ErrorMessage = "Use at least 8 characters."), DataType(DataType.Password), Display(Name = "New password")]
    public string? NewPassword { get; set; }

    // Student
    [StringLength(20), Display(Name = "Student number")]
    public string? StudentNumber { get; set; }

    [Range(1, 6, ErrorMessage = "Year level must be from 1 to 6."), Display(Name = "Year level")]
    public int? YearLevel { get; set; }

    [StringLength(50)]
    public string? Program { get; set; }

    // Teacher
    [StringLength(20), Display(Name = "Employee no.")]
    public string? EmployeeNo { get; set; }

    [StringLength(100)]
    public string? Department { get; set; }

    // Parent
    [EmailAddress(ErrorMessage = "Enter a valid email address."), StringLength(256), Display(Name = "Contact email")]
    public string? ContactEmail { get; set; }

    [StringLength(20), Display(Name = "Contact number")]
    public string? ContactNumber { get; set; }

    /// <summary>Shown under the form on a parent's Edit page; not posted back.</summary>
    public ParentLinksViewModel? ParentLinks { get; set; }
}
