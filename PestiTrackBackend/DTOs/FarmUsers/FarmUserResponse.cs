namespace PestiTrackBackend.DTOs.FarmUsers;

public sealed class FarmUserResponse
{
    public Guid Id { get; set; }

    public Guid FarmId { get; set; }

    public Guid UserId { get; set; }

    public string DisplayName { get; set; } = string.Empty;

    public string? Email { get; set; }

    public bool IsActive { get; set; }

    public DateTime AssignedAtUtc { get; set; }

    public List<FarmUserRoleResponse> Roles { get; set; } = [];
}

public sealed class FarmUserRoleResponse
{
    public Guid Id { get; set; }

    public string Name { get; set; } = string.Empty;
}