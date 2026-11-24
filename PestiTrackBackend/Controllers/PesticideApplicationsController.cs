using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PestiTrackBackend.Data;
using PestiTrackBackend.DTOs.Applications;
using PestiTrackBackend.Models.Applications;
using PestiTrackBackend.Models.Crops;
using PestiTrackBackend.Security.Authorization;
using System.Data;
using PestiTrackBackend.Models.Inventory;
using PestiTrackBackend.Services.Authorization;






namespace PestiTrackBackend.Controllers;

[ApiController]
[Route("api/farms/{farmId:guid}/applications")]
[Authorize]
public sealed class PesticideApplicationsController : ControllerBase
{
        private readonly AppDbContext _db;
        private readonly IFarmAuthorizationService _farmAuthorizationService;

        public PesticideApplicationsController(
            AppDbContext db,
            IFarmAuthorizationService farmAuthorizationService)
        {
            _db = db;
            _farmAuthorizationService = farmAuthorizationService;
        }


    // GET: api/farms/{farmId}/applications
    [HttpGet]
    [FarmPermission("applications.view")]
    public async Task<IActionResult> GetAll(
        Guid farmId,
        CancellationToken cancellationToken)
    {
        var applications = await ApplicationQuery()
            .Where(a => a.FarmId == farmId)
            .OrderByDescending(a => a.ApplicationDateUtc)
            .ThenByDescending(a => a.CreatedAtUtc)
            .ToListAsync(cancellationToken);

        var response = applications
            .Select(MapToResponse)
            .ToList();

        return Ok(response);
    }

    // GET: api/farms/{farmId}/applications/{applicationId}
    [HttpGet("{applicationId:guid}")]
    [FarmPermission("applications.view")]
    public async Task<IActionResult> GetById(
        Guid farmId,
        Guid applicationId,
        CancellationToken cancellationToken)
    {
        var application = await ApplicationQuery()
            .FirstOrDefaultAsync(
                a => a.FarmId == farmId && a.Id == applicationId,
                cancellationToken);

        if (application is null)
            return NotFound(new { message = "Application was not found." });

        return Ok(MapToResponse(application));
    }

    // POST: api/farms/{farmId}/applications
    [HttpPost]
    [FarmPermission("applications.create")]
    public async Task<IActionResult> Create(
        Guid farmId,
        CreatePesticideApplicationRequest request,
        CancellationToken cancellationToken)
    {
        var userIdValue = User.FindFirstValue(
            ClaimTypes.NameIdentifier);

        if (!Guid.TryParse(userIdValue, out var userId))
            return Unauthorized();

        var validationError = await ValidateApplicationAsync(
            farmId,
            request.FieldId,
            request.FieldCropId,
            request.WorkerId,
            request.ApplicationDateUtc,
            request.AreaTreatedHectares,
            request.WeatherRecordId,
            request.Products,
            cancellationToken);

        if (validationError is not null)
        {
            return BadRequest(new
            {
                message = validationError
            });
        }

        var application = new PesticideApplication
        {
            Id = Guid.NewGuid(),
            FarmId = farmId,
            FieldId = request.FieldId,
            FieldCropId = request.FieldCropId,
            WorkerId = request.WorkerId,
            ApplicationDateUtc = request.ApplicationDateUtc,
            TargetPest = request.TargetPest.Trim(),
            ApplicationMethod = request.ApplicationMethod.Trim(),
            Equipment = Normalize(request.Equipment),
            AreaTreatedHectares = request.AreaTreatedHectares,
            WeatherRecordId = request.WeatherRecordId,
            Notes = Normalize(request.Notes),
            Status = PesticideApplicationStatus.Draft,
            CreatedByUserId = userId,
            CreatedAtUtc = DateTime.UtcNow
        };

        foreach (var productRequest in request.Products)
        {
            application.Products.Add(new PesticideApplicationProduct
            {
                Id = Guid.NewGuid(),
                ProductId = productRequest.ProductId,
                BatchNumber = productRequest.BatchNumber.Trim(),
                QuantityUsed = productRequest.QuantityUsed,
                ApplicationRate = productRequest.ApplicationRate,
                RateUnit = productRequest.RateUnit.Trim()
            });
        }

        _db.PesticideApplications.Add(application);

        await _db.SaveChangesAsync(cancellationToken);

        var createdApplication = await ApplicationQuery()
            .FirstAsync(
                a => a.FarmId == farmId && a.Id == application.Id,
                cancellationToken);

        var response = MapToResponse(createdApplication);

        return CreatedAtAction(
            nameof(GetById),
            new
            {
                farmId,
                applicationId = application.Id
            },
            response);
    }



