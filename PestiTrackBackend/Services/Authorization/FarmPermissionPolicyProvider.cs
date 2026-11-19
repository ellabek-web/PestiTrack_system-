using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;

namespace PestiTrackBackend.Security.Authorization;

public sealed class FarmPermissionPolicyProvider
    : IAuthorizationPolicyProvider
{
    private const string PolicyPrefix = "FarmPermission:";

    private readonly DefaultAuthorizationPolicyProvider _fallbackProvider;

    public FarmPermissionPolicyProvider(
        IOptions<AuthorizationOptions> options)
    {
        _fallbackProvider =
            new DefaultAuthorizationPolicyProvider(options);
    }

    public Task<AuthorizationPolicy?> GetPolicyAsync(string policyName)
    {
        if (policyName.StartsWith(
                PolicyPrefix,
                StringComparison.OrdinalIgnoreCase))
        {
            var permissionCode = policyName[PolicyPrefix.Length..];

            if (string.IsNullOrWhiteSpace(permissionCode))
            {
                return Task.FromResult<AuthorizationPolicy?>(null);
            }

            var policy = new AuthorizationPolicyBuilder()
                .RequireAuthenticatedUser()
                .AddRequirements(
                    new FarmPermissionRequirement(permissionCode))
                .Build();

            return Task.FromResult<AuthorizationPolicy?>(policy);
        }

        return _fallbackProvider.GetPolicyAsync(policyName);
    }

    public Task<AuthorizationPolicy> GetDefaultPolicyAsync()
    {
        return _fallbackProvider.GetDefaultPolicyAsync();
    }

    public Task<AuthorizationPolicy?> GetFallbackPolicyAsync()
    {
        return _fallbackProvider.GetFallbackPolicyAsync();
    }
}