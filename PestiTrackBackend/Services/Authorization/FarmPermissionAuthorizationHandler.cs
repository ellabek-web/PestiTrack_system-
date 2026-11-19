using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using PestiTrackBackend.Services.Authorization;

namespace PestiTrackBackend.Security.Authorization;

public sealed class FarmPermissionAuthorizationHandler
    : AuthorizationHandler<FarmPermissionRequirement>
{
    private readonly IFarmAuthorizationService _farmAuthorizationService;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public FarmPermissionAuthorizationHandler(
        IFarmAuthorizationService farmAuthorizationService,
        IHttpContextAccessor httpContextAccessor)
    {
        _farmAuthorizationService = farmAuthorizationService;
        _httpContextAccessor = httpContextAccessor;
    }

    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        FarmPermissionRequirement requirement)
    {
        var httpContext = _httpContextAccessor.HttpContext;

        if (httpContext is null)
        {
            return;
        }

        // Obtain the authenticated user's ID.
        var userIdValue = context.User.FindFirstValue(
            ClaimTypes.NameIdentifier);

        if (!Guid.TryParse(userIdValue, out var userId))
        {
            return;
        }

        // Obtain the farm ID from the route.
        if (!httpContext.Request.RouteValues.TryGetValue(
                "farmId",
                out var farmIdValue) ||
            !Guid.TryParse(
                Convert.ToString(farmIdValue),
                out var farmId))
        {
            return;
        }

        // Check the user's permission within this specific farm.
        var hasPermission =
            await _farmAuthorizationService.HasPermissionAsync(
                userId,
                farmId,
                requirement.PermissionCode,
                httpContext.RequestAborted);

        if (hasPermission)
        {
            context.Succeed(requirement);
        }
    }
}