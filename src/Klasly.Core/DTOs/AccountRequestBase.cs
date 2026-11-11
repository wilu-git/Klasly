using System.ComponentModel.DataAnnotations;

namespace Klasly.Core.DTOs;

/// <summary>
/// Fields shared by create and update account requests. Profile fields apply only to the matching role
/// (Teacher: EmployeeNo/Department, Student: StudentNumber/YearLevel/Program, Parent: ContactEmail/ContactNumber);
/// the service checks the required ones and ignores the rest.
/// </summary>
public abstract class AccountRequestBase
{
    [Required, MaxLength(150)]
    public string FullName { get; init; } = string.Empty;

    [Required, EmailAddress, MaxLength(256)]
    public string Email { get; init; } = string.Empty;

    [MaxLength(20)]
    public string? StudentNumber { get; init; }

    [Range(1, 6)]
    public int? YearLevel { get; init; }

    [MaxLength(50)]
    public string? Program { get; init; }

    [MaxLength(20)]
    public string? EmployeeNo { get; init; }

    [MaxLength(100)]
    public string? Department { get; init; }

    [EmailAddress, MaxLength(256)]
    public string? ContactEmail { get; init; }

    [MaxLength(20)]
    public string? ContactNumber { get; init; }
}
