using System.ComponentModel.DataAnnotations;
using Klasly.Core.Entities;

namespace Klasly.Core.DTOs;

/// <summary>Body of <c>POST /api/accounts</c>. The role cannot be changed later.</summary>
public class CreateAccountRequest : AccountRequestBase
{
    [Required, MinLength(8)]
    public string Password { get; init; } = string.Empty;

    [Required]
    public UserRole? Role { get; init; }
}
