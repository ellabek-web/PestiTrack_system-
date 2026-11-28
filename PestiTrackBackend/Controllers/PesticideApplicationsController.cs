using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PestiTrackBackend.Data;
using PestiTrackBackend.DTOs.Applications;
using PestiTrackBackend.DTOs.Compliance;
using PestiTrackBackend.Models.Applications;
using PestiTrackBackend.Models.Crops;
using PestiTrackBackend.Models.Weather;
using PestiTrackBackend.Security;
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

    // GET: api/farms/{farmId}/applications/history
    [HttpGet("history")]
    [FarmPermission("applications.view")]
    public async Task<IActionResult> GetHistory(
        Guid farmId,
        [FromQuery] PesticideApplicationHistoryQueryRequest request,
        CancellationToken cancellationToken)
    {
        if (request.FromUtc.HasValue &&
            request.ToUtc.HasValue &&
            request.FromUtc.Value > request.ToUtc.Value)
        {
            return BadRequest(new
            {
                message = "FromUtc must be earlier than or equal to ToUtc."
            });
        }

        var query = _db.PesticideApplications
            .AsNoTracking()
            .Where(application => application.FarmId == farmId);

        if (request.FieldId.HasValue)
        {
            query = query.Where(application =>
                application.FieldId == request.FieldId.Value);
        }

        if (request.FieldCropId.HasValue)
        {
            query = query.Where(application =>
                application.FieldCropId == request.FieldCropId.Value);
        }

        if (request.WorkerId.HasValue)
        {
            query = query.Where(application =>
                application.WorkerId == request.WorkerId.Value);
        }

        if (request.ProductId.HasValue)
        {
            query = query.Where(application =>
                application.Products.Any(product =>
                    product.ProductId == request.ProductId.Value));
        }

        if (request.Status.HasValue)
        {
            query = query.Where(application =>
                application.Status == request.Status.Value);
        }

        if (request.FromUtc.HasValue)
        {
            var fromUtc = request.FromUtc.Value.UtcDateTime;
            query = query.Where(application =>
                application.ApplicationDateUtc >= fromUtc);
        }

        if (request.ToUtc.HasValue)
        {
            var toUtc = request.ToUtc.Value.UtcDateTime;
            query = query.Where(application =>
                application.ApplicationDateUtc <= toUtc);
        }

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var search = request.Search.Trim();

            query = query.Where(application =>
                application.TargetPest.Contains(search) ||
                application.ApplicationMethod.Contains(search) ||
                (application.Equipment != null &&
                    application.Equipment.Contains(search)) ||
                application.Field.Name.Contains(search) ||
                application.FieldCrop.Crop.Name.Contains(search) ||
                application.Worker.FullName.Contains(search) ||
                application.Products.Any(product =>
                    product.Product.Name.Contains(search) ||
                    product.BatchNumber.Contains(search)));
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderByDescending(application =>
                application.ApplicationDateUtc)
            .ThenByDescending(application =>
                application.CreatedAtUtc)
            .ThenByDescending(application =>
                application.Id)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(application =>
                new PesticideApplicationHistoryItemResponse
                {
                    Id = application.Id,
                    FarmId = application.FarmId,
                    FieldId = application.FieldId,
                    FieldName = application.Field.Name,
                    FieldCropId = application.FieldCropId,
                    CropName = application.FieldCrop.Crop.Name,
                    WorkerId = application.WorkerId,
                    WorkerName = application.Worker.FullName,
                    ApplicationDateUtc = new DateTimeOffset(
                        DateTime.SpecifyKind(
                            application.ApplicationDateUtc,
                            DateTimeKind.Utc)),
                    TargetPest = application.TargetPest,
                    ApplicationMethod =
                        application.ApplicationMethod,
                    AreaTreatedHectares =
                        application.AreaTreatedHectares,
                    Status =
                        application.Status ==
                        PesticideApplicationStatus.Draft
                            ? "Draft"
                            : "Completed",
                    ProductCount = application.Products.Count,
                    HasInventoryTraceability =
                        application.Products.Count > 0 &&
                        application.Products.All(product =>
                            product.InventoryTransaction != null),
                    CreatedByUserId =
                        application.CreatedByUserId,
                    CreatedByDisplayName =
                        application.CreatedByUser.DisplayName,
                    CreatedAtUtc = new DateTimeOffset(
                        DateTime.SpecifyKind(
                            application.CreatedAtUtc,
                            DateTimeKind.Utc))
                })
            .ToListAsync(cancellationToken);

        return Ok(new PesticideApplicationHistoryResponse
        {
            Page = request.Page,
            PageSize = request.PageSize,
            TotalCount = totalCount,
            TotalPages = (int)Math.Ceiling(
                totalCount / (double)request.PageSize),
            Items = items
        });
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

    // GET: api/farms/{farmId}/applications/{applicationId}/safety-check
    [HttpGet("{applicationId:guid}/safety-check")]
    [FarmPermission(PermissionCodes.ApplicationsView)]
    public async Task<ActionResult<ApplicationSafetyCheckResponse>> GetSafetyCheck(
        Guid farmId,
        Guid applicationId,
        CancellationToken cancellationToken)
    {
        var application = await _db.PesticideApplications
            .AsNoTracking()
            .Include(a => a.FieldCrop)
            .Include(a => a.Worker)
            .Include(a => a.WeatherRecord)
            .Include(a => a.Products)
                .ThenInclude(p => p.Product)
            .Include(a => a.Products)
                .ThenInclude(p => p.InventoryTransaction)
            .FirstOrDefaultAsync(
                a => a.Id == applicationId && a.FarmId == farmId,
                cancellationToken);

        if (application is null)
            return NotFound(new { message = "Pesticide application not found." });

        var checks = new List<SafetyCheckItem>();
        var applicationDate = DateOnly.FromDateTime(application.ApplicationDateUtc);

        // 1. Applicator certification check.
        if (!application.Worker.IsActive || !application.Worker.IsApplicator)
        {
            checks.Add(new SafetyCheckItem
            {
                Code = "applicator.eligibility",
                Severity = "Violation",
                Title = "Applicator eligibility",
                Message = $"{application.Worker.FullName} is not an active certified applicator."
            });
        }
        else if (string.IsNullOrWhiteSpace(application.Worker.CertificationNumber) ||
                 !application.Worker.CertificationExpiryDate.HasValue ||
                 application.Worker.CertificationExpiryDate.Value.Date < application.ApplicationDateUtc.Date)
        {
            checks.Add(new SafetyCheckItem
            {
                Code = "applicator.certification",
                Severity = "Violation",
                Title = "Applicator certification",
                Message = $"{application.Worker.FullName}'s certification is missing or expired for the application date."
            });
        }
        else
        {
            checks.Add(new SafetyCheckItem
            {
                Code = "applicator.certification",
                Severity = "Pass",
                Title = "Applicator certification",
                Message = $"{application.Worker.FullName} holds valid certification {application.Worker.CertificationNumber} through {application.Worker.CertificationExpiryDate.Value:yyyy-MM-dd}."
            });
        }

        // 2. Product registration, max rate, wind threshold, batch expiry, REI, and PHI.
        int? maxReiHours = null;
        int? maxPhiDays = null;

        foreach (var line in application.Products)
        {
            var product = line.Product;

            if (product.RegistrationExpiryDate.HasValue)
            {
                if (product.RegistrationExpiryDate.Value < applicationDate)
                {
                    checks.Add(new SafetyCheckItem
                    {
                        Code = "product.registration",
                        Severity = "Violation",
                        Title = $"{product.Name} registration",
                        Message = $"Registration expired on {product.RegistrationExpiryDate.Value:yyyy-MM-dd}."
                    });
                }
                else if (product.RegistrationExpiryDate.Value <= applicationDate.AddDays(30))
                {
                    checks.Add(new SafetyCheckItem
                    {
                        Code = "product.registration",
                        Severity = "Warning",
                        Title = $"{product.Name} registration",
                        Message = $"Registration expires soon on {product.RegistrationExpiryDate.Value:yyyy-MM-dd}."
                    });
                }
                else
                {
                    checks.Add(new SafetyCheckItem
                    {
                        Code = "product.registration",
                        Severity = "Pass",
                        Title = $"{product.Name} registration",
                        Message = $"Registration is valid through {product.RegistrationExpiryDate.Value:yyyy-MM-dd}."
                    });
                }
            }

            if (product.MaxApplicationRate.HasValue)
            {
                var unitMatches =
                    string.IsNullOrWhiteSpace(product.MaxApplicationRateUnit) ||
                    string.Equals(
                        product.MaxApplicationRateUnit.Trim(),
                        line.RateUnit.Trim(),
                        StringComparison.OrdinalIgnoreCase);

                if (!unitMatches)
                {
                    checks.Add(new SafetyCheckItem
                    {
                        Code = "product.max-rate",
                        Severity = "Warning",
                        Title = $"{product.Name} rate unit",
                        Message = $"Applied rate uses '{line.RateUnit}' while label maximum is {product.MaxApplicationRate.Value} {product.MaxApplicationRateUnit}. Verify unit conversion."
                    });
                }
                else if (line.ApplicationRate > product.MaxApplicationRate.Value)
                {
                    checks.Add(new SafetyCheckItem
                    {
                        Code = "product.max-rate",
                        Severity = "Violation",
                        Title = $"{product.Name} maximum rate",
                        Message = $"Applied rate ({line.ApplicationRate} {line.RateUnit}) exceeds label maximum ({product.MaxApplicationRate.Value} {product.MaxApplicationRateUnit ?? line.RateUnit})."
                    });
                }
                else
                {
                    checks.Add(new SafetyCheckItem
                    {
                        Code = "product.max-rate",
                        Severity = "Pass",
                        Title = $"{product.Name} maximum rate",
                        Message = $"Applied rate ({line.ApplicationRate} {line.RateUnit}) is within label maximum ({product.MaxApplicationRate.Value} {product.MaxApplicationRateUnit ?? line.RateUnit})."
                    });
                }
            }

            if (product.MaxWindSpeedMetersPerSecond.HasValue)
            {
                if (application.WeatherRecord?.WindSpeedMetersPerSecond is null)
                {
                    checks.Add(new SafetyCheckItem
                    {
                        Code = "weather.wind-speed",
                        Severity = "Warning",
                        Title = $"{product.Name} wind limit",
                        Message = $"Product specifies a {product.MaxWindSpeedMetersPerSecond.Value} m/s wind limit, but no wind speed is recorded on this application."
                    });
                }
                else if (application.WeatherRecord.WindSpeedMetersPerSecond.Value >
                         product.MaxWindSpeedMetersPerSecond.Value)
                {
                    checks.Add(new SafetyCheckItem
                    {
                        Code = "weather.wind-speed",
                        Severity = "Violation",
                        Title = $"{product.Name} wind limit",
                        Message = $"Recorded wind speed ({application.WeatherRecord.WindSpeedMetersPerSecond.Value} m/s) exceeds label maximum ({product.MaxWindSpeedMetersPerSecond.Value} m/s)."
                    });
                }
                else
                {
                    checks.Add(new SafetyCheckItem
                    {
                        Code = "weather.wind-speed",
                        Severity = "Pass",
                        Title = $"{product.Name} wind limit",
                        Message = $"Recorded wind speed ({application.WeatherRecord.WindSpeedMetersPerSecond.Value} m/s) is within label limit ({product.MaxWindSpeedMetersPerSecond.Value} m/s)."
                    });
                }
            }

            var batchExpiry = line.InventoryTransaction?.ExpiryDate;
            if (!batchExpiry.HasValue)
            {
                batchExpiry = await _db.InventoryTransactions
                    .AsNoTracking()
                    .Where(t =>
                        t.FarmId == farmId &&
                        t.ProductId == line.ProductId &&
                        t.BatchNumber == line.BatchNumber)
                    .Select(t => t.ExpiryDate)
                    .FirstOrDefaultAsync(cancellationToken);
            }

            if (batchExpiry.HasValue)
            {
                if (batchExpiry.Value < applicationDate)
                {
                    checks.Add(new SafetyCheckItem
                    {
                        Code = "inventory.batch-expiry",
                        Severity = "Violation",
                        Title = $"{product.Name} batch {line.BatchNumber}",
                        Message = $"Batch expired on {batchExpiry.Value:yyyy-MM-dd} before the application date."
                    });
                }
                else
                {
                    checks.Add(new SafetyCheckItem
                    {
                        Code = "inventory.batch-expiry",
                        Severity = "Pass",
                        Title = $"{product.Name} batch {line.BatchNumber}",
                        Message = $"Batch is valid through {batchExpiry.Value:yyyy-MM-dd}."
                    });
                }
            }

            if (product.ReEntryIntervalHours.HasValue)
            {
                maxReiHours = maxReiHours.HasValue
                    ? Math.Max(maxReiHours.Value, product.ReEntryIntervalHours.Value)
                    : product.ReEntryIntervalHours.Value;
            }

            if (product.PreHarvestIntervalDays.HasValue)
            {
                maxPhiDays = maxPhiDays.HasValue
                    ? Math.Max(maxPhiDays.Value, product.PreHarvestIntervalDays.Value)
                    : product.PreHarvestIntervalDays.Value;
            }
        }

        DateTimeOffset? reEntryAllowedAtUtc = maxReiHours.HasValue
            ? new DateTimeOffset(
                DateTime.SpecifyKind(
                    application.ApplicationDateUtc.AddHours(maxReiHours.Value),
                    DateTimeKind.Utc))
            : null;

        DateOnly? earliestSafeHarvestDate = maxPhiDays.HasValue
            ? applicationDate.AddDays(maxPhiDays.Value)
            : null;

        if (earliestSafeHarvestDate.HasValue &&
            application.FieldCrop.ExpectedHarvestDate.HasValue &&
            application.FieldCrop.ExpectedHarvestDate.Value < earliestSafeHarvestDate.Value)
        {
            checks.Add(new SafetyCheckItem
            {
                Code = "crop.pre-harvest-interval",
                Severity = "Warning",
                Title = "Pre-harvest interval conflict",
                Message = $"Earliest safe harvest date ({earliestSafeHarvestDate.Value:yyyy-MM-dd}) is after the crop's expected harvest date ({application.FieldCrop.ExpectedHarvestDate.Value:yyyy-MM-dd})."
            });
        }
        else if (earliestSafeHarvestDate.HasValue)
        {
            checks.Add(new SafetyCheckItem
            {
                Code = "crop.pre-harvest-interval",
                Severity = "Pass",
                Title = "Pre-harvest interval",
                Message = $"Earliest safe harvest date is {earliestSafeHarvestDate.Value:yyyy-MM-dd}."
            });
        }

        var violationCount = checks.Count(c => c.Severity == "Violation");
        var warningCount = checks.Count(c => c.Severity == "Warning");

        return Ok(new ApplicationSafetyCheckResponse
        {
            ApplicationId = application.Id,
            IsCompliant = violationCount == 0,
            ViolationCount = violationCount,
            WarningCount = warningCount,
            ReEntryAllowedAtUtc = reEntryAllowedAtUtc,
            EarliestSafeHarvestDate = earliestSafeHarvestDate,
            Checks = checks
        });
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
            request.WeatherCondition,
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
            WeatherCondition = Normalize(request.WeatherCondition),
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
            request.WeatherCondition,
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
        application.WeatherCondition = Normalize(request.WeatherCondition);
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
            .Include(a => a.WeatherRecord)
            .Include(a => a.CreatedByUser)
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
            application.WeatherCondition,
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

            var batchExpiry = expiryDates[0];
            if (batchExpiry.HasValue &&
                batchExpiry.Value < DateOnly.FromDateTime(application.ApplicationDateUtc))
            {
                return Conflict(new
                {
                    message = $"Batch '{batchNumber}' for product '{productLine.Product.Name}' expired on {batchExpiry.Value:yyyy-MM-dd} and cannot be applied."
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
        string? weatherCondition,
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
        WeatherRecord? weather = null;

        if (weatherRecordId.HasValue)
        {
            weather = await _db.WeatherRecords
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

        // 5b. Validate the recorded weather condition, when one is given.
        if (weatherCondition is not null &&
            !WeatherConditions.IsValid(weatherCondition))
        {{
            return "The selected weather condition is not valid.";
        }}

        // 6. Validate product lines and authoritative label safety restrictions.
        var productIds = products
            .Select(p => p.ProductId)
            .Distinct()
            .ToList();

        if (productIds.Any(id => id == Guid.Empty))
            return "Every product line must contain a valid product.";

        var activeProducts = await _db.Products
            .AsNoTracking()
            .Where(p => productIds.Contains(p.Id) && p.IsActive)
            .ToDictionaryAsync(p => p.Id, cancellationToken);

        if (activeProducts.Count != productIds.Count)
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

            var catalogProduct = activeProducts[product.ProductId];

            if (catalogProduct.RegistrationExpiryDate.HasValue &&
                catalogProduct.RegistrationExpiryDate.Value < applicationDate)
            {
                return $"Product '{catalogProduct.Name}' registration expired on {catalogProduct.RegistrationExpiryDate.Value:yyyy-MM-dd}.";
            }

            if (catalogProduct.MaxApplicationRate.HasValue)
            {
                var unitMatches =
                    string.IsNullOrWhiteSpace(catalogProduct.MaxApplicationRateUnit) ||
                    string.Equals(
                        catalogProduct.MaxApplicationRateUnit.Trim(),
                        product.RateUnit.Trim(),
                        StringComparison.OrdinalIgnoreCase);

                if (unitMatches &&
                    product.ApplicationRate > catalogProduct.MaxApplicationRate.Value)
                {
                    return $"Application rate for '{catalogProduct.Name}' ({product.ApplicationRate} {product.RateUnit}) exceeds the label maximum ({catalogProduct.MaxApplicationRate.Value} {catalogProduct.MaxApplicationRateUnit ?? product.RateUnit}).";
                }
            }

            if (weather?.WindSpeedMetersPerSecond is decimal windSpeed &&
                catalogProduct.MaxWindSpeedMetersPerSecond.HasValue &&
                windSpeed > catalogProduct.MaxWindSpeedMetersPerSecond.Value)
            {
                return $"Recorded wind speed ({windSpeed} m/s) exceeds the label maximum for '{catalogProduct.Name}' ({catalogProduct.MaxWindSpeedMetersPerSecond.Value} m/s).";
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
            WeatherCondition = application.WeatherCondition,
            Notes = application.Notes,
            Status = application.Status.ToString(),
            CreatedByUserId = application.CreatedByUserId,
            CreatedByDisplayName =
                application.CreatedByUser.DisplayName,
            CreatedByEmail =
                application.CreatedByUser.Email,
            CreatedAtUtc = application.CreatedAtUtc,
            Weather = application.WeatherRecord is null
                ? null
                : new ApplicationWeatherResponse
                {
                    Id = application.WeatherRecord.Id,
                    RecordedAtUtc = new DateTimeOffset(
                        DateTime.SpecifyKind(
                            application.WeatherRecord.RecordedAtUtc,
                            DateTimeKind.Utc)),
                    TemperatureCelsius =
                        application.WeatherRecord.TemperatureCelsius,
                    HumidityPercentage =
                        application.WeatherRecord.HumidityPercentage,
                    RainfallMillimeters =
                        application.WeatherRecord.RainfallMillimeters,
                    WindSpeedMetersPerSecond =
                        application.WeatherRecord.WindSpeedMetersPerSecond,
                    WindDirectionDegrees =
                        application.WeatherRecord.WindDirectionDegrees,
                    Conditions =
                        application.WeatherRecord.Conditions,
                    Source =
                        application.WeatherRecord.Source
                },

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
                        p.InventoryTransaction?.Id,
                    InventoryTransaction =
                        p.InventoryTransaction is null
                            ? null
                            : new ApplicationInventoryTransactionResponse
                            {
                                Id = p.InventoryTransaction.Id,
                                TransactionType =
                                    p.InventoryTransaction.TransactionType
                                        .ToString(),
                                Quantity =
                                    p.InventoryTransaction.Quantity,
                                BatchNumber =
                                    p.InventoryTransaction.BatchNumber,
                                ExpiryDate =
                                    p.InventoryTransaction.ExpiryDate,
                                Reference =
                                    p.InventoryTransaction.Reference,
                                Reason =
                                    p.InventoryTransaction.Reason,
                                CreatedByUserId =
                                    p.InventoryTransaction.CreatedByUserId,
                                CreatedAtUtc = new DateTimeOffset(
                                    DateTime.SpecifyKind(
                                        p.InventoryTransaction.CreatedAtUtc,
                                        DateTimeKind.Utc))
                            }
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