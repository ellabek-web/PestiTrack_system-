namespace PestiTrackBackend.Services.Authorization;

public interface IFarmAuthorizationService
{
    Task<bool> HasPermissionAsync(
        Guid userId,
        Guid farmId,
        string permissionCode,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<string>> GetPermissionsAsync(
        Guid userId,
        Guid farmId,
        CancellationToken cancellationToken = default);
}