    // PUT: api/farms/{farmId}/applications/{applicationId}
    [HttpPut("{applicationId:guid}")]
    [FarmPermission("applications.create")]
    public async Task<IActionResult> UpdateDraft(
        Guid farmId,
        Guid applicationId,
        UpdatePesticideApplicationRequest request,
        CancellationToken cancellationToken)
    {
        // Load the application as a tracked entity because we will update it.
       // Use a serializable transaction to protect the draft replacement.
    await using var transaction =
        await _db.Database.BeginTransactionAsync(
            IsolationLevel.Serializable,
            cancellationToken);

    // Load the application as a tracked entity.
    // Do not Include Products here; we will replace them using set-based operations.
    var application = await _db.PesticideApplications
        .FirstOrDefaultAsync(
            a => a.Id == applicationId &&
                a.FarmId == farmId,
            cancellationToken);

    if (application is null)
    {
        return NotFound(new
        {
            message = "Application was not found."
        });
    }

    if (application.Status != PesticideApplicationStatus.Draft)
    {
        return Conflict(new
        {
            message = "Only draft applications can be updated."
        });
    }

    // Check that none of its product lines has an inventory transaction.
    var hasInventoryLinks = await _db.PesticideApplicationProducts
        .AnyAsync(
            p => p.PesticideApplicationId == applicationId &&
                _db.InventoryTransactions.Any(
                    t => t.PesticideApplicationProductId == p.Id),
            cancellationToken);

    if (hasInventoryLinks)
    {
        return Conflict(new
        {
            message = "This application contains inventory-linked product lines and cannot be updated."
        });
    }

        var validationError = await ValidateApplicationAsync(
            farmId,
            request.FieldId,
            request.FieldCropId,
            request.WorkerId,
            request.ApplicationDateUtc,
            request.AreaTreatedHectares,
            request.WeatherRecordId,
            request.Products,
            cancellationToken);

        if (validationError is not null)
        {
            return BadRequest(new
            {
                message = validationError
            });
        }

        // Update the application details.
        application.FieldId = request.FieldId;
        application.FieldCropId = request.FieldCropId;
        application.WorkerId = request.WorkerId;
        application.ApplicationDateUtc = request.ApplicationDateUtc;
        application.TargetPest = request.TargetPest.Trim();
        application.ApplicationMethod = request.ApplicationMethod.Trim();
        application.Equipment = Normalize(request.Equipment);
        application.AreaTreatedHectares = request.AreaTreatedHectares;
        application.WeatherRecordId = request.WeatherRecordId;
        application.Notes = Normalize(request.Notes);

        // Delete existing draft product lines directly in the database.
        // The inventory-link check above protects inventory history.
        await _db.PesticideApplicationProducts
            .Where(p => p.PesticideApplicationId == application.Id)
            .ExecuteDeleteAsync(cancellationToken);

        // Insert replacement product lines.
        var replacementProducts = request.Products
            .Select(productRequest => new PesticideApplicationProduct
            {
                Id = Guid.NewGuid(),
                PesticideApplicationId = application.Id,
                ProductId = productRequest.ProductId,
                BatchNumber = productRequest.BatchNumber.Trim(),
                QuantityUsed = productRequest.QuantityUsed,
                ApplicationRate = productRequest.ApplicationRate,
                RateUnit = productRequest.RateUnit.Trim()
            })
            .ToList();

        _db.PesticideApplicationProducts.AddRange(
            replacementProducts);

        // Save the application and replacement lines.
        await _db.SaveChangesAsync(cancellationToken);

        // Commit both the deletion and the replacement together.
        await transaction.CommitAsync(cancellationToken);

        // Reload the application with its related records for the response.
        var updatedApplication = await ApplicationQuery()
            .FirstAsync(
                a => a.Id == applicationId &&
                     a.FarmId == farmId,
                cancellationToken);

        return Ok(MapToResponse(updatedApplication));
    }






    private IQueryable<PesticideApplication> ApplicationQuery()
    {
        return _db.PesticideApplications
            .AsNoTracking()
            .Include(a => a.Field)
            .Include(a => a.FieldCrop)
                .ThenInclude(fc => fc.Crop)
            .Include(a => a.Worker)
            .Include(a => a.Products)
                .ThenInclude(p => p.Product)
            .Include(a => a.Products)
                .ThenInclude(p => p.InventoryTransaction);
    }


