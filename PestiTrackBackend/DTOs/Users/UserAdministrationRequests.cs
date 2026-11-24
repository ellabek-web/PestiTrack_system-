using System.ComponentModel.DataAnnotations;

namespace PestiTrackBackend.DTOs.Users;

public sealed class UserQueryRequest
{
    [StringLength(256)]
    public string? Search { get; set; }

    public bool? IsActive { get; set; }

    [StringLength(100)]
    public string? SystemRole { get; set; }

    [Range(1, int.MaxValue)]
    public int Page { get; set; } = 1;

    [Range(1, 100)]
    public int PageSize { get; set; } = 20;
}

public sealed class CreateUserRequest
{
    [Required]
    [StringLength(150, MinimumLength = 2)]
    public string DisplayName { get; set; } = string.Empty;

    [Required]
    [EmailAddress]
    [StringLength(256)]
    public string Email { get; set; } = string.Empty;

    [Required]
    [StringLength(256, MinimumLength = 8)]
    public string Password { get; set; } = string.Empty;

    public List<string> SystemRoles { get; set; } = [];
}

public sealed class UpdateUserRequest
{
    [Required]
    [StringLength(150, MinimumLength = 2)]
    public string DisplayName { get; set; } = string.Empty;

    [Required]
    [EmailAddress]
    [StringLength(256)]
    public string Email { get; set; } = string.Empty;
}

public sealed class UpdateUserStatusRequest
{
    public bool IsActive { get; set; }
}

public sealed class UpdateUserSystemRolesRequest
{
    public List<string> SystemRoles { get; set; } = [];
}

public sealed class AdminResetPasswordRequest
{
    [Required]
    [StringLength(256, MinimumLength = 8)]
    public string NewPassword { get; set; } = string.Empty;
}

public sealed class ChangePasswordRequest
{
    [Required]
    [StringLength(256)]
    public string CurrentPassword { get; set; } = string.Empty;

    [Required]
    [StringLength(256, MinimumLength = 8)]
    public string NewPassword { get; set; } = string.Empty;
}
