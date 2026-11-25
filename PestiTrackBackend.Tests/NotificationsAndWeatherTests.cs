using System.Net;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PestiTrackBackend.Controllers;
using PestiTrackBackend.Data;
using PestiTrackBackend.DTOs.Inventory;
using PestiTrackBackend.DTOs.Notifications;
using PestiTrackBackend.DTOs.Weather;
using PestiTrackBackend.Models.Farms;
using PestiTrackBackend.Models.Identity;
using PestiTrackBackend.Models.Inventory;
using PestiTrackBackend.Models.Notifications;
using PestiTrackBackend.Models.Products;
using PestiTrackBackend.Models.Workers;
using PestiTrackBackend.Security;
using PestiTrackBackend.Services.Authorization;
using PestiTrackBackend.Services.Notifications;
using PestiTrackBackend.Services.Weather;

namespace PestiTrackBackend.Tests;

public sealed class NotificationsAndWeatherTests
{
    [Fact]
    public async Task SyncCurrentWeather_UsesFarmCoordinatesAndPersistsRecord()
    {
        var now = new DateTimeOffset(2026, 11, 25, 10, 0, 0, TimeSpan.Zero);
        await using var db = CreateDb();
        var seed = await SeedAsync(db, now, latitude: 9.0192m, longitude: 38.7525m);

        var fakeProvider = new FakeWeatherProvider(new ExternalWeatherReading
        {
            RecordedAtUtc = now.UtcDateTime,
            TemperatureCelsius = 21.4m,
            HumidityPercentage = 54m,
            RainfallMillimeters = 0m,
            WindSpeedMetersPerSecond = 3.2m,
            WindDirectionDegrees = 180m,
            Conditions = "Partly cloudy",
            Source = "Open-Meteo"
        });

        var controller = new WeatherRecordsController(db, fakeProvider);
        var result = await controller.SyncCurrentWeather(
            seed.FarmId,
            new SyncWeatherRequest(),
            CancellationToken.None);

        var created = Assert.IsType<CreatedAtActionResult>(result.Result);
        var response = Assert.IsType<WeatherRecordResponse>(created.Value);

        Assert.Equal(21.4m, response.TemperatureCelsius);
        Assert.Equal(3.2m, response.WindSpeedMetersPerSecond);
        Assert.Equal("Open-Meteo", response.Source);
        Assert.Equal("Partly cloudy", response.Conditions);
        Assert.Equal(9.0192m, fakeProvider.LastLatitude);
        Assert.Equal(38.7525m, fakeProvider.LastLongitude);
        Assert.Equal(1, await db.WeatherRecords.CountAsync());
    }

    [Fact]
    public async Task SyncCurrentWeather_ReturnsBadRequestWhenNoCoordinatesConfigured()
    {
        var now = new DateTimeOffset(2026, 11, 25, 10, 0, 0, TimeSpan.Zero);
        await using var db = CreateDb();
        var seed = await SeedAsync(db, now, latitude: null, longitude: null);

        var controller = new WeatherRecordsController(db, new FakeWeatherProvider(new ExternalWeatherReading()));
        var result = await controller.SyncCurrentWeather(
            seed.FarmId,
            new SyncWeatherRequest(),
            CancellationToken.None);

        Assert.IsType<BadRequestObjectResult>(result.Result);
    }

    [Fact]
    public async Task OpenMeteoWeatherProvider_ParsesCurrentWeatherPayload()
    {
        var now = new DateTimeOffset(2026, 11, 25, 10, 0, 0, TimeSpan.Zero);
        const string json = """
            {
              "current": {
                "time": "2026-11-25T10:00",
                "temperature_2m": 19.5,
                "relative_humidity_2m": 62,
                "precipitation": 0.4,
                "wind_speed_10m": 2.8,
                "wind_direction_10m": 240,
                "weather_code": 61
              }
            }
            """;

        var factory = new StubHttpClientFactory(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json")
        });

        var provider = new OpenMeteoWeatherProvider(factory, new FixedTimeProvider(now));
        var reading = await provider.FetchCurrentAsync(9.0m, 38.75m, CancellationToken.None);

