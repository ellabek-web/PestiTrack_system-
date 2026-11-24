using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PestiTrackBackend.Data;
using PestiTrackBackend.DTOs.Users;
using PestiTrackBackend.Models.Identity;
using PestiTrackBackend.Security;

namespace PestiTrackBackend.Controllers;

[ApiController]
[Route("api/admin/users")]
[Authorize(Roles = SystemRoleCodes.Administrator)]
public sealed class UserAdministrationController : ControllerBase
{
    private static readonly string[] AllowedSystemRoles =
    [
        SystemRoleCodes.Administrator,
        SystemRoleCodes.Auditor
    ];

    private readonly AppDbContext _db;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly TimeProvider _timeProvider;

    public UserAdministrationController(
        AppDbContext db,
        UserManager<ApplicationUser> userManager,
        TimeProvider timeProvider)
    {
        _db = db;
        _userManager = userManager;
        _timeProvider = timeProvider;
    }

    // GET: api/admin/users
    [HttpGet]
    public async Task<ActionResult<UserListResponse>> GetUsers(
        [FromQuery] UserQueryRequest request,
        CancellationToken cancellationToken)
    {
        var query = _db.Users.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var search = request.Search.Trim();
            query = query.Where(user =>
                user.DisplayName.Contains(search) ||
                (user.Email != null &&
                    user.Email.Contains(search)));
        }

        if (request.IsActive.HasValue)
        {
            query = query.Where(user =>
                user.IsActive == request.IsActive.Value);
        }

        if (!string.IsNullOrWhiteSpace(request.SystemRole))
        {
            var role = CanonicalRole(request.SystemRole);

            if (role is null)
            {
                return BadRequest(new
                {
                    message = "The system role filter is invalid."
                });
            }

            query = query.Where(user =>
                _db.UserRoles
                    .Where(userRole =>
                        userRole.UserId == user.Id)
                    .Join(
                        _db.Roles,
                        userRole => userRole.RoleId,
                        systemRole => systemRole.Id,
                        (_, systemRole) => systemRole.Name)
                    .Any(name => name == role));
        }

        var totalCount = await query.CountAsync(
            cancellationToken);
        var now = _timeProvider.GetUtcNow();

        var users = await query
            .OrderBy(user => user.DisplayName)
            .ThenBy(user => user.Email)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(user => new UserResponse
            {
                Id = user.Id,
                DisplayName = user.DisplayName,
                Email = user.Email,
                EmailConfirmed = user.EmailConfirmed,
                IsActive = user.IsActive,
                IsLockedOut =
                    user.LockoutEnd.HasValue &&
                    user.LockoutEnd.Value > now,
                LockoutEnd = user.LockoutEnd,
                AccessFailedCount = user.AccessFailedCount,
                CreatedAtUtc = new DateTimeOffset(
                    DateTime.SpecifyKind(
                        user.CreatedAtUtc,
                        DateTimeKind.Utc)),
                SystemRoles = _db.UserRoles
                    .Where(userRole =>
                        userRole.UserId == user.Id)
                    .Join(
                        _db.Roles,
                        userRole => userRole.RoleId,
                        systemRole => systemRole.Id,
                        (_, systemRole) => systemRole.Name!)
                    .OrderBy(name => name)
                    .ToList()
            })
            .ToListAsync(cancellationToken);

