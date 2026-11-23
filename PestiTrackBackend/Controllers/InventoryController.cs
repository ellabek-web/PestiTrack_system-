
using System.Data;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PestiTrackBackend.Data;
using PestiTrackBackend.DTOs.Inventory;
using PestiTrackBackend.Models.Inventory;
using PestiTrackBackend.Security.Authorization;

namespace PestiTrackBackend.Controllers;

[ApiController]
[Route("api/farms/{farmId:guid}/inventory")]
[Authorize]
public sealed class InventoryController : ControllerBase
{
    private readonly AppDbContext _db;

    public InventoryController(AppDbContext db)
    {
        _db = db;
    }

    // GET: api/farms/{farmId}/inventory/balances
    [HttpGet("balances")]
    [FarmPermission("inventory.view")]
    public async Task<IActionResult> GetBalances(
        Guid farmId,
        CancellationToken cancellationToken)
    {
        var farmExists = await _db.Farms
            .AsNoTracking()
            .AnyAsync(f => f.Id == farmId && f.IsActive, cancellationToken);

        if (!farmExists)
            return NotFound(new { message = "Farm not found or inactive." });

        var balances = await _db.InventoryTransactions
            .AsNoTracking()
            .Where(t => t.FarmId == farmId)
            .GroupBy(t => new
            {
                t.FarmId,
                t.ProductId,
                ProductName = t.Product.Name,
                t.BatchNumber,
                t.ExpiryDate
            })
            .Select(group => new InventoryBalanceResponse
            {
                FarmId = group.Key.FarmId,
                ProductId = group.Key.ProductId,
                ProductName = group.Key.ProductName,
                BatchNumber = group.Key.BatchNumber ?? string.Empty,
                ExpiryDate = group.Key.ExpiryDate,
                QuantityOnHand = group.Sum(t =>
                    t.TransactionType == InventoryTransactionType.StockIn ||
                    t.TransactionType == InventoryTransactionType.AdjustmentIn
                        ? t.Quantity
                        : -t.Quantity)
            })
            .Where(balance => balance.QuantityOnHand > 0)
            .OrderBy(balance => balance.ProductName)
            .ThenBy(balance => balance.BatchNumber)
            .ToListAsync(cancellationToken);

        return Ok(balances);
    }

