using System.IO.Compression;
using System.Text;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PestiTrackBackend.Controllers;
using PestiTrackBackend.Data;
using PestiTrackBackend.DTOs.Reports;
using PestiTrackBackend.Models.Applications;
using PestiTrackBackend.Models.Crops;
using PestiTrackBackend.Models.Farms;
using PestiTrackBackend.Models.Identity;
using PestiTrackBackend.Models.Inventory;
using PestiTrackBackend.Models.Products;
using PestiTrackBackend.Models.Workers;
using PestiTrackBackend.Services.Exports;

namespace PestiTrackBackend.Tests;

public sealed class ReportsControllerTests
{
    [Fact]
    public void DocumentWriters_CreateValidXlsxAndPdfContainers()
    {
        var document = new TabularExportDocument
        {
            Title = "Test report",
            GeneratedAtUtc = new DateTimeOffset(
                2026, 11, 24, 12, 0, 0, TimeSpan.Zero),
            Headers = ["Name", "Value"],
            Rows =
            [
                ["Product", "12.5"]
            ]
        };

        var xlsx = XlsxExportWriter.Write(document);
        var pdf = PdfExportWriter.Write(document);

        Assert.Equal((byte)'P', xlsx[0]);
        Assert.Equal((byte)'K', xlsx[1]);

        using var archive = new ZipArchive(
            new MemoryStream(xlsx),
            ZipArchiveMode.Read);
        Assert.NotNull(
            archive.GetEntry("xl/workbook.xml"));
        Assert.NotNull(
            archive.GetEntry("xl/worksheets/sheet1.xml"));

        Assert.StartsWith(
            "%PDF-1.4",
            Encoding.ASCII.GetString(pdf));
        Assert.EndsWith(
            "%%EOF",
            Encoding.ASCII.GetString(pdf));
    }

    [Theory]
    [InlineData(
        "xlsx",
        "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
        ".xlsx")]
    [InlineData("pdf", "application/pdf", ".pdf")]
    public async Task ApplicationDocumentExport_ReturnsRequestedFormat(
        string format,
        string contentType,
        string extension)
    {
        await using var fixture = await ReportFixture.CreateAsync();

        var result =
            await fixture.Controller.ExportApplicationsDocument(
                fixture.FarmId,
                format,
                new ApplicationReportQueryRequest(),
                CancellationToken.None);

        var file = Assert.IsType<FileContentResult>(result);
        Assert.Equal(contentType, file.ContentType);
        Assert.EndsWith(extension, file.FileDownloadName);
        Assert.NotEmpty(file.FileContents);
    }

    [Fact]
    public async Task ApplicationReport_FiltersAndReturnsSummary()
    {
        await using var fixture = await ReportFixture.CreateAsync();

        var result = await fixture.Controller.GetApplications(
            fixture.FarmId,
            new ApplicationReportQueryRequest
            {
                Status = PesticideApplicationStatus.Completed,
                ProductId = fixture.ProductId,
                Page = 1,
                PageSize = 10
            },
            CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result);
        var report = Assert.IsType<ApplicationReportResponse>(
            ok.Value);

        Assert.Equal(1, report.TotalCount);
        Assert.Equal(1, report.CompletedCount);
        Assert.Equal(0, report.DraftCount);
        Assert.Equal(3m, report.TotalAreaTreatedHectares);
        Assert.Single(report.Items);
    }

    [Fact]
    public async Task AggregateReports_ReturnProductAndWorkerActivity()
    {
        await using var fixture = await ReportFixture.CreateAsync();
        var request = new ApplicationReportQueryRequest();

        var productResult = await fixture.Controller.GetProductUsage(
            fixture.FarmId,
            request,
            CancellationToken.None);
        var workerResult = await fixture.Controller.GetWorkerActivity(
            fixture.FarmId,
            request,
            CancellationToken.None);

        var productOk = Assert.IsType<OkObjectResult>(productResult);
        var productReport =
            Assert.IsType<ProductUsageReportResponse>(
                productOk.Value);
        var usage = Assert.Single(productReport.Items);
        Assert.Equal(2.5m, usage.TotalQuantityUsed);
        Assert.Equal(1, usage.ApplicationCount);

        var workerOk = Assert.IsType<OkObjectResult>(workerResult);
        var workerReport =
            Assert.IsType<WorkerActivityReportResponse>(
                workerOk.Value);
        var activity = Assert.Single(workerReport.Items);
        Assert.Equal(2, activity.ApplicationCount);
        Assert.Equal(1, activity.CompletedApplicationCount);
        Assert.Equal(4m, activity.TotalAreaTreatedHectares);
    }

