using System.Security.Claims;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Diagnostics;
using PestiTrackBackend.Models.Auditing;
using PestiTrackBackend.Models.Farms;
using PestiTrackBackend.Security.Authorization;

namespace PestiTrackBackend.Services.Auditing;

public sealed class AuditSaveChangesInterceptor : SaveChangesInterceptor
{
    private static readonly HashSet<string> SensitiveProperties =
        new(StringComparer.OrdinalIgnoreCase)
        {
            "PasswordHash",
            "SecurityStamp",
            "ConcurrencyStamp",
            "AuthenticatorKey",
            "RecoveryCodes"
        };

    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly TimeProvider _timeProvider;

    public AuditSaveChangesInterceptor(
        IHttpContextAccessor httpContextAccessor,
        TimeProvider timeProvider)
    {
        _httpContextAccessor = httpContextAccessor;
        _timeProvider = timeProvider;
    }

    public override InterceptionResult<int> SavingChanges(
        DbContextEventData eventData,
        InterceptionResult<int> result)
    {
        CaptureAuditLogs(eventData.Context);
        return base.SavingChanges(eventData, result);
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        CaptureAuditLogs(eventData.Context);
        return base.SavingChangesAsync(
            eventData,
            result,
            cancellationToken);
    }

    private void CaptureAuditLogs(DbContext? db)
    {
        if (db is null)
            return;

        db.ChangeTracker.DetectChanges();

        var httpContext = _httpContextAccessor.HttpContext;
        var principal = httpContext?.User;
        var actorIdValue = principal?.FindFirstValue(
            ClaimTypes.NameIdentifier);
        var actorId = Guid.TryParse(actorIdValue, out var parsedActorId)
            ? parsedActorId
            : (Guid?)null;
        var occurredAtUtc = _timeProvider.GetUtcNow().UtcDateTime;

        var entries = db.ChangeTracker
            .Entries()
            .Where(entry =>
                entry.Entity is not AuditLog &&
                entry.State is EntityState.Added or
                    EntityState.Modified or
                    EntityState.Deleted)
            .ToList();

        foreach (var entry in entries)
        {
            var changes = GetChanges(entry);

            if (entry.State == EntityState.Modified &&
                changes.Count == 0)
            {
                continue;
            }

            db.Set<AuditLog>().Add(new AuditLog
            {
                OccurredAtUtc = occurredAtUtc,
                ActorUserId = actorId,
                ActorDisplayName = Truncate(
                    principal?.FindFirstValue(
                        PestiTrackUserClaimsPrincipalFactory
                            .DisplayNameClaimType),
                    150),
                ActorEmail = Truncate(
                    principal?.FindFirstValue(ClaimTypes.Email) ??
                        principal?.Identity?.Name,
                    256),
                FarmId = GetFarmId(entry),
                Action = entry.State switch
                {
                    EntityState.Added => "Create",
                    EntityState.Modified => "Update",
                    EntityState.Deleted => "Delete",
                    _ => throw new InvalidOperationException()
                },
                EntityType = entry.Metadata.ClrType.Name,
                EntityId = GetEntityId(entry),
                ChangesJson = JsonSerializer.Serialize(changes),
                HttpMethod = Truncate(
                    httpContext?.Request.Method,
                    10),
                RequestPath = Truncate(
                    httpContext?.Request.Path.Value,
                    500),
                IpAddress = Truncate(
                    httpContext?.Connection.RemoteIpAddress?.ToString(),
                    64),
                UserAgent = Truncate(
                    httpContext?.Request.Headers.UserAgent.ToString(),
                    500)
            });
        }
    }

    private static Dictionary<string, AuditValueChange> GetChanges(
        EntityEntry entry)
    {
        var result =
            new Dictionary<string, AuditValueChange>(
                StringComparer.Ordinal);

        foreach (var property in entry.Properties)
        {
            var name = property.Metadata.Name;

            if (SensitiveProperties.Contains(name) ||
                property.Metadata.IsShadowProperty())
            {
                continue;
            }

            if (entry.State == EntityState.Modified &&
                (!property.IsModified ||
                    Equals(
                        property.OriginalValue,
                        property.CurrentValue)))
            {
                continue;
            }

            result[name] = new AuditValueChange
            {
                OldValue = entry.State == EntityState.Added
                    ? null
                    : NormalizeValue(property.OriginalValue),
                NewValue = entry.State == EntityState.Deleted
                    ? null
                    : NormalizeValue(property.CurrentValue)
            };
        }

        return result;
    }

    private static object? NormalizeValue(object? value)
    {
        if (value is string stringValue)
            return Truncate(stringValue, 1_000);

        if (value is byte[] bytes)
            return $"[binary:{bytes.Length} bytes]";

        return value;
    }

    private static string GetEntityId(EntityEntry entry)
    {
        var primaryKey = entry.Metadata.FindPrimaryKey();

        if (primaryKey is null)
            return string.Empty;

        return string.Join(
            ",",
            primaryKey.Properties.Select(property =>
                $"{property.Name}={entry.Property(property.Name).CurrentValue}"));
    }

    private static Guid? GetFarmId(EntityEntry entry)
    {
        if (entry.Entity is Farm farm)
            return farm.Id;

        var farmIdProperty = entry.Properties.FirstOrDefault(
            property =>
                property.Metadata.Name.Equals(
                    "FarmId",
                    StringComparison.OrdinalIgnoreCase));

        if (farmIdProperty?.CurrentValue is Guid farmId &&
            farmId != Guid.Empty)
        {
            return farmId;
        }

        foreach (var reference in entry.References)
        {
            if (reference.CurrentValue is Farm relatedFarm)
                return relatedFarm.Id;

            var relatedFarmId = reference.CurrentValue?
                .GetType()
                .GetProperty("FarmId")
                ?.GetValue(reference.CurrentValue);

            if (relatedFarmId is Guid relatedId &&
                relatedId != Guid.Empty)
            {
                return relatedId;
            }
        }

        return null;
    }

    private static string? Truncate(string? value, int maximumLength)
    {
        if (string.IsNullOrEmpty(value) ||
            value.Length <= maximumLength)
        {
            return value;
        }

        return value[..maximumLength];
    }

    private sealed class AuditValueChange
    {
        public object? OldValue { get; init; }

        public object? NewValue { get; init; }
    }
}