    // POST: api/farms/{farmId}/applications/{applicationId}/complete
    [HttpPost("{applicationId:guid}/complete")]
    [FarmPermission("applications.approve")]
    public async Task<IActionResult> Complete(
        Guid farmId,
        Guid applicationId,
        CancellationToken cancellationToken)
    {
        var userIdValue = User.FindFirstValue(
            ClaimTypes.NameIdentifier);

        if (!Guid.TryParse(userIdValue, out var userId))
            return Unauthorized();

        // Completion requires BOTH approval and inventory issue permission.
        var canIssueInventory =
            await _farmAuthorizationService.HasPermissionAsync(
                userId,
                farmId,
                "inventory.issue",
                cancellationToken);

        if (!canIssueInventory)
            return Forbid();

        await using var transaction =
            await _db.Database.BeginTransactionAsync(
                IsolationLevel.Serializable,
                cancellationToken);

        // Load this graph as tracked entities because completion changes
        // the application status and adds inventory transactions.
        var application = await _db.PesticideApplications
            .Include(a => a.Field)
            .Include(a => a.FieldCrop)
                .ThenInclude(fc => fc.Crop)
            .Include(a => a.Worker)
            .Include(a => a.Products)
                .ThenInclude(p => p.Product)
            .Include(a => a.Products)
                .ThenInclude(p => p.InventoryTransaction)
            .FirstOrDefaultAsync(
                a => a.FarmId == farmId &&
                     a.Id == applicationId,
                cancellationToken);

        if (application is null)
            return NotFound(new
            {
                message = "Application was not found."
            });

        if (application.Status != PesticideApplicationStatus.Draft)
        {
            return Conflict(new
            {
                message = "Only draft applications can be completed."
            });
        }

        if (application.Products.Count == 0)
        {
            return BadRequest(new
            {
                message = "An application must contain at least one product."
            });
        }

        if (application.Products.Any(p => p.InventoryTransaction != null))
        {
            return Conflict(new
            {
                message = "One or more product lines already have inventory transactions."
            });
        }

        // Revalidate the application before consuming inventory.
        var validationError = await ValidateApplicationAsync(
            farmId,
            application.FieldId,
            application.FieldCropId,
            application.WorkerId,
            application.ApplicationDateUtc,
            application.AreaTreatedHectares,
            application.WeatherRecordId,
            application.Products.Select(p => new ApplicationProductRequest
            {
                ProductId = p.ProductId,
                BatchNumber = p.BatchNumber,
                QuantityUsed = p.QuantityUsed,
                ApplicationRate = p.ApplicationRate,
                RateUnit = p.RateUnit
            }).ToList(),
            cancellationToken);

        if (validationError is not null)
        {
            return BadRequest(new
            {
                message = validationError
            });
        }

        foreach (var productLine in application.Products)
        {
            var batchNumber = productLine.BatchNumber.Trim();

            // Read the complete ledger for this exact farm/product/batch.
            var existingTransactions = await _db.InventoryTransactions
                .Where(t =>
                    t.FarmId == farmId &&
                    t.ProductId == productLine.ProductId &&
                    t.BatchNumber == batchNumber)
                .ToListAsync(cancellationToken);

            if (existingTransactions.Count == 0)
            {
                return Conflict(new
                {
                    message = $"No inventory exists for product '{productLine.Product.Name}' and batch '{batchNumber}'.",
                    productId = productLine.ProductId,
                    batchNumber
                });
            }

            // Every transaction for a batch must retain the same expiry date.
            var expiryDates = existingTransactions
                .Select(t => t.ExpiryDate)
                .Distinct()
                .ToList();

            if (expiryDates.Count != 1)
            {
                return Conflict(new
                {
                    message = $"The expiry date is inconsistent for batch '{batchNumber}'."
                });
            }

            var availableQuantity = existingTransactions.Sum(t =>
                t.TransactionType == InventoryTransactionType.StockIn ||
                t.TransactionType == InventoryTransactionType.AdjustmentIn
                    ? t.Quantity
                    : -t.Quantity);

            if (availableQuantity < productLine.QuantityUsed)
            {
                return Conflict(new
                {
                    message = $"Insufficient stock for product '{productLine.Product.Name}', batch '{batchNumber}'.",
                    availableQuantity,
                    requestedQuantity = productLine.QuantityUsed
                });
            }

            // Generate a deterministic reference for this application line.
            var reference =
                $"APP-{application.Id:N}-{productLine.Id:N}";

            var referenceExists = await _db.InventoryTransactions
                .AnyAsync(t =>
                    t.FarmId == farmId &&
                    t.ProductId == productLine.ProductId &&
                    t.BatchNumber == batchNumber &&
                    t.TransactionType == InventoryTransactionType.StockOut &&
                    t.Reference == reference,
                    cancellationToken);

            if (referenceExists)
            {
                return Conflict(new
                {
                    message = "An inventory transaction already exists for this application product line."
                });
            }

            var stockOut = new InventoryTransaction
            {
                Id = Guid.NewGuid(),
                FarmId = farmId,
                ProductId = productLine.ProductId,
                TransactionType = InventoryTransactionType.StockOut,
                Quantity = productLine.QuantityUsed,
                BatchNumber = batchNumber,
                ExpiryDate = expiryDates[0],
                Reference = reference,
                Reason = "Pesticide application consumption",
                Notes = $"Automatically issued for application {application.Id}.",
                CreatedByUserId = userId,
                CreatedAtUtc = DateTime.UtcNow,
                // Set only the foreign key. The application and product line
                // already exist; attaching their navigation graph as Added
                // would make EF Core attempt to insert them again.
                PesticideApplicationProductId = productLine.Id
            };

            // Mark only this new transaction as Added.
            // Do not traverse or mark related existing entities as Added.
            _db.Entry(stockOut).State = EntityState.Added;
        }

        // Complete the application in the same database transaction.
        application.Status = PesticideApplicationStatus.Completed;

        await _db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return Ok(MapToResponse(application));
    }