        return Ok(new UserListResponse
        {
            Page = request.Page,
            PageSize = request.PageSize,
            TotalCount = totalCount,
            TotalPages = (int)Math.Ceiling(
                totalCount / (double)request.PageSize),
            Items = users
        });
    }

    // GET: api/admin/users/{userId}
    [HttpGet("{userId:guid}")]
    public async Task<ActionResult<UserResponse>> GetById(
        Guid userId,
        CancellationToken cancellationToken)
    {
        var response = await FindResponse(
            userId,
            cancellationToken);

        return response is null
            ? NotFound(new { message = "User not found." })
            : Ok(response);
    }

    // POST: api/admin/users
    [HttpPost]
    public async Task<ActionResult<UserResponse>> Create(
        CreateUserRequest request,
        CancellationToken cancellationToken)
    {
        var roles = ValidateRoles(request.SystemRoles);

        if (roles is null)
        {
            return BadRequest(new
            {
                message = "One or more system roles are invalid or duplicated."
            });
        }

        var email = request.Email.Trim().ToLowerInvariant();
        var user = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            UserName = email,
            Email = email,
            DisplayName = request.DisplayName.Trim(),
            IsActive = true,
            EmailConfirmed = false,
            CreatedAtUtc = _timeProvider.GetUtcNow().UtcDateTime
        };

        var createResult = await _userManager.CreateAsync(
            user,
            request.Password);

        if (!createResult.Succeeded)
            return IdentityFailure(createResult);

        if (roles.Count > 0)
        {
            var roleEntities = await _db.Roles
                .Where(role =>
                    role.Name != null &&
                    roles.Contains(role.Name))
                .ToListAsync(cancellationToken);

            if (roleEntities.Count != roles.Count)
            {
                await _userManager.DeleteAsync(user);
                return StatusCode(
                    StatusCodes.Status500InternalServerError,
                    new
                    {
                        message = "A configured system role is missing."
                    });
            }

            _db.UserRoles.AddRange(
                roleEntities.Select(role =>
                    new IdentityUserRole<Guid>
                    {
                        UserId = user.Id,
                        RoleId = role.Id
                    }));
            await _db.SaveChangesAsync(cancellationToken);
        }

        var response = await FindResponse(
            user.Id,
            cancellationToken);

        return CreatedAtAction(
            nameof(GetById),
            new { userId = user.Id },
            response);
    }

    // PUT: api/admin/users/{userId}
    [HttpPut("{userId:guid}")]
    public async Task<ActionResult<UserResponse>> Update(
        Guid userId,
        UpdateUserRequest request,
        CancellationToken cancellationToken)
    {
        var user = await _userManager.FindByIdAsync(
            userId.ToString());

        if (user is null)
            return NotFound(new { message = "User not found." });

        var email = request.Email.Trim().ToLowerInvariant();
        user.DisplayName = request.DisplayName.Trim();
        user.Email = email;
        user.NormalizedEmail =
            _userManager.NormalizeEmail(email);
        user.UserName = email;
        user.NormalizedUserName =
            _userManager.NormalizeName(email);

        var result = await _userManager.UpdateAsync(user);

        if (!result.Succeeded)
            return IdentityFailure(result);

        return Ok(await FindResponse(
            user.Id,
            cancellationToken));
    }

    // PATCH: api/admin/users/{userId}/status
    [HttpPatch("{userId:guid}/status")]
    public async Task<IActionResult> UpdateStatus(
        Guid userId,
        UpdateUserStatusRequest request,
        CancellationToken cancellationToken)
    {
        var user = await _userManager.FindByIdAsync(
            userId.ToString());

        if (user is null)
            return NotFound(new { message = "User not found." });

        var currentUserId = CurrentUserId();

        if (!request.IsActive &&
            currentUserId == user.Id)
        {
            return Conflict(new
            {
                message = "You cannot deactivate your own account."
            });
        }

        if (!request.IsActive &&
            user.IsActive &&
            await _userManager.IsInRoleAsync(
                user,
                SystemRoleCodes.Administrator) &&
            await ActiveAdministratorCount(
                cancellationToken) <= 1)
        {
            return Conflict(new
            {
                message = "The last active system administrator cannot be deactivated."
            });
        }

        user.IsActive = request.IsActive;

        var result =
            await _userManager.UpdateSecurityStampAsync(user);

        if (!result.Succeeded)
            return IdentityFailure(result);

        return Ok(await FindResponse(
            user.Id,
            cancellationToken));
    }

    // PUT: api/admin/users/{userId}/system-roles
    [HttpPut("{userId:guid}/system-roles")]
    public async Task<IActionResult> UpdateSystemRoles(
        Guid userId,
        UpdateUserSystemRolesRequest request,
        CancellationToken cancellationToken)
    {
        var roles = ValidateRoles(request.SystemRoles);

        if (roles is null)
        {
            return BadRequest(new
            {
                message = "One or more system roles are invalid or duplicated."
            });
        }

        var user = await _userManager.FindByIdAsync(
            userId.ToString());

        if (user is null)
            return NotFound(new { message = "User not found." });

        var currentRoles = await _userManager.GetRolesAsync(user);
        var removesAdministrator =
            currentRoles.Contains(
                SystemRoleCodes.Administrator) &&
            !roles.Contains(
                SystemRoleCodes.Administrator);

        if (removesAdministrator &&
            CurrentUserId() == user.Id)
        {
            return Conflict(new
            {
                message = "You cannot remove your own administrator role."
            });
        }

        if (removesAdministrator &&
            user.IsActive &&
            await ActiveAdministratorCount(
                cancellationToken) <= 1)
        {
            return Conflict(new
            {
                message = "The last active system administrator cannot lose the administrator role."
            });
        }

        var roleEntities = await _db.Roles
            .Where(role =>
                role.Name != null &&
                roles.Contains(role.Name))
            .ToListAsync(cancellationToken);

        if (roleEntities.Count != roles.Count)
        {
            return StatusCode(
                StatusCodes.Status500InternalServerError,
                new
                {
                    message = "A configured system role is missing."
                });
        }

        var existingAssignments = await _db.UserRoles
            .Where(userRole =>
                userRole.UserId == user.Id)
            .ToListAsync(cancellationToken);

        _db.UserRoles.RemoveRange(existingAssignments);
        _db.UserRoles.AddRange(
            roleEntities.Select(role =>
                new IdentityUserRole<Guid>
                {
                    UserId = user.Id,
                    RoleId = role.Id
                }));
        await _db.SaveChangesAsync(cancellationToken);

        var stampResult =
            await _userManager.UpdateSecurityStampAsync(user);

        if (!stampResult.Succeeded)
            return IdentityFailure(stampResult);

        return Ok(await FindResponse(
            user.Id,
            cancellationToken));
    }

    // POST: api/admin/users/{userId}/reset-password
    [HttpPost("{userId:guid}/reset-password")]
    public async Task<IActionResult> ResetPassword(
        Guid userId,
        AdminResetPasswordRequest request)
    {
        var user = await _userManager.FindByIdAsync(
            userId.ToString());

        if (user is null)
            return NotFound(new { message = "User not found." });

        var token =
            await _userManager.GeneratePasswordResetTokenAsync(user);
        var result = await _userManager.ResetPasswordAsync(
            user,
            token,
            request.NewPassword);

        if (!result.Succeeded)
            return IdentityFailure(result);

        await _userManager.ResetAccessFailedCountAsync(user);
        await _userManager.SetLockoutEndDateAsync(
            user,
            null);

        return Ok(new
        {
            message = "Password reset successfully."
        });
    }

    // POST: api/admin/users/{userId}/unlock
    [HttpPost("{userId:guid}/unlock")]
    public async Task<IActionResult> Unlock(Guid userId)
    {
        var user = await _userManager.FindByIdAsync(
            userId.ToString());

        if (user is null)
            return NotFound(new { message = "User not found." });

        var lockoutResult =
            await _userManager.SetLockoutEndDateAsync(
                user,
                null);

        if (!lockoutResult.Succeeded)
            return IdentityFailure(lockoutResult);

        var failedCountResult =
            await _userManager.ResetAccessFailedCountAsync(user);

        if (!failedCountResult.Succeeded)
            return IdentityFailure(failedCountResult);

        return Ok(new
        {
            message = "Account unlocked successfully."
        });
    }

    private async Task<UserResponse?> FindResponse(
        Guid userId,
        CancellationToken cancellationToken)
    {
        var now = _timeProvider.GetUtcNow();

        return await _db.Users
            .AsNoTracking()
            .Where(user => user.Id == userId)
            .Select(user => new UserResponse
            {
                Id = user.Id,
                DisplayName = user.DisplayName,
                Email = user.Email,
                EmailConfirmed = user.EmailConfirmed,
                IsActive = user.IsActive,
                IsLockedOut =
                    user.LockoutEnd.HasValue &&
                    user.LockoutEnd.Value > now,
                LockoutEnd = user.LockoutEnd,
                AccessFailedCount = user.AccessFailedCount,
                CreatedAtUtc = new DateTimeOffset(
                    DateTime.SpecifyKind(
                        user.CreatedAtUtc,
                        DateTimeKind.Utc)),
                SystemRoles = _db.UserRoles
                    .Where(userRole =>
                        userRole.UserId == user.Id)
                    .Join(
                        _db.Roles,
                        userRole => userRole.RoleId,
                        role => role.Id,
                        (_, role) => role.Name!)
                    .OrderBy(name => name)
                    .ToList()
            })
            .FirstOrDefaultAsync(cancellationToken);
    }

    private async Task<int> ActiveAdministratorCount(
        CancellationToken cancellationToken)
    {
        return await _db.Users
            .CountAsync(
                user =>
                    user.IsActive &&
                    _db.UserRoles
                        .Where(userRole =>
                            userRole.UserId == user.Id)
                        .Join(
                            _db.Roles,
                            userRole => userRole.RoleId,
                            role => role.Id,
                            (_, role) => role.Name)
                        .Any(roleName =>
                            roleName ==
                                SystemRoleCodes.Administrator),
                cancellationToken);
    }

    private Guid? CurrentUserId()
    {
        var value = User.FindFirstValue(
            ClaimTypes.NameIdentifier);

        return Guid.TryParse(value, out var userId)
            ? userId
            : null;
    }

    private static List<string>? ValidateRoles(
        IEnumerable<string> suppliedRoles)
    {
        var values = suppliedRoles.ToList();
        var roles = new List<string>();

        foreach (var value in values)
        {
            var role = CanonicalRole(value);

            if (role is null)
                return null;

            roles.Add(role);
        }

        return roles
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Count() == values.Count
                ? roles
                : null;
    }

    private static string? CanonicalRole(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;

        return AllowedSystemRoles.FirstOrDefault(role =>
            string.Equals(
                role,
                value.Trim(),
                StringComparison.OrdinalIgnoreCase));
    }

    private ObjectResult IdentityFailure(IdentityResult result)
    {
        return BadRequest(new
        {
            message = "Identity operation failed.",
            errors = result.Errors.Select(error => new
            {
                error.Code,
                error.Description
            })
        });
    }
}
