namespace PestiTrackBackend.DTOs.Users;

public sealed class UserListResponse
{
    public int Page { get; set; }

    public int PageSize { get; set; }

    public int TotalCount { get; set; }

    public int TotalPages { get; set; }

    public List<UserResponse> Items { get; set; } = [];
}

public sealed class UserResponse
{
    public Guid Id { get; set; }

    public string DisplayName { get; set; } = string.Empty;

    public string? Email { get; set; }

    public bool EmailConfirmed { get; set; }

    public bool IsActive { get; set; }

    public bool IsLockedOut { get; set; }

    public DateTimeOffset? LockoutEnd { get; set; }

    public int AccessFailedCount { get; set; }

    public DateTimeOffset CreatedAtUtc { get; set; }

    public List<string> SystemRoles { get; set; } = [];
}