    private async Task<string?> ValidateApplicationAsync(
        Guid farmId,
        Guid fieldId,
        Guid fieldCropId,
        Guid workerId,
        DateTime applicationDateUtc,
        decimal areaTreatedHectares,
        Guid? weatherRecordId,
        List<ApplicationProductRequest>? products,
        CancellationToken cancellationToken)
    {
        if (farmId == Guid.Empty ||
            fieldId == Guid.Empty ||
            fieldCropId == Guid.Empty ||
            workerId == Guid.Empty)
        {
            return "Farm, field, crop assignment, and worker are required.";
        }

        if (products is null || products.Count == 0)
            return "At least one pesticide product is required.";

        if (applicationDateUtc == default)
            return "A valid application date is required.";

        if (areaTreatedHectares <= 0)
            return "Treated area must be greater than zero.";

        // 1. Validate farm.
        var farmExists = await _db.Farms.AnyAsync(
            f => f.Id == farmId && f.IsActive,
            cancellationToken);

        if (!farmExists)
            return "The farm does not exist or is inactive.";

        // 2. Validate field belongs to this farm and is active.
        var field = await _db.Fields
            .AsNoTracking()
            .FirstOrDefaultAsync(
                f => f.Id == fieldId &&
                     f.FarmId == farmId &&
                     f.IsActive,
                cancellationToken);

        if (field is null)
            return "The selected field does not exist, is inactive, or does not belong to this farm.";

        if (field.AreaHectares.HasValue &&
            areaTreatedHectares > field.AreaHectares.Value)
        {
            return "Treated area cannot exceed the field area.";
        }

        // 3. Validate crop assignment.
        var fieldCrop = await _db.FieldCrops
            .AsNoTracking()
            .FirstOrDefaultAsync(
                fc => fc.Id == fieldCropId &&
                      fc.FarmId == farmId &&
                      fc.FieldId == fieldId &&
                      fc.Status == FieldCropStatus.Planted,
                cancellationToken);

        if (fieldCrop is null)
        {
            return "The selected crop assignment is not currently planted on this field.";
        }

        var applicationDate = DateOnly.FromDateTime(
            applicationDateUtc);

        if (applicationDate < fieldCrop.PlantingDate)
            return "Application date cannot be before the crop planting date.";

        if (fieldCrop.ActualHarvestDate.HasValue &&
            applicationDate > fieldCrop.ActualHarvestDate.Value)
        {
            return "Application date cannot be after the actual harvest date.";
        }

        if (fieldCrop.AreaHectares.HasValue &&
            areaTreatedHectares > fieldCrop.AreaHectares.Value)
        {
            return "Treated area cannot exceed the crop assignment area.";
        }

        // 4. Validate worker and applicator qualification.
        var worker = await _db.Workers
            .AsNoTracking()
            .FirstOrDefaultAsync(
                w => w.Id == workerId &&
                     w.FarmId == farmId &&
                     w.IsActive,
                cancellationToken);

        if (worker is null)
            return "The selected worker does not exist, is inactive, or does not belong to this farm.";

        if (!worker.IsApplicator)
            return "The selected worker is not registered as a pesticide applicator.";

        if (string.IsNullOrWhiteSpace(worker.CertificationNumber))
            return "The selected applicator does not have a certification number recorded.";

        if (!worker.CertificationExpiryDate.HasValue ||
            worker.CertificationExpiryDate.Value.Date < applicationDateUtc.Date)
        {
            return "The selected applicator's certification is expired or missing for the application date.";
        }

        // 5. Validate optional weather record.
        if (weatherRecordId.HasValue)
        {
            var weather = await _db.WeatherRecords
                .AsNoTracking()
                .FirstOrDefaultAsync(
                    w => w.Id == weatherRecordId.Value &&
                         w.FarmId == farmId,
                    cancellationToken);

            if (weather is null)
                return "The selected weather record does not belong to this farm or does not exist.";

            if (weather.FieldId.HasValue &&
                weather.FieldId.Value != fieldId)
            {
                return "The selected weather record belongs to a different field.";
            }
        }

        // 6. Validate product lines.
        var productIds = products
            .Select(p => p.ProductId)
            .Distinct()
            .ToList();

        if (productIds.Any(id => id == Guid.Empty))
            return "Every product line must contain a valid product.";

        var activeProductCount = await _db.Products
            .CountAsync(
                p => productIds.Contains(p.Id) && p.IsActive,
                cancellationToken);

        if (activeProductCount != productIds.Count)
        {
            return "One or more selected products do not exist or are inactive.";
        }

        // Prevent duplicate product + batch lines in one application.
        var productBatchKeys = new HashSet<string>(
            StringComparer.OrdinalIgnoreCase);

        foreach (var product in products)
        {
            if (string.IsNullOrWhiteSpace(product.BatchNumber))
                return "Every product line must have a batch number.";

            if (product.QuantityUsed <= 0)
                return "Product quantity used must be greater than zero.";

            if (product.ApplicationRate <= 0)
                return "Application rate must be greater than zero.";

            if (string.IsNullOrWhiteSpace(product.RateUnit))
                return "Every product line must specify an application rate unit.";

            var key = $"{product.ProductId:N}|{product.BatchNumber.Trim()}";

            if (!productBatchKeys.Add(key))
            {
                return "The same product and batch cannot appear more than once in an application.";
            }
        }

        return null;
    }

