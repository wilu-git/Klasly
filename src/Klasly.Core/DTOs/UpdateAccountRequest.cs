using System.ComponentModel.DataAnnotations;

namespace Klasly.Core.DTOs;

/// <summary>Body of <c>PUT /api/accounts/{userId}</c>. No role change; password only replaced when <see cref="NewPassword"/> is sent.</summary>
public class UpdateAccountRequest : AccountRequestBase
{
    // Nullable + Required so leaving it out is a 400 instead of silently deactivating the account.
    [Required]
    public bool? IsActive { get; init; }

    [MinLength(8)]
    public string? NewPassword { get; init; }
}