    // GET: api/farms/{farmId}/inventory/transactions
    [HttpGet("transactions")]
    [FarmPermission("inventory.view")]
    public async Task<IActionResult> GetTransactions(
        Guid farmId,
        [FromQuery] InventoryTransactionQueryRequest request,
        CancellationToken cancellationToken)
    {
        if (request.FromUtc.HasValue &&
            request.ToUtc.HasValue &&
            request.FromUtc > request.ToUtc)
        {
            return BadRequest(new
            {
                message = "FromUtc must be earlier than or equal to ToUtc."
            });
        }

        var farmExists = await _db.Farms
            .AsNoTracking()
            .AnyAsync(f => f.Id == farmId && f.IsActive, cancellationToken);

        if (!farmExists)
            return NotFound(new { message = "Farm not found or inactive." });

        var query = _db.InventoryTransactions
            .AsNoTracking()
            .Where(t => t.FarmId == farmId);

        if (request.ProductId.HasValue)
            query = query.Where(t => t.ProductId == request.ProductId.Value);

        if (!string.IsNullOrWhiteSpace(request.BatchNumber))
        {
            var batchNumber = request.BatchNumber.Trim();
            query = query.Where(t => t.BatchNumber == batchNumber);
        }

        if (request.TransactionType.HasValue)
        {
            query = query.Where(t =>
                t.TransactionType == request.TransactionType.Value);
        }

        if (request.FromUtc.HasValue)
        {
            var from = request.FromUtc.Value.UtcDateTime;
            query = query.Where(t => t.CreatedAtUtc >= from);
        }

        if (request.ToUtc.HasValue)
        {
            var to = request.ToUtc.Value.UtcDateTime;
            query = query.Where(t => t.CreatedAtUtc <= to);
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderByDescending(t => t.CreatedAtUtc)
            .ThenByDescending(t => t.Id)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(t => new InventoryTransactionResponse
            {
                Id = t.Id,
                FarmId = t.FarmId,
                ProductId = t.ProductId,
                ProductName = t.Product.Name,
                TransactionType = t.TransactionType.ToString(),
                Quantity = t.Quantity,
                BatchNumber = t.BatchNumber,
                ExpiryDate = t.ExpiryDate,
                Reference = t.Reference,
                Reason = t.Reason,
                Notes = t.Notes,
                CreatedByUserId = t.CreatedByUserId,
                CreatedAtUtc = new DateTimeOffset(
                    DateTime.SpecifyKind(t.CreatedAtUtc, DateTimeKind.Utc))
            })
            .ToListAsync(cancellationToken);

        return Ok(new
        {
            page = request.Page,
            pageSize = request.PageSize,
            totalCount,
            totalPages = (int)Math.Ceiling(
                totalCount / (double)request.PageSize),
            items
        });
    }

    // POST: api/farms/{farmId}/inventory/receive
    [HttpPost("receive")]
    [FarmPermission("inventory.manage")]
    public async Task<IActionResult> Receive(
        Guid farmId,
        [FromBody] ReceiveInventoryRequest request,
        CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();

        if (userId is null)
            return Unauthorized();

        var batchNumber = request.BatchNumber.Trim();

        if (string.IsNullOrWhiteSpace(batchNumber))
            return BadRequest(new { message = "Batch number is required." });

        await using var transaction =
            await _db.Database.BeginTransactionAsync(
                IsolationLevel.Serializable,
                cancellationToken);

        var farmExists = await _db.Farms
            .AnyAsync(f => f.Id == farmId && f.IsActive, cancellationToken);

        if (!farmExists)
            return NotFound(new { message = "Farm not found or inactive." });

        var product = await _db.Products
            .FirstOrDefaultAsync(
                p => p.Id == request.ProductId && p.IsActive,
                cancellationToken);

        if (product is null)
            return BadRequest(new { message = "Product not found or inactive." });

        var existing = await GetBatchTransactions(
            farmId, request.ProductId, batchNumber, cancellationToken);

        if (!HasConsistentExpiry(existing, request.ExpiryDate))
        {
            return Conflict(new
            {
                message = "The expiry date does not match the existing batch."
            });
        }

        var record = new InventoryTransaction
        {
            FarmId = farmId,
            ProductId = request.ProductId,
            TransactionType = InventoryTransactionType.StockIn,
            Quantity = request.Quantity,
            BatchNumber = batchNumber,
            ExpiryDate = request.ExpiryDate,
            Reference = Normalize(request.Reference),
            Notes = Normalize(request.Notes),
            CreatedByUserId = userId.Value,
            CreatedAtUtc = DateTime.UtcNow
        };

        _db.InventoryTransactions.Add(record);
        await _db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return CreatedAtAction(
            nameof(GetTransactions),
            new { farmId },
            ToResponse(record, product.Name));
    }

    // POST: api/farms/{farmId}/inventory/issue
    [HttpPost("issue")]
    [FarmPermission("inventory.issue")]
    public async Task<IActionResult> Issue(
        Guid farmId,
        [FromBody] IssueInventoryRequest request,
        CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();

        if (userId is null)
            return Unauthorized();

        var batchNumber = request.BatchNumber.Trim();

        if (string.IsNullOrWhiteSpace(batchNumber))
            return BadRequest(new { message = "Batch number is required." });

        await using var transaction =
            await _db.Database.BeginTransactionAsync(
                IsolationLevel.Serializable,
                cancellationToken);

        var farmExists = await _db.Farms
            .AnyAsync(f => f.Id == farmId && f.IsActive, cancellationToken);

        if (!farmExists)
            return NotFound(new { message = "Farm not found or inactive." });

        var product = await _db.Products
            .FirstOrDefaultAsync(
                p => p.Id == request.ProductId && p.IsActive,
                cancellationToken);

        if (product is null)
            return BadRequest(new { message = "Product not found or inactive." });

        // The exact farm/product/batch query is executed inside a
        // Serializable transaction to protect the balance from concurrent issues.
        var existing = await GetBatchTransactions(
            farmId, request.ProductId, batchNumber, cancellationToken);

        var availableQuantity = existing.Sum(t =>
            t.TransactionType == InventoryTransactionType.StockIn ||
            t.TransactionType == InventoryTransactionType.AdjustmentIn
                ? t.Quantity
                : -t.Quantity);

        if (availableQuantity < request.Quantity)
        {
            return Conflict(new
            {
                message = "Insufficient stock for this batch.",
                availableQuantity,
                requestedQuantity = request.Quantity
            });
        }

        var expiryDate = existing
            .Select(t => t.ExpiryDate)
            .Distinct()
            .SingleOrDefault();

        var record = new InventoryTransaction
        {
            FarmId = farmId,
            ProductId = request.ProductId,
            TransactionType = InventoryTransactionType.StockOut,
            Quantity = request.Quantity,
            BatchNumber = batchNumber,
            ExpiryDate = expiryDate,
            Reference = Normalize(request.Reference),
            Notes = Normalize(request.Notes),
            CreatedByUserId = userId.Value,
            CreatedAtUtc = DateTime.UtcNow
        };

        _db.InventoryTransactions.Add(record);
        await _db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return CreatedAtAction(
            nameof(GetTransactions),
            new { farmId },
            ToResponse(record, product.Name));
    }

    // POST: api/farms/{farmId}/inventory/adjustments
    [HttpPost("adjustments")]
    [FarmPermission("inventory.manage")]
    public async Task<IActionResult> Adjust(
        Guid farmId,
        [FromBody] AdjustInventoryRequest request,
        CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();

        if (userId is null)
            return Unauthorized();

        var batchNumber = request.BatchNumber.Trim();
        var reason = request.Reason.Trim();

        if (string.IsNullOrWhiteSpace(batchNumber))
            return BadRequest(new { message = "Batch number is required." });

        if (string.IsNullOrWhiteSpace(reason))
            return BadRequest(new { message = "Adjustment reason is required." });

        await using var transaction =
            await _db.Database.BeginTransactionAsync(
                IsolationLevel.Serializable,
                cancellationToken);

        var farmExists = await _db.Farms
            .AnyAsync(f => f.Id == farmId && f.IsActive, cancellationToken);

        if (!farmExists)
            return NotFound(new { message = "Farm not found or inactive." });

        var product = await _db.Products
            .FirstOrDefaultAsync(
                p => p.Id == request.ProductId && p.IsActive,
                cancellationToken);

        if (product is null)
            return BadRequest(new { message = "Product not found or inactive." });

        var existing = await GetBatchTransactions(
            farmId, request.ProductId, batchNumber, cancellationToken);

        var batchExpiry = existing.Count == 0
            ? request.ExpiryDate
            : existing.Select(t => t.ExpiryDate).Distinct().SingleOrDefault();

        if (existing.Count > 0 &&
            !HasConsistentExpiry(existing, request.ExpiryDate))
        {
            return Conflict(new
            {
                message = "The expiry date does not match the existing batch."
            });
        }

        var currentQuantity = existing.Sum(t =>
            t.TransactionType == InventoryTransactionType.StockIn ||
            t.TransactionType == InventoryTransactionType.AdjustmentIn
                ? t.Quantity
                : -t.Quantity);

        var transactionType =
            request.Direction == InventoryAdjustmentDirection.Increase
                ? InventoryTransactionType.AdjustmentIn
                : InventoryTransactionType.AdjustmentOut;

        if (transactionType == InventoryTransactionType.AdjustmentOut &&
            request.Quantity > currentQuantity)
        {
            return Conflict(new
            {
                message = "An adjustment cannot reduce stock below zero.",
                availableQuantity = currentQuantity,
                requestedQuantity = request.Quantity
            });
        }

        var record = new InventoryTransaction
        {
            FarmId = farmId,
            ProductId = request.ProductId,
            TransactionType = transactionType,
            Quantity = request.Quantity,
            BatchNumber = batchNumber,
            ExpiryDate = batchExpiry,
            Reason = reason,
            Reference = Normalize(request.Reference),
            Notes = Normalize(request.Notes),
            CreatedByUserId = userId.Value,
            CreatedAtUtc = DateTime.UtcNow
        };

        _db.InventoryTransactions.Add(record);
        await _db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return CreatedAtAction(
            nameof(GetTransactions),
            new { farmId },
            ToResponse(record, product.Name));
    }

    private async Task<List<InventoryTransaction>> GetBatchTransactions(
        Guid farmId,
        Guid productId,
        string batchNumber,
        CancellationToken cancellationToken)
    {
        return await _db.InventoryTransactions
            .Where(t =>
                t.FarmId == farmId &&
                t.ProductId == productId &&
                t.BatchNumber == batchNumber)
            .ToListAsync(cancellationToken);
    }

    private static bool HasConsistentExpiry(
        List<InventoryTransaction> existing,
        DateOnly? requestedExpiry)
    {
        if (existing.Count == 0)
            return true;

        return existing.All(t => t.ExpiryDate == requestedExpiry);
    }

    private Guid? GetCurrentUserId()
    {
        var value = User.FindFirstValue(ClaimTypes.NameIdentifier);

        return Guid.TryParse(value, out var userId)
            ? userId
            : null;
    }

    private static string? Normalize(string? value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? null
            : value.Trim();
    }

    private static InventoryTransactionResponse ToResponse(
        InventoryTransaction transaction,
        string productName)
    {
        return new InventoryTransactionResponse
        {
            Id = transaction.Id,
            FarmId = transaction.FarmId,
            ProductId = transaction.ProductId,
            ProductName = productName,
            TransactionType = transaction.TransactionType.ToString(),
            Quantity = transaction.Quantity,
            BatchNumber = transaction.BatchNumber,
            ExpiryDate = transaction.ExpiryDate,
            Reference = transaction.Reference,
            Reason = transaction.Reason,
            Notes = transaction.Notes,
            CreatedByUserId = transaction.CreatedByUserId,
            CreatedAtUtc = new DateTimeOffset(
                DateTime.SpecifyKind(
                    transaction.CreatedAtUtc,
                    DateTimeKind.Utc))
        };
    }
}