    private static PesticideApplicationResponse MapToResponse(
        PesticideApplication application)
    {
        return new PesticideApplicationResponse
        {
            Id = application.Id,
            FarmId = application.FarmId,
            FieldId = application.FieldId,
            FieldName = application.Field.Name,
            FieldCropId = application.FieldCropId,
            CropName = application.FieldCrop.Crop.Name,
            WorkerId = application.WorkerId,
            WorkerName = application.Worker.FullName,
            ApplicationDateUtc = application.ApplicationDateUtc,
            TargetPest = application.TargetPest,
            ApplicationMethod = application.ApplicationMethod,
            Equipment = application.Equipment,
            AreaTreatedHectares = application.AreaTreatedHectares,
            WeatherRecordId = application.WeatherRecordId,
            Notes = application.Notes,
            Status = application.Status.ToString(),
            CreatedByUserId = application.CreatedByUserId,
            CreatedAtUtc = application.CreatedAtUtc,

            Products = application.Products
                .Select(p => new ApplicationProductResponse
                {
                    Id = p.Id,
                    ProductId = p.ProductId,
                    ProductName = p.Product.Name,
                    BatchNumber = p.BatchNumber,
                    QuantityUsed = p.QuantityUsed,
                    ApplicationRate = p.ApplicationRate,
                    RateUnit = p.RateUnit,
                    InventoryTransactionId =
                        p.InventoryTransaction?.Id
                })
                .ToList()
        };
    }

    private static string? Normalize(string? value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? null
            : value.Trim();
    }
}