        Assert.Equal(19.5m, reading.TemperatureCelsius);
        Assert.Equal(62m, reading.HumidityPercentage);
        Assert.Equal(0.4m, reading.RainfallMillimeters);
        Assert.Equal(2.8m, reading.WindSpeedMetersPerSecond);
        Assert.Equal(240m, reading.WindDirectionDegrees);
        Assert.Equal("Rain", reading.Conditions);
        Assert.Equal("Open-Meteo", reading.Source);
    }

    [Fact]
    public async Task InventoryThresholds_UpsertFlagsLowStockAndDeletesCleanly()
    {
        var now = new DateTimeOffset(2026, 11, 25, 10, 0, 0, TimeSpan.Zero);
        await using var db = CreateDb();
        var seed = await SeedAsync(db, now, 9.0m, 38.75m);

        var controller = new InventoryController(db);

        // Current stock is 5; setting threshold to 10 marks it low stock.
        var upsertResult = await controller.UpsertThreshold(
            seed.FarmId,
            seed.ProductId,
            new UpsertInventoryThresholdRequest { MinimumQuantity = 10m },
            CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(upsertResult.Result);
        var threshold = Assert.IsType<InventoryThresholdResponse>(ok.Value);
        Assert.True(threshold.IsLowStock);
        Assert.Equal(5m, threshold.CurrentQuantityOnHand);
        Assert.Equal(10m, threshold.MinimumQuantity);

        var listResult = await controller.GetThresholds(seed.FarmId, CancellationToken.None);
        var listOk = Assert.IsType<OkObjectResult>(listResult.Result);
        var items = Assert.IsAssignableFrom<IReadOnlyList<InventoryThresholdResponse>>(listOk.Value);
        Assert.Single(items);

        var deleteResult = await controller.DeleteThreshold(seed.FarmId, seed.ProductId, CancellationToken.None);
        Assert.IsType<NoContentResult>(deleteResult);
        Assert.Empty(await db.InventoryThresholds.ToListAsync());
    }

    [Fact]
    public async Task Notifications_GeneratesAlertsMarksReadAndAutoResolves()
    {
        var now = new DateTimeOffset(2026, 11, 25, 10, 0, 0, TimeSpan.Zero);
        await using var db = CreateDb();
        var seed = await SeedAsync(db, now, 9.0m, 38.75m);

        // Configure low-stock threshold (current stock is 5 < 15 -> LowStock alert).
        db.InventoryThresholds.Add(new InventoryThreshold
        {
            Id = Guid.NewGuid(),
            FarmId = seed.FarmId,
            ProductId = seed.ProductId,
            MinimumQuantity = 15m,
            UpdatedAtUtc = now.UtcDateTime
        });

        // Add expired applicator cert (-> ExpiredCertification alert).
        db.Workers.Add(new Worker
        {
            Id = Guid.NewGuid(),
            FarmId = seed.FarmId,
            FullName = "Expired Worker",
            WorkerCode = "W-EXP",
            IsApplicator = true,
            CertificationNumber = "CERT-99",
            CertificationExpiryDate = now.AddDays(-2).UtcDateTime,
            IsActive = true,
            CreatedAtUtc = now.UtcDateTime
        });

        await db.SaveChangesAsync();

        var syncService = new FarmNotificationService(db, new FixedTimeProvider(now));
        var controller = CreateNotificationsController(db, syncService, now, seed.UserId);

        var getResult = await controller.GetAll(
            seed.FarmId,
            new NotificationQueryRequest(),
            CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(getResult.Result);
        var list = Assert.IsType<NotificationListResponse>(ok.Value);

        Assert.Equal(2, list.TotalCount);
        Assert.Equal(2, list.UnreadCount);
        Assert.Contains(list.Items, item => item.Category == NotificationCategory.LowStock.ToString());
        Assert.Contains(list.Items, item => item.Category == NotificationCategory.ExpiredCertification.ToString());

        // Mark all read -> unread count drops to 0.
        var markAllResult = await controller.MarkAllRead(seed.FarmId, CancellationToken.None);
        var markAllOk = Assert.IsType<OkObjectResult>(markAllResult.Result);
        var summary = Assert.IsType<NotificationSummaryResponse>(markAllOk.Value);
        Assert.Equal(0, summary.UnreadCount);

        // Replenish stock above threshold -> LowStock alert auto-resolves on next sync.
        db.InventoryTransactions.Add(new InventoryTransaction
        {
            Id = Guid.NewGuid(),
            FarmId = seed.FarmId,
            ProductId = seed.ProductId,
            BatchNumber = "B-2",
            TransactionType = InventoryTransactionType.StockIn,
            Quantity = 20m,
            CreatedByUserId = seed.UserId,
            CreatedAtUtc = now.UtcDateTime
        });
        await db.SaveChangesAsync();

        var afterRestock = await controller.GetAll(
            seed.FarmId,
            new NotificationQueryRequest(),
            CancellationToken.None);
        var afterOk = Assert.IsType<OkObjectResult>(afterRestock.Result);
        var afterList = Assert.IsType<NotificationListResponse>(afterOk.Value);

        Assert.Equal(1, afterList.TotalCount);
        Assert.DoesNotContain(afterList.Items, item => item.Category == NotificationCategory.LowStock.ToString());
    }

    private static AppDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new AppDbContext(options);
    }

    private static NotificationsController CreateNotificationsController(
        AppDbContext db,
        IFarmNotificationService notificationService,
        DateTimeOffset now,
        Guid userId)
    {
        return new NotificationsController(
            db,
            new FullPermissionFarmAuth(),
            notificationService,
            new FixedTimeProvider(now))
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = new ClaimsPrincipal(new ClaimsIdentity(
                        [
                            new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
                            new Claim(ClaimTypes.Role, SystemRoleCodes.Administrator)
                        ],
                        "Test"))
                }
            }
        };
    }

    private static async Task<(Guid UserId, Guid FarmId, Guid ProductId)> SeedAsync(
        AppDbContext db,
        DateTimeOffset now,
        decimal? latitude,
        decimal? longitude)
    {
        var user = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            UserName = "admin@example.com",
            Email = "admin@example.com",
            DisplayName = "Admin"
        };
        var farm = new Farm
        {
            Id = Guid.NewGuid(),
            Name = "Highland Farm",
            Code = "HF-01",
            Latitude = latitude,
            Longitude = longitude,
            IsActive = true,
            CreatedAtUtc = now.UtcDateTime
        };
        var product = new Product
        {
            Id = Guid.NewGuid(),
            Name = "CropShield",
            IsActive = true,
            CreatedAtUtc = now.UtcDateTime
        };

        db.Users.Add(user);
        db.Farms.Add(farm);
        db.Products.Add(product);
        db.InventoryTransactions.Add(new InventoryTransaction
        {
            Id = Guid.NewGuid(),
            FarmId = farm.Id,
            ProductId = product.Id,
            BatchNumber = "B-1",
            TransactionType = InventoryTransactionType.StockIn,
            Quantity = 5m,
            CreatedByUserId = user.Id,
            CreatedAtUtc = now.UtcDateTime
        });

        await db.SaveChangesAsync();
        return (user.Id, farm.Id, product.Id);
    }

    private sealed class FakeWeatherProvider(ExternalWeatherReading reading) : IExternalWeatherProvider
    {
        public decimal? LastLatitude { get; private set; }
        public decimal? LastLongitude { get; private set; }

        public Task<ExternalWeatherReading> FetchCurrentAsync(
            decimal latitude,
            decimal longitude,
            CancellationToken cancellationToken = default)
        {
            LastLatitude = latitude;
            LastLongitude = longitude;
            return Task.FromResult(reading);
        }
    }

    private sealed class StubHttpClientFactory(HttpResponseMessage response) : IHttpClientFactory
    {
        public HttpClient CreateClient(string name) =>
            new(new StubHandler(response));

        private sealed class StubHandler(HttpResponseMessage response) : HttpMessageHandler
        {
            protected override Task<HttpResponseMessage> SendAsync(
                HttpRequestMessage request,
                CancellationToken cancellationToken) =>
                Task.FromResult(response);
        }
    }

    private sealed class FixedTimeProvider(DateTimeOffset utcNow) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => utcNow;
    }

    private sealed class FullPermissionFarmAuth : IFarmAuthorizationService
    {
        public Task<bool> HasPermissionAsync(Guid userId, Guid farmId, string permissionCode, CancellationToken cancellationToken = default) =>
            Task.FromResult(true);

        public Task<IReadOnlyList<string>> GetPermissionsAsync(Guid userId, Guid farmId, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<string>>([
                PermissionCodes.InventoryView,
                PermissionCodes.WorkersView,
                PermissionCodes.ComplianceView,
                PermissionCodes.ApplicationsView
            ]);
    }
}
