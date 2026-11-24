using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PestiTrackBackend.Controllers;
using PestiTrackBackend.Data;
using PestiTrackBackend.DTOs.Dashboard;
using PestiTrackBackend.Models.Applications;
using PestiTrackBackend.Models.Crops;
using PestiTrackBackend.Models.Farms;
using PestiTrackBackend.Models.Identity;
using PestiTrackBackend.Models.Inventory;
using PestiTrackBackend.Models.Products;
using PestiTrackBackend.Models.Workers;
using PestiTrackBackend.Security;
using PestiTrackBackend.Services.Authorization;

namespace PestiTrackBackend.Tests;

public sealed class DashboardControllerTests
{
    [Fact]
    public async Task SystemDashboard_ReturnsCrossFarmTotals()
    {
        var now = new DateTimeOffset(
            2026, 11, 24, 12, 0, 0, TimeSpan.Zero);
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        await using var db = new AppDbContext(options);
        await SeedAsync(db, now);
        var controller = new SystemDashboardController(
            db,
            new FixedTimeProvider(now));

        var result = await controller.Get(
            CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var response = Assert.IsType<SystemDashboardResponse>(ok.Value);

        Assert.Equal(1, response.TotalFarms);
        Assert.Equal(1, response.ActiveFarms);
        Assert.Equal(2, response.TotalFields);
        Assert.Equal(1, response.ActiveFields);
        Assert.Equal(1, response.ActiveCropAssignments);
        Assert.Equal(1, response.ActiveWorkers);
        Assert.Equal(1, response.ActiveApplicators);
        Assert.Equal(1, response.InventoryProductCount);
        Assert.Equal(1, response.CompletedApplications);
        Assert.Equal(1, response.CompletedApplicationsWithin30Days);
    }

    [Fact]
    public async Task Get_ReturnsOnlyPermittedSectionsWithAccurateCounts()
    {
        var now = new DateTimeOffset(
            2026, 11, 24, 12, 0, 0, TimeSpan.Zero);
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        await using var db = new AppDbContext(options);
        var seeded = await SeedAsync(db, now);
        var permissions = new PermissionAuthorizationService(
            PermissionCodes.FieldsView,
            PermissionCodes.CropsView,
            PermissionCodes.InventoryView,
            PermissionCodes.ApplicationsView);
        var controller = CreateController(
            db,
            permissions,
            new FixedTimeProvider(now),
            seeded.UserId);

        var result = await controller.Get(
            seeded.FarmId,
            CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<FarmDashboardResponse>(ok.Value);

        Assert.Equal(
            ["fields", "crops", "inventory", "applications"],
            response.VisibleSections);
        Assert.NotNull(response.Fields);
        Assert.Equal(2, response.Fields!.Total);
        Assert.Equal(1, response.Fields.Active);
        Assert.Equal(10m, response.Fields.TotalAreaHectares);
        Assert.NotNull(response.Crops);
        Assert.Equal(1, response.Crops!.Planted);
        Assert.Null(response.Workers);
        Assert.NotNull(response.Inventory);
        Assert.Equal(1, response.Inventory!.StockLineCount);
        Assert.Equal(1, response.Inventory.ProductCount);
        Assert.NotNull(response.Applications);
        Assert.Equal(1, response.Applications!.Completed);
        Assert.Equal(1, response.Applications.CompletedWithin30Days);
        Assert.Single(response.RecentApplications);
        Assert.Equal(2, response.RecentInventoryTransactions.Count);
    }

    [Fact]
    public async Task Get_RequiresAnAuthenticatedUserIdentifier()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        await using var db = new AppDbContext(options);
        var farm = new Farm { Name = "Farm", Code = "FARM" };
        db.Farms.Add(farm);
        await db.SaveChangesAsync();

        var controller = new DashboardController(
            db,
            new PermissionAuthorizationService(),
            TimeProvider.System)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext()
            }
        };

        var result = await controller.Get(
            farm.Id,
            CancellationToken.None);

