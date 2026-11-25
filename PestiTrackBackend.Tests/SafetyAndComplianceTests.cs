using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PestiTrackBackend.Controllers;
using PestiTrackBackend.Data;
using PestiTrackBackend.DTOs.Applications;
using PestiTrackBackend.DTOs.Compliance;
using PestiTrackBackend.Models.Applications;
using PestiTrackBackend.Models.Compliance;
using PestiTrackBackend.Models.Crops;
using PestiTrackBackend.Models.Farms;
using PestiTrackBackend.Models.Identity;
using PestiTrackBackend.Models.Inventory;
using PestiTrackBackend.Models.Products;
using PestiTrackBackend.Models.Weather;
using PestiTrackBackend.Models.Workers;
using PestiTrackBackend.Services.Authorization;

namespace PestiTrackBackend.Tests;

public sealed class SafetyAndComplianceTests
{
    [Fact]
    public async Task CreateApplication_RejectsExpiredProductRegistration()
    {
        var now = new DateTimeOffset(2026, 11, 25, 10, 0, 0, TimeSpan.Zero);
        await using var db = CreateDb();
        var seed = await SeedBaseAsync(db, now);

        var expiredProduct = new Product
        {
            Id = Guid.NewGuid(),
            Name = "Expired Herbicide",
            ActiveIngredient = "Test",
            RegistrationNumber = "REG-EXP-1",
            RegistrationExpiryDate = new DateOnly(2026, 11, 1),
            IsActive = true,
            CreatedAtUtc = now.UtcDateTime
        };
        db.Products.Add(expiredProduct);
        await db.SaveChangesAsync();

        var controller = CreateApplicationsController(db, seed.UserId);
        var result = await controller.Create(
            seed.FarmId,
            new CreatePesticideApplicationRequest
            {
                FieldId = seed.FieldId,
                FieldCropId = seed.FieldCropId,
                WorkerId = seed.ValidWorkerId,
                ApplicationDateUtc = now.UtcDateTime,
                TargetPest = "Broadleaf weeds",
                ApplicationMethod = "Boom sprayer",
                AreaTreatedHectares = 2m,
                Products =
                [
                    new ApplicationProductRequest
                    {
                        ProductId = expiredProduct.Id,
                        BatchNumber = "B-EXP",
                        QuantityUsed = 2m,
                        ApplicationRate = 1m,
                        RateUnit = "L/ha"
                    }
                ]
            },
            CancellationToken.None);

        var badRequest = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Contains("registration expired", badRequest.Value?.ToString(), StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task CreateApplication_RejectsRateExceedingLabelMaximum()
    {
        var now = new DateTimeOffset(2026, 11, 25, 10, 0, 0, TimeSpan.Zero);
        await using var db = CreateDb();
        var seed = await SeedBaseAsync(db, now);

        var controller = CreateApplicationsController(db, seed.UserId);
        var result = await controller.Create(
            seed.FarmId,
            new CreatePesticideApplicationRequest
            {
                FieldId = seed.FieldId,
                FieldCropId = seed.FieldCropId,
                WorkerId = seed.ValidWorkerId,
                ApplicationDateUtc = now.UtcDateTime,
                TargetPest = "Broadleaf weeds",
                ApplicationMethod = "Boom sprayer",
                AreaTreatedHectares = 2m,
                Products =
                [
                    new ApplicationProductRequest
                    {
                        ProductId = seed.ProductId,
                        BatchNumber = "B-100",
                        QuantityUsed = 8m,
                        ApplicationRate = 4.0m, // Label max is 2.5 L/ha
                        RateUnit = "L/ha"
                    }
                ]
            },
            CancellationToken.None);

        var badRequest = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Contains("exceeds the label maximum", badRequest.Value?.ToString(), StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task CreateApplication_RejectsWindSpeedExceedingLabelMaximum()
    {
        var now = new DateTimeOffset(2026, 11, 25, 10, 0, 0, TimeSpan.Zero);
        await using var db = CreateDb();
        var seed = await SeedBaseAsync(db, now);

        var windyWeather = new WeatherRecord
        {
            Id = Guid.NewGuid(),
            FarmId = seed.FarmId,
            FieldId = seed.FieldId,
            RecordedAtUtc = now.UtcDateTime,
            WindSpeedMetersPerSecond = 9.5m, // Label max is 5.0 m/s
            Source = "Manual",
            CreatedAtUtc = now.UtcDateTime
        };
        db.WeatherRecords.Add(windyWeather);
        await db.SaveChangesAsync();

        var controller = CreateApplicationsController(db, seed.UserId);
        var result = await controller.Create(
            seed.FarmId,
            new CreatePesticideApplicationRequest
            {
                FieldId = seed.FieldId,
                FieldCropId = seed.FieldCropId,
                WorkerId = seed.ValidWorkerId,
                WeatherRecordId = windyWeather.Id,
                ApplicationDateUtc = now.UtcDateTime,
                TargetPest = "Broadleaf weeds",
                ApplicationMethod = "Boom sprayer",
                AreaTreatedHectares = 2m,
                Products =
                [
                    new ApplicationProductRequest
                    {
                        ProductId = seed.ProductId,
                        BatchNumber = "B-100",
                        QuantityUsed = 3m,
                        ApplicationRate = 1.5m,
                        RateUnit = "L/ha"
                    }
                ]
            },
            CancellationToken.None);

        var badRequest = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Contains("wind speed", badRequest.Value?.ToString(), StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task GetSafetyCheck_ComputesReiPhiAndFlagsConflicts()
    {
        var now = new DateTimeOffset(2026, 11, 25, 10, 0, 0, TimeSpan.Zero);
        await using var db = CreateDb();
        var seed = await SeedBaseAsync(db, now);

        var app = new PesticideApplication
        {
            Id = Guid.NewGuid(),
            FarmId = seed.FarmId,
            FieldId = seed.FieldId,
            FieldCropId = seed.FieldCropId,
            WorkerId = seed.ValidWorkerId,
            ApplicationDateUtc = now.UtcDateTime,
            TargetPest = "Broadleaf weeds",
            ApplicationMethod = "Boom sprayer",
            AreaTreatedHectares = 2m,
            Status = PesticideApplicationStatus.Draft,
            CreatedByUserId = seed.UserId,
            CreatedAtUtc = now.UtcDateTime,
            Products =
            [
                new PesticideApplicationProduct
                {
                    Id = Guid.NewGuid(),
                    ProductId = seed.ProductId,
                    BatchNumber = "B-100",
                    QuantityUsed = 3m,
                    ApplicationRate = 1.5m,
                    RateUnit = "L/ha"
                }
            ]
        };
        db.PesticideApplications.Add(app);
        await db.SaveChangesAsync();

        var controller = CreateApplicationsController(db, seed.UserId);
        var result = await controller.GetSafetyCheck(
            seed.FarmId,
            app.Id,
            CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var response = Assert.IsType<ApplicationSafetyCheckResponse>(ok.Value);

        Assert.True(response.IsCompliant);
        Assert.Equal(0, response.ViolationCount);
        // Expected harvest is Nov 30, PHI is 14 days -> earliest safe harvest is Dec 9 -> warning.
        // Also no weather record attached while product has wind limit -> warning.
        Assert.Equal(2, response.WarningCount);
        Assert.Equal(now.AddHours(24), response.ReEntryAllowedAtUtc);
        Assert.Equal(new DateOnly(2026, 12, 9), response.EarliestSafeHarvestDate);
    }

    [Fact]
    public async Task ComplianceOverview_TracksCertificationsReiPhiHoldsAndRegistrationAlerts()
    {
        var now = new DateTimeOffset(2026, 11, 25, 10, 0, 0, TimeSpan.Zero);
        await using var db = CreateDb();
        var seed = await SeedBaseAsync(db, now);

        // Add an expiring-soon applicator and an expired applicator.
        db.Workers.AddRange(
            new Worker
            {
                Id = Guid.NewGuid(),
                FarmId = seed.FarmId,
                FullName = "Expiring Applicator",
                WorkerCode = "APP-2",
                IsApplicator = true,
                CertificationNumber = "CERT-SOON",
                CertificationExpiryDate = now.UtcDateTime.AddDays(10),
                IsActive = true,
                CreatedAtUtc = now.UtcDateTime
            },
            new Worker
            {
                Id = Guid.NewGuid(),
                FarmId = seed.FarmId,
                FullName = "Expired Applicator",
                WorkerCode = "APP-3",
                IsApplicator = true,
                CertificationNumber = "CERT-OLD",
                CertificationExpiryDate = now.UtcDateTime.AddDays(-5),
                IsActive = true,
                CreatedAtUtc = now.UtcDateTime
            });

        // Completed application 2 hours ago -> REI (24h) active, PHI (14d) active & conflicts with Nov 30 harvest.
        db.PesticideApplications.Add(new PesticideApplication
        {
            Id = Guid.NewGuid(),
            FarmId = seed.FarmId,
            FieldId = seed.FieldId,
            FieldCropId = seed.FieldCropId,
            WorkerId = seed.ValidWorkerId,
            ApplicationDateUtc = now.AddHours(-2).UtcDateTime,
            TargetPest = "Broadleaf weeds",
            ApplicationMethod = "Boom sprayer",
            AreaTreatedHectares = 3m,
            Status = PesticideApplicationStatus.Completed,
            CreatedByUserId = seed.UserId,
            CreatedAtUtc = now.AddHours(-2).UtcDateTime,
            Products =
            [
                new PesticideApplicationProduct
                {
                    Id = Guid.NewGuid(),
                    ProductId = seed.ProductId,
                    BatchNumber = "B-100",
                    QuantityUsed = 3m,
                    ApplicationRate = 1m,
                    RateUnit = "L/ha"
                }
            ]
        });

        // Product expiring in 20 days.
        db.Products.Add(new Product
        {
            Id = Guid.NewGuid(),
            Name = "Soon-Expiring Fungicide",
            ActiveIngredient = "Copper",
            RegistrationNumber = "REG-SOON",
            RegistrationExpiryDate = new DateOnly(2026, 12, 15),
            IsActive = true,
            CreatedAtUtc = now.UtcDateTime
        });

        db.ComplianceRecords.Add(new ComplianceRecord
        {
            Id = Guid.NewGuid(),
            FarmId = seed.FarmId,
            RecordType = ComplianceRecordType.Inspection,
            Title = "Storage ventilation inspection",
            Status = ComplianceRecordStatus.NonCompliant,
            Severity = ComplianceSeverity.High,
            CheckedAtUtc = now.UtcDateTime,
            CreatedByUserId = seed.UserId,
            CreatedAtUtc = now.UtcDateTime
        });

        await db.SaveChangesAsync();

        var controller = CreateComplianceController(db, now, seed.UserId);
        var result = await controller.GetOverview(seed.FarmId, CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var overview = Assert.IsType<ComplianceOverviewResponse>(ok.Value);

        Assert.Equal(1, overview.ExpiredCertifications);
        Assert.Equal(1, overview.ExpiringCertificationsWithin30Days);
        Assert.Equal(1, overview.ActiveReEntryHolds);
        Assert.Equal(1, overview.ActivePreHarvestHolds);
        Assert.True(overview.PreHarvestHolds[0].ConflictsWithExpectedHarvest);
        Assert.Equal(1, overview.ExpiringProductRegistrations);
        Assert.Equal(1, overview.NonCompliantRecords);
    }

    [Fact]
    public async Task ComplianceRecords_CreateUpdateAndResolveLifecycle()
    {
        var now = new DateTimeOffset(2026, 11, 25, 10, 0, 0, TimeSpan.Zero);
        await using var db = CreateDb();
        var seed = await SeedBaseAsync(db, now);
        var controller = CreateComplianceController(db, now, seed.UserId);

        var createResult = await controller.CreateRecord(
            seed.FarmId,
            new CreateComplianceRecordRequest
            {
                FieldId = seed.FieldId,
                WorkerId = seed.ValidWorkerId,
                RecordType = ComplianceRecordType.Inspection,
                Title = "PPE availability audit",
                Status = ComplianceRecordStatus.Open,
                Severity = ComplianceSeverity.Medium,
                CheckedAtUtc = now.UtcDateTime,
                InspectorName = "Inspector Jane",
                Findings = "Respirator cartridges due for replacement."
            },
            CancellationToken.None);

        var created = Assert.IsType<CreatedResult>(createResult.Result);
        var createdRecord = Assert.IsType<ComplianceRecordResponse>(created.Value);
        Assert.Equal("Open", createdRecord.Status);
        Assert.Null(createdRecord.ResolvedAtUtc);
        Assert.Equal("North Field", createdRecord.FieldName);

        var statusResult = await controller.UpdateStatus(
            seed.FarmId,
            createdRecord.Id,
            new UpdateComplianceRecordStatusRequest
            {
                Status = ComplianceRecordStatus.Resolved,
                CorrectiveAction = "Replaced all respirator cartridges."
            },
            CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(statusResult.Result);
        var resolvedRecord = Assert.IsType<ComplianceRecordResponse>(ok.Value);
        Assert.Equal("Resolved", resolvedRecord.Status);
        Assert.NotNull(resolvedRecord.ResolvedAtUtc);
        Assert.Equal("Replaced all respirator cartridges.", resolvedRecord.CorrectiveAction);

        var listResult = await controller.GetRecords(
            seed.FarmId,
            new ComplianceRecordQueryRequest
            {
                Status = ComplianceRecordStatus.Resolved,
                Search = "respirator"
            },
            CancellationToken.None);

        var listOk = Assert.IsType<OkObjectResult>(listResult.Result);
        var list = Assert.IsType<ComplianceRecordListResponse>(listOk.Value);
        Assert.Equal(1, list.TotalCount);
    }

    private static AppDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new AppDbContext(options);
    }

    private static PesticideApplicationsController CreateApplicationsController(
        AppDbContext db,
        Guid userId)
    {
        return new PesticideApplicationsController(db, new AllowAllFarmAuth())
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = new ClaimsPrincipal(new ClaimsIdentity(
                        [new Claim(ClaimTypes.NameIdentifier, userId.ToString())],
                        "Test"))
                }
            }
        };
    }

    private static ComplianceController CreateComplianceController(
        AppDbContext db,
        DateTimeOffset now,
        Guid userId)
    {
        return new ComplianceController(db, new FixedTimeProvider(now))
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = new ClaimsPrincipal(new ClaimsIdentity(
                        [new Claim(ClaimTypes.NameIdentifier, userId.ToString())],
                        "Test"))
                }
            }
        };
    }

    private static async Task<SeedData> SeedBaseAsync(AppDbContext db, DateTimeOffset now)
    {
        var user = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            UserName = "manager@example.com",
            Email = "manager@example.com",
            DisplayName = "Farm Manager"
        };
        var farm = new Farm
        {
            Id = Guid.NewGuid(),
            Name = "Green Valley Farm",
            IsActive = true,
            CreatedAtUtc = now.UtcDateTime
        };
        var field = new Field
        {
            Id = Guid.NewGuid(),
            FarmId = farm.Id,
            Name = "North Field",
            AreaHectares = 10m,
            IsActive = true,
            CreatedAtUtc = now.UtcDateTime
        };
        var crop = new Crop
        {
            Id = Guid.NewGuid(),
            Name = "Wheat",
            IsActive = true,
            CreatedAtUtc = now.UtcDateTime
        };
        var fieldCrop = new FieldCrop
        {
            Id = Guid.NewGuid(),
            FarmId = farm.Id,
            FieldId = field.Id,
            CropId = crop.Id,
            PlantingDate = new DateOnly(2026, 9, 1),
            ExpectedHarvestDate = new DateOnly(2026, 11, 30),
            Status = FieldCropStatus.Planted,
            CreatedAtUtc = now.UtcDateTime
        };
        var worker = new Worker
        {
            Id = Guid.NewGuid(),
            FarmId = farm.Id,
            FullName = "Certified Applicator",
            WorkerCode = "APP-1",
            IsApplicator = true,
            CertificationNumber = "CERT-001",
            CertificationExpiryDate = now.UtcDateTime.AddMonths(6),
            IsActive = true,
            CreatedAtUtc = now.UtcDateTime
        };
        var product = new Product
        {
            Id = Guid.NewGuid(),
            Name = "SafeGuard Herbicide",
            ActiveIngredient = "Glyphosate",
            RegistrationNumber = "REG-100",
            SignalWord = "Caution",
            RegistrationExpiryDate = new DateOnly(2027, 6, 30),
            PreHarvestIntervalDays = 14,
            ReEntryIntervalHours = 24,
            MaxApplicationRate = 2.5m,
            MaxApplicationRateUnit = "L/ha",
            MaxWindSpeedMetersPerSecond = 5.0m,
            IsActive = true,
            CreatedAtUtc = now.UtcDateTime
        };

        db.Users.Add(user);
        db.Farms.Add(farm);
        db.Fields.Add(field);
        db.Crops.Add(crop);
        db.FieldCrops.Add(fieldCrop);
        db.Workers.Add(worker);
        db.Products.Add(product);
        db.InventoryTransactions.Add(new InventoryTransaction
        {
            Id = Guid.NewGuid(),
            FarmId = farm.Id,
            ProductId = product.Id,
            BatchNumber = "B-100",
            TransactionType = InventoryTransactionType.StockIn,
            Quantity = 100m,
            ExpiryDate = new DateOnly(2027, 12, 31),
            CreatedByUserId = user.Id,
            CreatedAtUtc = now.AddDays(-10).UtcDateTime
        });

        await db.SaveChangesAsync();

        return new SeedData(
            user.Id,
            farm.Id,
            field.Id,
            fieldCrop.Id,
            worker.Id,
            product.Id);
    }

    private sealed record SeedData(
        Guid UserId,
        Guid FarmId,
        Guid FieldId,
        Guid FieldCropId,
        Guid ValidWorkerId,
        Guid ProductId);

    private sealed class FixedTimeProvider(DateTimeOffset utcNow) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => utcNow;
    }

    private sealed class AllowAllFarmAuth : IFarmAuthorizationService
    {
        public Task<bool> IsFarmMemberAsync(Guid userId, Guid farmId, CancellationToken cancellationToken = default) =>
            Task.FromResult(true);

        public Task<bool> HasPermissionAsync(Guid userId, Guid farmId, string permissionCode, CancellationToken cancellationToken = default) =>
            Task.FromResult(true);

        public Task<IReadOnlyList<string>> GetPermissionsAsync(Guid userId, Guid farmId, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<string>>([]);
    }
}
