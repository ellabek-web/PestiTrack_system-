using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PestiTrackBackend.Controllers;
using PestiTrackBackend.Data;
using PestiTrackBackend.DTOs.Applications;
using PestiTrackBackend.Models.Applications;
using PestiTrackBackend.Models.Crops;
using PestiTrackBackend.Models.Farms;
using PestiTrackBackend.Models.Identity;
using PestiTrackBackend.Models.Inventory;
using PestiTrackBackend.Models.Products;
using PestiTrackBackend.Models.Weather;
using PestiTrackBackend.Models.Workers;
using PestiTrackBackend.Services.Authorization;

namespace PestiTrackBackend.Tests;

public sealed class PesticideApplicationHistoryTests
{
    [Fact]
    public async Task GetHistory_FiltersPaginatesAndScopesToFarm()
    {
        await using var fixture = await ApplicationFixture.CreateAsync();

        var result = await fixture.Controller.GetHistory(
            fixture.FarmId,
            new PesticideApplicationHistoryQueryRequest
            {
                ProductId = fixture.ProductId,
                Status = PesticideApplicationStatus.Completed,
                Search = "Aphid",
                Page = 1,
                PageSize = 1
            },
            CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<PesticideApplicationHistoryResponse>(
            ok.Value);

        Assert.Equal(2, response.TotalCount);
        Assert.Equal(2, response.TotalPages);
        Assert.Single(response.Items);
        Assert.Equal(fixture.FarmId, response.Items[0].FarmId);
        Assert.Equal("Completed", response.Items[0].Status);
        Assert.True(response.Items[0].HasInventoryTraceability);
    }

    [Fact]
    public async Task GetHistory_RejectsAnInvertedDateRange()
    {
        await using var fixture = await ApplicationFixture.CreateAsync();

        var result = await fixture.Controller.GetHistory(
            fixture.FarmId,
            new PesticideApplicationHistoryQueryRequest
            {
                FromUtc = new DateTimeOffset(
                    2026, 2, 1, 0, 0, 0, TimeSpan.Zero),
                ToUtc = new DateTimeOffset(
                    2026, 1, 1, 0, 0, 0, TimeSpan.Zero)
            },
            CancellationToken.None);

        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task GetById_ReturnsWeatherCreatorAndInventoryTrace()
    {
        await using var fixture = await ApplicationFixture.CreateAsync();

        var result = await fixture.Controller.GetById(
            fixture.FarmId,
            fixture.TraceableApplicationId,
            CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<PesticideApplicationResponse>(
            ok.Value);

        Assert.Equal("Test Administrator", response.CreatedByDisplayName);
        Assert.NotNull(response.Weather);
        Assert.Equal("Clear", response.Weather!.Conditions);

        var product = Assert.Single(response.Products);
        Assert.NotNull(product.InventoryTransaction);
        Assert.Equal(
            "StockOut",
            product.InventoryTransaction!.TransactionType);
        Assert.Equal(product.QuantityUsed, product.InventoryTransaction.Quantity);
    }

    private sealed class ApplicationFixture : IAsyncDisposable
    {
        private ApplicationFixture(
            AppDbContext db,
            PesticideApplicationsController controller,
            Guid farmId,
            Guid productId,
            Guid traceableApplicationId)
        {
            Db = db;
            Controller = controller;
            FarmId = farmId;
            ProductId = productId;
            TraceableApplicationId = traceableApplicationId;
        }

        public AppDbContext Db { get; }

        public PesticideApplicationsController Controller { get; }

        public Guid FarmId { get; }

        public Guid ProductId { get; }

        public Guid TraceableApplicationId { get; }

        public static async Task<ApplicationFixture> CreateAsync()
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;

            var db = new AppDbContext(options);

            var farm = new Farm
            {
                Name = "Green Farm",
                Code = "GREEN"
            };
            var otherFarm = new Farm
            {
                Name = "Other Farm",
                Code = "OTHER"
            };
            var field = new Field
            {
                FarmId = farm.Id,
                Farm = farm,
                Name = "North Field",
                Code = "NORTH",
                AreaHectares = 12
            };
            var crop = new Crop
            {
                Name = "Maize"
            };
            var fieldCrop = new FieldCrop
            {
                FarmId = farm.Id,
                Farm = farm,
                FieldId = field.Id,
                Field = field,
                CropId = crop.Id,
                Crop = crop,
                PlantingDate = new DateOnly(2026, 1, 1),
                Status = FieldCropStatus.Planted
            };
            var worker = new Worker
            {
                FarmId = farm.Id,
                Farm = farm,
                FullName = "Jane Applicator",
                WorkerCode = "APP-1",
                IsApplicator = true,
                CertificationNumber = "CERT-1",
                CertificationExpiryDate = new DateTime(2027, 1, 1)
            };
            var product = new Product
            {
                Name = "Aphid Control"
            };
            var otherProduct = new Product
            {
                Name = "Fungicide"
            };
            var user = new ApplicationUser
            {
                Id = Guid.NewGuid(),
                UserName = "admin@example.com",
                NormalizedUserName = "ADMIN@EXAMPLE.COM",
                Email = "admin@example.com",
                NormalizedEmail = "ADMIN@EXAMPLE.COM",
                DisplayName = "Test Administrator"
            };
            var weather = new WeatherRecord
            {
                FarmId = farm.Id,
                Farm = farm,
                FieldId = field.Id,
                Field = field,
                RecordedAtUtc = new DateTime(2026, 1, 20, 8, 0, 0),
                TemperatureCelsius = 24,
                Conditions = "Clear",
                Source = "Manual"
            };

            var newest = CreateApplication(
                farm, field, fieldCrop, worker, product, user,
                new DateTime(2026, 1, 25, 8, 0, 0),
                PesticideApplicationStatus.Completed,
                "Aphid",
                weather);
            var newestLine = newest.Products.Single();
            newestLine.InventoryTransaction = CreateStockOut(
                farm, product, user, newestLine);

            var older = CreateApplication(
                farm, field, fieldCrop, worker, product, user,
                new DateTime(2026, 1, 15, 8, 0, 0),
                PesticideApplicationStatus.Completed,
                "Aphid",
                null);
            var olderLine = older.Products.Single();
            olderLine.InventoryTransaction = CreateStockOut(
                farm, product, user, olderLine);

            var draft = CreateApplication(
                farm, field, fieldCrop, worker, otherProduct, user,
                new DateTime(2026, 1, 10, 8, 0, 0),
                PesticideApplicationStatus.Draft,
                "Rust",
                null);

            var otherFarmApplication = new PesticideApplication
            {
                FarmId = otherFarm.Id,
                Farm = otherFarm,
                FieldId = field.Id,
                Field = field,
                FieldCropId = fieldCrop.Id,
                FieldCrop = fieldCrop,
                WorkerId = worker.Id,
                Worker = worker,
                ApplicationDateUtc =
                    new DateTime(2026, 1, 30, 8, 0, 0),
                TargetPest = "Aphid",
                ApplicationMethod = "Spray",
                AreaTreatedHectares = 1,
                Status = PesticideApplicationStatus.Completed,
                CreatedByUserId = user.Id,
                CreatedByUser = user
            };
            otherFarmApplication.Products.Add(
                new PesticideApplicationProduct
                {
                    PesticideApplicationId = otherFarmApplication.Id,
                    PesticideApplication = otherFarmApplication,
                    ProductId = product.Id,
                    Product = product,
                    BatchNumber = "BATCH-OTHER",
                    QuantityUsed = 1,
                    ApplicationRate = 1,
                    RateUnit = "L/ha"
                });

            db.AddRange(
                farm,
                otherFarm,
                field,
                crop,
                fieldCrop,
                worker,
                product,
                otherProduct,
                user,
                weather,
                newest,
                older,
                draft,
                otherFarmApplication);
            await db.SaveChangesAsync();

            return new ApplicationFixture(
                db,
                new PesticideApplicationsController(
                    db,
                    new AllowAllFarmAuthorizationService()),
                farm.Id,
                product.Id,
                newest.Id);
        }

        public ValueTask DisposeAsync() => Db.DisposeAsync();

        private static PesticideApplication CreateApplication(
            Farm farm,
            Field field,
            FieldCrop fieldCrop,
            Worker worker,
            Product product,
            ApplicationUser user,
            DateTime date,
            PesticideApplicationStatus status,
            string targetPest,
            WeatherRecord? weather)
        {
            var application = new PesticideApplication
            {
                FarmId = farm.Id,
                Farm = farm,
                FieldId = field.Id,
                Field = field,
                FieldCropId = fieldCrop.Id,
                FieldCrop = fieldCrop,
                WorkerId = worker.Id,
                Worker = worker,
                ApplicationDateUtc = date,
                TargetPest = targetPest,
                ApplicationMethod = "Boom spray",
                AreaTreatedHectares = 2,
                WeatherRecordId = weather?.Id,
                WeatherRecord = weather,
                Status = status,
                CreatedByUserId = user.Id,
                CreatedByUser = user,
                CreatedAtUtc = date.AddHours(-1)
            };

            application.Products.Add(
                new PesticideApplicationProduct
                {
                    PesticideApplicationId = application.Id,
                    PesticideApplication = application,
                    ProductId = product.Id,
                    Product = product,
                    BatchNumber = "BATCH-1",
                    QuantityUsed = 2,
                    ApplicationRate = 1,
                    RateUnit = "L/ha"
                });

            return application;
        }

        private static InventoryTransaction CreateStockOut(
            Farm farm,
            Product product,
            ApplicationUser user,
            PesticideApplicationProduct line)
        {
            return new InventoryTransaction
            {
                FarmId = farm.Id,
                Farm = farm,
                ProductId = product.Id,
                Product = product,
                TransactionType = InventoryTransactionType.StockOut,
                Quantity = line.QuantityUsed,
                BatchNumber = line.BatchNumber,
                Reference = $"APP-{line.Id:N}",
                Reason = "Pesticide application consumption",
                CreatedByUserId = user.Id,
                PesticideApplicationProductId = line.Id,
                ApplicationProduct = line
            };
        }
    }

    private sealed class AllowAllFarmAuthorizationService
        : IFarmAuthorizationService
    {
        public Task<bool> HasPermissionAsync(
            Guid userId,
            Guid farmId,
            string permissionCode,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(true);
        }
    }
}