    [Fact]
    public async Task InventoryReportAndCsvExport_ReturnLedgerData()
    {
        await using var fixture = await ReportFixture.CreateAsync();

        var reportResult = await fixture.Controller.GetInventory(
            fixture.FarmId,
            new InventoryReportQueryRequest(),
            CancellationToken.None);
        var exportResult =
            await fixture.Controller.ExportInventoryCsv(
                fixture.FarmId,
                new InventoryReportQueryRequest(),
                CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(reportResult);
        var report = Assert.IsType<InventoryReportResponse>(
            ok.Value);
        Assert.Equal(2, report.TotalCount);
        Assert.Equal(1, report.StockInCount);
        Assert.Equal(1, report.StockOutCount);

        var file = Assert.IsType<FileContentResult>(exportResult);
        Assert.Equal("text/csv; charset=utf-8", file.ContentType);
        var csv = Encoding.UTF8.GetString(file.FileContents);
        Assert.Contains("Transaction ID", csv);
        Assert.Contains("BATCH-1", csv);
    }

    [Fact]
    public void CsvWriter_EscapesQuotesAndNeutralizesFormulas()
    {
        var content = CsvExportWriter.Write(
            ["Name", "Value"],
            [
                ["A \"quoted\" value", "=SUM(1,1)"]
            ]);

        var csv = Encoding.UTF8.GetString(content);

        Assert.Contains("\"A \"\"quoted\"\" value\"", csv);
        Assert.Contains("\"'=SUM(1,1)\"", csv);
    }

    private sealed class ReportFixture : IAsyncDisposable
    {
        private ReportFixture(
            AppDbContext db,
            ReportsController controller,
            Guid farmId,
            Guid productId)
        {
            Db = db;
            Controller = controller;
            FarmId = farmId;
            ProductId = productId;
        }

        public AppDbContext Db { get; }

        public ReportsController Controller { get; }

        public Guid FarmId { get; }

        public Guid ProductId { get; }

        public static async Task<ReportFixture> CreateAsync()
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;
            var db = new AppDbContext(options);

            var farm = new Farm
            {
                Name = "Report Farm",
                Code = "REPORT"
            };
            var field = new Field
            {
                FarmId = farm.Id,
                Farm = farm,
                Name = "North",
                Code = "NORTH",
                AreaHectares = 10
            };
            var crop = new Crop { Name = "Maize" };
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
                FullName = "Jane Worker",
                WorkerCode = "W-1"
            };
            var product = new Product { Name = "Test Product" };
            var user = new ApplicationUser
            {
                Id = Guid.NewGuid(),
                UserName = "report@example.com",
                DisplayName = "Report User"
            };

            var completed = CreateApplication(
                farm,
                field,
                fieldCrop,
                worker,
                product,
                user,
                new DateTime(2026, 1, 20, 8, 0, 0),
                PesticideApplicationStatus.Completed,
                3,
                2.5m);
            var draft = CreateApplication(
                farm,
                field,
                fieldCrop,
                worker,
                product,
                user,
                new DateTime(2026, 1, 21, 8, 0, 0),
                PesticideApplicationStatus.Draft,
                1,
                1m);

            var stockIn = new InventoryTransaction
            {
                FarmId = farm.Id,
                Farm = farm,
                ProductId = product.Id,
                Product = product,
                TransactionType = InventoryTransactionType.StockIn,
                Quantity = 10,
                BatchNumber = "BATCH-1",
                CreatedByUserId = user.Id,
                CreatedAtUtc = new DateTime(2026, 1, 19)
            };
            var stockOut = new InventoryTransaction
            {
                FarmId = farm.Id,
                Farm = farm,
                ProductId = product.Id,
                Product = product,
                TransactionType = InventoryTransactionType.StockOut,
                Quantity = 2.5m,
                BatchNumber = "BATCH-1",
                Reference = "APP-1",
                CreatedByUserId = user.Id,
                CreatedAtUtc = new DateTime(2026, 1, 20)
            };

            db.AddRange(
                farm,
                field,
                crop,
                fieldCrop,
                worker,
                product,
                user,
                completed,
                draft,
                stockIn,
                stockOut);
            await db.SaveChangesAsync();

            return new ReportFixture(
                db,
                new ReportsController(db, TimeProvider.System),
                farm.Id,
                product.Id);
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
            decimal area,
            decimal quantity)
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
                TargetPest = "=SUM(1,1)",
                ApplicationMethod = "Spray",
                AreaTreatedHectares = area,
                Status = status,
                CreatedByUserId = user.Id,
                CreatedByUser = user
            };
            application.Products.Add(
                new PesticideApplicationProduct
                {
                    PesticideApplicationId = application.Id,
                    PesticideApplication = application,
                    ProductId = product.Id,
                    Product = product,
                    BatchNumber = "BATCH-1",
                    QuantityUsed = quantity,
                    ApplicationRate = 1,
                    RateUnit = "L/ha"
                });

            return application;
        }
    }
}
