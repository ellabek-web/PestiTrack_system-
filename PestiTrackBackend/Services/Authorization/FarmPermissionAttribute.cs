using Microsoft.AspNetCore.Authorization;

namespace PestiTrackBackend.Security.Authorization;

public sealed class FarmPermissionAttribute : AuthorizeAttribute
{
    private const string PolicyPrefix = "FarmPermission:";

    public FarmPermissionAttribute(string permissionCode)
    {
        if (string.IsNullOrWhiteSpace(permissionCode))
        {
            throw new ArgumentException(
                "Permission code is required.",
                nameof(permissionCode));
        }

        Policy = $"{PolicyPrefix}{permissionCode}";
    }
}