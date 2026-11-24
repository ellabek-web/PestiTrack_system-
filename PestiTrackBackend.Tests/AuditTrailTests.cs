using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PestiTrackBackend.Controllers;
using PestiTrackBackend.Data;
using PestiTrackBackend.DTOs.Auditing;
using PestiTrackBackend.Models.Auditing;
using PestiTrackBackend.Models.Farms;
using PestiTrackBackend.Models.Identity;
using PestiTrackBackend.Security.Authorization;
using PestiTrackBackend.Services.Auditing;

namespace PestiTrackBackend.Tests;

public sealed class AuditTrailTests
{
    [Fact]
    public async Task SaveChanges_CapturesActorRequestAndFarmChanges()
    {
        var actorId = Guid.NewGuid();
        var now = new DateTimeOffset(
            2026, 11, 24, 16, 0, 0, TimeSpan.Zero);
        var httpContext = CreateHttpContext(actorId);
        var accessor = new HttpContextAccessor
        {
            HttpContext = httpContext
        };
        var interceptor = new AuditSaveChangesInterceptor(
            accessor,
            new FixedTimeProvider(now));
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .AddInterceptors(interceptor)
            .Options;

        await using var db = new AppDbContext(options);
        var farm = new Farm
        {
            Name = "Audit Farm",
            Code = "AUDIT",
            Location = "North"
        };
        db.Farms.Add(farm);
        await db.SaveChangesAsync();

        var log = await db.AuditLogs.SingleAsync();

        Assert.Equal("Create", log.Action);
        Assert.Equal(nameof(Farm), log.EntityType);
        Assert.Equal(farm.Id, log.FarmId);
        Assert.Equal(actorId, log.ActorUserId);
        Assert.Equal("Audit Administrator", log.ActorDisplayName);
        Assert.Equal("admin@example.com", log.ActorEmail);
        Assert.Equal("POST", log.HttpMethod);
        Assert.Equal("/api/farms", log.RequestPath);
        Assert.Equal(now.UtcDateTime, log.OccurredAtUtc);
        Assert.Contains("\"Name\"", log.ChangesJson);
        Assert.Contains("Audit Farm", log.ChangesJson);
    }

    [Fact]
    public async Task SaveChanges_DoesNotRecordIdentitySecrets()
    {
        var interceptor = new AuditSaveChangesInterceptor(
            new HttpContextAccessor(),
            TimeProvider.System);
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .AddInterceptors(interceptor)
            .Options;

        await using var db = new AppDbContext(options);
        db.Users.Add(new ApplicationUser
        {
            DisplayName = "Safe User",
            UserName = "safe@example.com",
            Email = "safe@example.com",
            PasswordHash = "must-not-be-audited",
            SecurityStamp = "secret-stamp"
        });
        await db.SaveChangesAsync();

        var log = await db.AuditLogs.SingleAsync();

        Assert.DoesNotContain("PasswordHash", log.ChangesJson);
        Assert.DoesNotContain("must-not-be-audited", log.ChangesJson);
        Assert.DoesNotContain("SecurityStamp", log.ChangesJson);
        Assert.DoesNotContain("secret-stamp", log.ChangesJson);
    }

    [Fact]
    public async Task FarmHistory_IsScopedFilteredAndPaginated()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        await using var db = new AppDbContext(options);
        var farmId = Guid.NewGuid();
        var otherFarmId = Guid.NewGuid();
        var actorId = Guid.NewGuid();

        db.AuditLogs.AddRange(
            CreateLog(farmId, actorId, "Create", 3),
            CreateLog(farmId, actorId, "Update", 2),
            CreateLog(farmId, Guid.NewGuid(), "Update", 1),
            CreateLog(otherFarmId, actorId, "Update", 4));
        await db.SaveChangesAsync();

        var controller = new AuditLogsController(db);
        var result = await controller.GetFarmAuditLogs(
            farmId,
            new AuditLogQueryRequest
            {
                ActorUserId = actorId,
                Page = 1,
                PageSize = 1
            },
            CancellationToken.None);

        var response = Assert.IsType<AuditLogListResponse>(
            result.Value);
        Assert.Equal(2, response.TotalCount);
        Assert.Equal(2, response.TotalPages);
        Assert.Single(response.Items);
        Assert.Equal("Update", response.Items[0].Action);
        Assert.Equal("North", response.Items[0]
            .Changes["Location"].NewValue?.ToString());
    }

    private static DefaultHttpContext CreateHttpContext(Guid actorId)
    {
        var context = new DefaultHttpContext
        {
            User = new ClaimsPrincipal(
                new ClaimsIdentity(
                    [
                        new Claim(
                            ClaimTypes.NameIdentifier,
                            actorId.ToString()),
                        new Claim(
                            ClaimTypes.Name,
                            "admin@example.com"),
                        new Claim(
                            PestiTrackUserClaimsPrincipalFactory
                                .DisplayNameClaimType,
                            "Audit Administrator"),
                        new Claim(
                            ClaimTypes.Email,
                            "admin@example.com")
                    ],
                    "Test"))
        };
        context.Request.Method = "POST";
        context.Request.Path = "/api/farms";
        context.Connection.RemoteIpAddress =
            System.Net.IPAddress.Loopback;
        return context;
    }

    private static AuditLog CreateLog(
        Guid farmId,
        Guid actorId,
        string action,
        int minutesAgo)
    {
        return new AuditLog
        {
            OccurredAtUtc = DateTime.UtcNow.AddMinutes(-minutesAgo),
            FarmId = farmId,
            ActorUserId = actorId,
            Action = action,
            EntityType = "Farm",
            EntityId = $"Id={Guid.NewGuid()}",
            ChangesJson =
                """
                {"Location":{"OldValue":null,"NewValue":"North"}}
                """
        };
    }

    private sealed class FixedTimeProvider(
        DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
    }
}
