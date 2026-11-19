using Microsoft.AspNetCore.Authorization;

namespace PestiTrackBackend.Security.Authorization;

public sealed class FarmPermissionRequirement : IAuthorizationRequirement
{
    public string PermissionCode { get; }

    public FarmPermissionRequirement(string permissionCode)
    {
        PermissionCode = permissionCode;
    }
}