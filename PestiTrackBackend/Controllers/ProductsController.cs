
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PestiTrackBackend.Data;
using PestiTrackBackend.DTOs.Products;
using PestiTrackBackend.Models.Products;
using PestiTrackBackend.Security;

namespace PestiTrackBackend.Controllers;

[ApiController]
[Route("api/products")]
[Authorize]
public sealed class ProductsController : ControllerBase
{
    private readonly AppDbContext _db;

    public ProductsController(AppDbContext db)
    {
        _db = db;
    }

    // GET: api/products
    // Farm users see active products.
    // System administrators can see all products.
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<ProductResponse>>> GetAll(
        CancellationToken cancellationToken)
    {
        var query = _db.Products.AsNoTracking();

        if (!User.IsInRole(SystemRoleCodes.Administrator))
        {
            query = query.Where(product => product.IsActive);
        }

        var products = await query
            .OrderBy(product => product.Name)
            .Select(product => new ProductResponse
            {
                Id = product.Id,
                Name = product.Name,
                ActiveIngredient = product.ActiveIngredient,
                Manufacturer = product.Manufacturer,
                RegistrationNumber = product.RegistrationNumber,
                Formulation = product.Formulation,
                Concentration = product.Concentration,
                HazardClass = product.HazardClass,
                Description = product.Description,
                IsActive = product.IsActive,
                CreatedAtUtc = product.CreatedAtUtc
            })
            .ToListAsync(cancellationToken);

        return Ok(products);
    }

    // GET: api/products/{productId}
    [HttpGet("{productId:guid}")]
    public async Task<ActionResult<ProductResponse>> GetById(
        Guid productId,
        CancellationToken cancellationToken)
    {
        var query = _db.Products
            .AsNoTracking()
            .Where(product => product.Id == productId);

        if (!User.IsInRole(SystemRoleCodes.Administrator))
        {
            query = query.Where(product => product.IsActive);
        }

        var product = await query
            .Select(product => new ProductResponse
            {
                Id = product.Id,
                Name = product.Name,
                ActiveIngredient = product.ActiveIngredient,
                Manufacturer = product.Manufacturer,
                RegistrationNumber = product.RegistrationNumber,
                Formulation = product.Formulation,
                Concentration = product.Concentration,
                HazardClass = product.HazardClass,
                Description = product.Description,
                IsActive = product.IsActive,
                CreatedAtUtc = product.CreatedAtUtc
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (product is null)
        {
            return NotFound(new
            {
                message = "Product was not found."
            });
        }

        return Ok(product);
    }

    // POST: api/products
    [HttpPost]
    [Authorize(Roles = SystemRoleCodes.Administrator)]
    public async Task<ActionResult<ProductResponse>> Create(
        CreateProductRequest request,
        CancellationToken cancellationToken)
    {
        var product = new Product
        {
            Name = request.Name.Trim(),
            ActiveIngredient = Normalize(request.ActiveIngredient),
            Manufacturer = Normalize(request.Manufacturer),
            RegistrationNumber = Normalize(request.RegistrationNumber),
            Formulation = Normalize(request.Formulation),
            Concentration = Normalize(request.Concentration),
            HazardClass = Normalize(request.HazardClass),
            Description = Normalize(request.Description),
            IsActive = true,
            CreatedAtUtc = DateTime.UtcNow
        };

        _db.Products.Add(product);

        await _db.SaveChangesAsync(cancellationToken);

        var response = ToResponse(product);

        return CreatedAtAction(
            nameof(GetById),
            new { productId = product.Id },
            response);
    }

    // PUT: api/products/{productId}
    [HttpPut("{productId:guid}")]
    [Authorize(Roles = SystemRoleCodes.Administrator)]
    public async Task<ActionResult<ProductResponse>> Update(
        Guid productId,
        UpdateProductRequest request,
        CancellationToken cancellationToken)
    {
        var product = await _db.Products
            .FirstOrDefaultAsync(
                item => item.Id == productId,
                cancellationToken);

        if (product is null)
        {
            return NotFound(new
            {
                message = "Product was not found."
            });
        }

        product.Name = request.Name.Trim();
        product.ActiveIngredient = Normalize(request.ActiveIngredient);
        product.Manufacturer = Normalize(request.Manufacturer);
        product.RegistrationNumber = Normalize(request.RegistrationNumber);
        product.Formulation = Normalize(request.Formulation);
        product.Concentration = Normalize(request.Concentration);
        product.HazardClass = Normalize(request.HazardClass);
        product.Description = Normalize(request.Description);

        await _db.SaveChangesAsync(cancellationToken);

        return Ok(ToResponse(product));
    }

    // PATCH: api/products/{productId}/status
    [HttpPatch("{productId:guid}/status")]
    [Authorize(Roles = SystemRoleCodes.Administrator)]
    public async Task<ActionResult<ProductResponse>> UpdateStatus(
        Guid productId,
        UpdateProductStatusRequest request,
        CancellationToken cancellationToken)
    {
        var product = await _db.Products
            .FirstOrDefaultAsync(
                item => item.Id == productId,
                cancellationToken);

        if (product is null)
        {
            return NotFound(new
            {
                message = "Product was not found."
            });
        }

        product.IsActive = request.IsActive;

        await _db.SaveChangesAsync(cancellationToken);

        return Ok(ToResponse(product));
    }

    private static ProductResponse ToResponse(Product product)
    {
        return new ProductResponse
        {
            Id = product.Id,
            Name = product.Name,
            ActiveIngredient = product.ActiveIngredient,
            Manufacturer = product.Manufacturer,
            RegistrationNumber = product.RegistrationNumber,
            Formulation = product.Formulation,
            Concentration = product.Concentration,
            HazardClass = product.HazardClass,
            Description = product.Description,
            IsActive = product.IsActive,
            CreatedAtUtc = product.CreatedAtUtc
        };
    }

    private static string? Normalize(string? value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? null
            : value.Trim();
    }
}