        Assert.IsType<UnauthorizedResult>(result);
    }

    private static DashboardController CreateController(
        AppDbContext db,
        IFarmAuthorizationService authorizationService,
        TimeProvider timeProvider,
        Guid userId)
    {
        var identity = new ClaimsIdentity(
            [
                new Claim(
                    ClaimTypes.NameIdentifier,
                    userId.ToString())
            ],
            "Test");

        return new DashboardController(
            db,
            authorizationService,
            timeProvider)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = new ClaimsPrincipal(identity)
                }
            }
        };
    }

    private static async Task<(Guid FarmId, Guid UserId)> SeedAsync(
        AppDbContext db,
        DateTimeOffset now)
    {
        var farm = new Farm
        {
            Name = "Green Farm",
            Code = "GREEN"
        };
        var activeField = new Field
        {
            FarmId = farm.Id,
            Farm = farm,
            Name = "North",
            Code = "NORTH",
            AreaHectares = 10,
            IsActive = true
        };
        var inactiveField = new Field
        {
            FarmId = farm.Id,
            Farm = farm,
            Name = "South",
            Code = "SOUTH",
            AreaHectares = 20,
            IsActive = false
        };
        var crop = new Crop { Name = "Maize" };
        var fieldCrop = new FieldCrop
        {
            FarmId = farm.Id,
            Farm = farm,
            FieldId = activeField.Id,
            Field = activeField,
            CropId = crop.Id,
            Crop = crop,
            PlantingDate = new DateOnly(2026, 10, 1),
            Status = FieldCropStatus.Planted
        };
        var worker = new Worker
        {
            FarmId = farm.Id,
            Farm = farm,
            FullName = "Applicator",
            WorkerCode = "APP-1",
            IsApplicator = true,
            CertificationNumber = "CERT",
            CertificationExpiryDate = now.UtcDateTime.AddDays(10)
        };
        var product = new Product { Name = "Product" };
        var user = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            UserName = "user@example.com",
            DisplayName = "User"
        };
        var stockIn = new InventoryTransaction
        {
            FarmId = farm.Id,
            Farm = farm,
            ProductId = product.Id,
            Product = product,
            TransactionType = InventoryTransactionType.StockIn,
            Quantity = 10,
            BatchNumber = "B-1",
            ExpiryDate = DateOnly.FromDateTime(
                now.UtcDateTime.AddDays(20)),
            CreatedByUserId = user.Id,
            CreatedAtUtc = now.UtcDateTime.AddDays(-2)
        };
        var stockOut = new InventoryTransaction
        {
            FarmId = farm.Id,
            Farm = farm,
            ProductId = product.Id,
            Product = product,
            TransactionType = InventoryTransactionType.StockOut,
            Quantity = 2,
            BatchNumber = "B-1",
            ExpiryDate = stockIn.ExpiryDate,
            CreatedByUserId = user.Id,
            CreatedAtUtc = now.UtcDateTime.AddDays(-1)
        };
        var application = new PesticideApplication
        {
            FarmId = farm.Id,
            Farm = farm,
            FieldId = activeField.Id,
            Field = activeField,
            FieldCropId = fieldCrop.Id,
            FieldCrop = fieldCrop,
            WorkerId = worker.Id,
            Worker = worker,
            ApplicationDateUtc = now.UtcDateTime.AddDays(-5),
            TargetPest = "Aphid",
            ApplicationMethod = "Spray",
            AreaTreatedHectares = 3,
            Status = PesticideApplicationStatus.Completed,
            CreatedByUserId = user.Id,
            CreatedByUser = user
        };

        db.AddRange(
            farm,
            activeField,
            inactiveField,
            crop,
            fieldCrop,
            worker,
            product,
            user,
            stockIn,
            stockOut,
            application);
        await db.SaveChangesAsync();

        return (farm.Id, user.Id);
    }

    private sealed class PermissionAuthorizationService(
        params string[] permissions)
        : IFarmAuthorizationService
    {
        private readonly HashSet<string> _permissions =
            permissions.ToHashSet(StringComparer.Ordinal);

        public Task<bool> HasPermissionAsync(
            Guid userId,
            Guid farmId,
            string permissionCode,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(
                _permissions.Contains(permissionCode));
        }
    }

    private sealed class FixedTimeProvider(
        DateTimeOffset utcNow)
        : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => utcNow;
    }
}
