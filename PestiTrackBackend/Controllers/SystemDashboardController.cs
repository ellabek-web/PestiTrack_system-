using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PestiTrackBackend.Data;
using PestiTrackBackend.DTOs.Dashboard;
using PestiTrackBackend.Models.Applications;
using PestiTrackBackend.Models.Crops;
using PestiTrackBackend.Models.Inventory;
using PestiTrackBackend.Security;

namespace PestiTrackBackend.Controllers;

[ApiController]
[Route("api/dashboard")]
[Authorize(Roles = SystemRoleCodes.Administrator)]
public sealed class SystemDashboardController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly TimeProvider _timeProvider;

    public SystemDashboardController(
        AppDbContext db,
        TimeProvider timeProvider)
    {
        _db = db;
        _timeProvider = timeProvider;
    }

    // GET: api/dashboard
    [HttpGet]
    public async Task<ActionResult<SystemDashboardResponse>> Get(
        CancellationToken cancellationToken)
    {
        var now = _timeProvider.GetUtcNow();
        var thirtyDaysAgo = now.UtcDateTime.AddDays(-30);

        var inventoryProductCount =
            await _db.InventoryTransactions
                .AsNoTracking()
                .GroupBy(transaction => new
                {
                    transaction.FarmId,
                    transaction.ProductId,
                    transaction.BatchNumber
                })
                .Select(group => new
                {
                    group.Key.ProductId,
                    Quantity = group.Sum(transaction =>
                        transaction.TransactionType ==
                            InventoryTransactionType.StockIn ||
                        transaction.TransactionType ==
                            InventoryTransactionType.AdjustmentIn
                            ? transaction.Quantity
                            : -transaction.Quantity)
                })
                .Where(stock => stock.Quantity > 0)
                .Select(stock => stock.ProductId)
                .Distinct()
                .CountAsync(cancellationToken);

        return Ok(new SystemDashboardResponse
        {
            GeneratedAtUtc = now,
            TotalFarms = await _db.Farms
                .CountAsync(cancellationToken),
            ActiveFarms = await _db.Farms
                .CountAsync(
                    farm => farm.IsActive,
                    cancellationToken),
            TotalFields = await _db.Fields
                .CountAsync(cancellationToken),
            ActiveFields = await _db.Fields
                .CountAsync(
                    field =>
                        field.IsActive &&
                        field.Farm.IsActive,
                    cancellationToken),
            ActiveCropAssignments =
                await _db.FieldCrops
                    .CountAsync(
                        crop =>
                            crop.Status ==
                                FieldCropStatus.Planted &&
                            crop.Farm.IsActive &&
                            crop.Field.IsActive,
                        cancellationToken),
            ActiveWorkers = await _db.Workers
                .CountAsync(
                    worker =>
                        worker.IsActive &&
                        worker.Farm.IsActive,
                    cancellationToken),
            ActiveApplicators = await _db.Workers
                .CountAsync(
                    worker =>
                        worker.IsActive &&
                        worker.IsApplicator &&
                        worker.Farm.IsActive,
                    cancellationToken),
            InventoryProductCount = inventoryProductCount,
            DraftApplications =
                await _db.PesticideApplications
                    .CountAsync(
                        application =>
                            application.Status ==
                                PesticideApplicationStatus.Draft,
                        cancellationToken),
            CompletedApplications =
                await _db.PesticideApplications
                    .CountAsync(
                        application =>
                            application.Status ==
                                PesticideApplicationStatus.Completed,
                        cancellationToken),
            CompletedApplicationsWithin30Days =
                await _db.PesticideApplications
                    .CountAsync(
                        application =>
                            application.Status ==
                                PesticideApplicationStatus.Completed &&
                            application.ApplicationDateUtc >=
                                thirtyDaysAgo,
                        cancellationToken)
        });
    }
}
