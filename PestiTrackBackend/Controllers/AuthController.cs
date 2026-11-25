
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using PestiTrackBackend.DTOs.Auth;
using PestiTrackBackend.DTOs.Users;
using PestiTrackBackend.Models.Identity;
using PestiTrackBackend.Security.RateLimiting;

namespace PestiTrackBackend.Controllers;

[ApiController]
[Route("api/auth")]
public sealed class AuthController : ControllerBase
{
    private readonly IAntiforgery _antiforgery;
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly UserManager<ApplicationUser> _userManager;

    public AuthController(
        IAntiforgery antiforgery,
        SignInManager<ApplicationUser> signInManager,
        UserManager<ApplicationUser> userManager)
    {
        _antiforgery = antiforgery;
        _signInManager = signInManager;
        _userManager = userManager;
    }

    // GET: /api/auth/csrf
    [HttpGet("csrf")]
    [AllowAnonymous]
    public IActionResult GetCsrfToken()
    {
        Response.Headers.CacheControl = "no-store";

        var tokens = _antiforgery.GetAndStoreTokens(HttpContext);

        return Ok(new
        {
            requestToken = tokens.RequestToken
        });
    }

    // POST: /api/auth/login
    [HttpPost("login")]
    [AllowAnonymous]
    [EnableRateLimiting(RateLimitPolicies.Auth)]
    public async Task<IActionResult> Login(
        [FromBody] LoginRequest request)
    {
        // Find the account by email.
        var user = await _userManager.FindByEmailAsync(
            request.Email.Trim());

        // Use a generic response to avoid disclosing account details.
        if (user is null || !user.IsActive)
        {
            return Unauthorized(new
            {
                message = "Invalid email or password."
            });
        }

        // Verify the password and apply Identity lockout rules.
        var result = await _signInManager.PasswordSignInAsync(
            user,
            request.Password,
            isPersistent: false,
            lockoutOnFailure: true);

        if (!result.Succeeded)
        {
            return Unauthorized(new
            {
                message = "Invalid email or password."
            });
        }

        // Return only safe account information.
        var roles = await _userManager.GetRolesAsync(user);

        Response.Headers.CacheControl = "no-store";

        return Ok(new
        {
            message = "Login successful.",
            user = new
            {
                id = user.Id,
                email = user.Email,
                displayName = user.DisplayName,
                systemRoles = roles
            }
        });
    }






        // GET: /api/auth/me
    [HttpGet("me")]
    [Authorize]
    public async Task<IActionResult> GetCurrentUser()
    {
        var user = await _userManager.GetUserAsync(User);

        if (user is null || !user.IsActive)
        {
            await _signInManager.SignOutAsync();

            return Unauthorized(new
            {
                message = "Authentication required."
            });
        }

        var roles = await _userManager.GetRolesAsync(user);

        Response.Headers.CacheControl = "no-store";

        return Ok(new
        {
            user = new
            {
                id = user.Id,
                email = user.Email,
                displayName = user.DisplayName,
                systemRoles = roles
            }
        });
    }

    // POST: /api/auth/logout
    [HttpPost("logout")]
    [Authorize]
    public async Task<IActionResult> Logout()
    {
        await _signInManager.SignOutAsync();

        Response.Headers.CacheControl = "no-store";

        return Ok(new
        {
            message = "Logout successful."
        });
    }

    // POST: /api/auth/change-password
    [HttpPost("change-password")]
    [Authorize]
    [EnableRateLimiting(RateLimitPolicies.Auth)]
    public async Task<IActionResult> ChangePassword(
        ChangePasswordRequest request)
    {
        var user = await _userManager.GetUserAsync(User);

        if (user is null || !user.IsActive)
        {
            await _signInManager.SignOutAsync();
            return Unauthorized(new
            {
                message = "Authentication required."
            });
        }

        var result = await _userManager.ChangePasswordAsync(
            user,
            request.CurrentPassword,
            request.NewPassword);

        if (!result.Succeeded)
        {
            return BadRequest(new
            {
                message = "Password change failed.",
                errors = result.Errors.Select(error => new
                {
                    error.Code,
                    error.Description
                })
            });
        }

        await _signInManager.RefreshSignInAsync(user);

        return Ok(new
        {
            message = "Password changed successfully."
        });
    }
}
