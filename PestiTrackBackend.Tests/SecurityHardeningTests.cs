using System.Reflection;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;
using PestiTrackBackend.Controllers;
using PestiTrackBackend.Data;
using PestiTrackBackend.Health;
using PestiTrackBackend.Security.Middleware;
using PestiTrackBackend.Security.RateLimiting;

namespace PestiTrackBackend.Tests;

public sealed class SecurityHardeningTests
{
    [Fact]
    public async Task GlobalExceptionHandler_HidesExceptionDetailsInProduction()
    {
        var handler = new GlobalExceptionHandler(
            NullLogger<GlobalExceptionHandler>.Instance,
            new FakeHostEnvironment(Environments.Production));

        var context = new DefaultHttpContext();
        context.Request.Method = HttpMethods.Get;
        context.Request.Path = "/api/test";
        context.Response.Body = new MemoryStream();

        var handled = await handler.TryHandleAsync(
            context,
            new InvalidOperationException("Sensitive SQL detail"),
            CancellationToken.None);

        Assert.True(handled);
        Assert.Equal(StatusCodes.Status500InternalServerError, context.Response.StatusCode);

        context.Response.Body.Position = 0;
        var problem = await JsonSerializer.DeserializeAsync<ProblemDetails>(context.Response.Body);
        Assert.NotNull(problem);
        Assert.DoesNotContain("Sensitive SQL detail", problem!.Detail ?? string.Empty);
    }

    [Fact]
    public async Task GlobalExceptionHandler_IncludesExceptionDetailInDevelopment()
    {
        var handler = new GlobalExceptionHandler(
            NullLogger<GlobalExceptionHandler>.Instance,
            new FakeHostEnvironment(Environments.Development));

        var context = new DefaultHttpContext();
        context.Request.Method = HttpMethods.Get;
        context.Request.Path = "/api/test";
        context.Response.Body = new MemoryStream();

        var handled = await handler.TryHandleAsync(
            context,
            new InvalidOperationException("Dev diagnostic message"),
            CancellationToken.None);

        Assert.True(handled);
        context.Response.Body.Position = 0;
        var problem = await JsonSerializer.DeserializeAsync<ProblemDetails>(context.Response.Body);
        Assert.NotNull(problem);
        Assert.Equal("Dev diagnostic message", problem!.Detail);
    }

    [Fact]
    public async Task DatabaseHealthCheck_ReturnsHealthyForReachableDatabase()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        await using var db = new AppDbContext(options);
        var check = new DatabaseHealthCheck(db);
        var result = await check.CheckHealthAsync(new HealthCheckContext(), CancellationToken.None);

        Assert.Equal(HealthStatus.Healthy, result.Status);
    }

    [Fact]
    public void AuthController_AppliesAuthRateLimitingToLoginAndChangePassword()
    {
        var loginAttr = typeof(AuthController)
            .GetMethod(nameof(AuthController.Login))!
            .GetCustomAttribute<EnableRateLimitingAttribute>();
        var changePasswordAttr = typeof(AuthController)
            .GetMethod(nameof(AuthController.ChangePassword))!
            .GetCustomAttribute<EnableRateLimitingAttribute>();

        Assert.NotNull(loginAttr);
        Assert.Equal(RateLimitPolicies.Auth, loginAttr!.PolicyName);
        Assert.NotNull(changePasswordAttr);
        Assert.Equal(RateLimitPolicies.Auth, changePasswordAttr!.PolicyName);
    }

    private sealed class FakeHostEnvironment(string environmentName) : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = environmentName;
        public string ApplicationName { get; set; } = "PestiTrackBackend";
        public string ContentRootPath { get; set; } = AppContext.BaseDirectory;
        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }
}
