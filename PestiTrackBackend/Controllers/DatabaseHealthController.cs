
using Microsoft.AspNetCore.Mvc;
using PestiTrackBackend.Data;
using Microsoft.EntityFrameworkCore;


namespace PestiTrackBackend.Controllers;

[ApiController]
[Route("api/database-health")]
public sealed class DatabaseHealthController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly IWebHostEnvironment _environment;
    private readonly ILogger<DatabaseHealthController> _logger;

    public DatabaseHealthController(
        AppDbContext db,
        IWebHostEnvironment environment,
        ILogger<DatabaseHealthController> logger)
    {
        _db = db;
        _environment = environment;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> Check(CancellationToken cancellationToken)
    {
        // This diagnostic endpoint is only available during development.
        if (!_environment.IsDevelopment())
            return NotFound();

        try
        {
            bool connected = await _db.Database.CanConnectAsync(
                cancellationToken);

            if (!connected)
            {
                return StatusCode(503, new
                {
                    status = "Unreachable",
                    message = "Could not connect to the configured database."
                });
            }

            return Ok(new
            {
                status = "Connected",
                database = _db.Database.GetDbConnection().Database,
                provider = _db.Database.ProviderName
            });
        }
       catch (Exception ex)
            {
                _logger.LogError(ex, "Database connectivity check failed.");

                return StatusCode(503, new
                {
                    status = "Unreachable",
                    message = ex.Message,
                    innerException = ex.InnerException?.Message
                });
            }
    }
}
