
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PestiTrackBackend.Data;
using PestiTrackBackend.DTOs.Weather;
using PestiTrackBackend.Models.Weather;
using PestiTrackBackend.Security.Authorization;

namespace PestiTrackBackend.Controllers;

[ApiController]
[Route("api/farms/{farmId:guid}/weather-records")]
[Authorize]
public sealed class WeatherRecordsController : ControllerBase
{
    private readonly AppDbContext _db;

    public WeatherRecordsController(AppDbContext db)
    {
        _db = db;
    }

    // GET: api/farms/{farmId}/weather-records
    [HttpGet]
    [FarmPermission("weather.view")]
    public async Task<ActionResult<IReadOnlyList<WeatherRecordResponse>>> GetAll(
        Guid farmId,
        [FromQuery] WeatherRecordQueryRequest query)
    {
        var farmExists = await _db.Farms
            .AsNoTracking()
            .AnyAsync(farm => farm.Id == farmId && farm.IsActive);

        if (!farmExists)
            return NotFound(new { message = "Farm not found or inactive." });

        if (query.FromUtc.HasValue &&
            query.ToUtc.HasValue &&
            query.FromUtc.Value > query.ToUtc.Value)
        {
            return BadRequest(new
            {
                message = "FromUtc must not be later than ToUtc."
            });
        }

        var recordsQuery = _db.WeatherRecords
            .AsNoTracking()
            .Where(record => record.FarmId == farmId);

        if (query.FieldId.HasValue)
        {
            recordsQuery = recordsQuery.Where(
                record => record.FieldId == query.FieldId.Value);
        }

        if (query.FromUtc.HasValue)
        {
            var fromUtc = query.FromUtc.Value.UtcDateTime;

            recordsQuery = recordsQuery.Where(
                record => record.RecordedAtUtc >= fromUtc);
        }

        if (query.ToUtc.HasValue)
        {
            var toUtc = query.ToUtc.Value.UtcDateTime;

            recordsQuery = recordsQuery.Where(
                record => record.RecordedAtUtc <= toUtc);
        }

        var records = await recordsQuery
            .OrderByDescending(record => record.RecordedAtUtc)
            .Select(record => new WeatherRecordResponse
            {
                Id = record.Id,
                FarmId = record.FarmId,
                FieldId = record.FieldId,
                RecordedAtUtc = new DateTimeOffset(
                    DateTime.SpecifyKind(
                        record.RecordedAtUtc,
                        DateTimeKind.Utc)),
                TemperatureCelsius = record.TemperatureCelsius,
                HumidityPercentage = record.HumidityPercentage,
                RainfallMillimeters = record.RainfallMillimeters,
                WindSpeedMetersPerSecond =
                    record.WindSpeedMetersPerSecond,
                WindDirectionDegrees = record.WindDirectionDegrees,
                Conditions = record.Conditions,
                Source = record.Source,
                Notes = record.Notes,
                CreatedAtUtc = new DateTimeOffset(
                    DateTime.SpecifyKind(
                        record.CreatedAtUtc,
                        DateTimeKind.Utc))
            })
            .ToListAsync();

        return Ok(records);
    }

    // GET: api/farms/{farmId}/weather-records/{weatherRecordId}
    [HttpGet("{weatherRecordId:guid}")]
    [FarmPermission("weather.view")]
    public async Task<ActionResult<WeatherRecordResponse>> GetById(
        Guid farmId,
        Guid weatherRecordId)
    {
        var farmExists = await _db.Farms
            .AsNoTracking()
            .AnyAsync(farm => farm.Id == farmId && farm.IsActive);

        if (!farmExists)
            return NotFound(new { message = "Farm not found or inactive." });

        var record = await _db.WeatherRecords
            .AsNoTracking()
            .FirstOrDefaultAsync(record =>
                record.Id == weatherRecordId &&
                record.FarmId == farmId);

        if (record is null)
            return NotFound(new { message = "Weather record not found." });

        return Ok(MapResponse(record));
    }

    // POST: api/farms/{farmId}/weather-records
    [HttpPost]
    [FarmPermission("weather.manage")]
    public async Task<ActionResult<WeatherRecordResponse>> Create(
        Guid farmId,
        [FromBody] CreateWeatherRecordRequest request)
    {
        var farmExists = await _db.Farms
            .AnyAsync(farm => farm.Id == farmId && farm.IsActive);

        if (!farmExists)
            return NotFound(new { message = "Farm not found or inactive." });

        if (!await ValidateFieldAsync(farmId, request.FieldId))
        {
            return BadRequest(new
            {
                message = "The specified field does not exist, is inactive, or does not belong to this farm."
            });
        }

        var record = new WeatherRecord
        {
            FarmId = farmId,
            FieldId = request.FieldId,
            RecordedAtUtc = request.RecordedAtUtc!.Value.UtcDateTime,
            TemperatureCelsius = request.TemperatureCelsius,
            HumidityPercentage = request.HumidityPercentage,
            RainfallMillimeters = request.RainfallMillimeters,
            WindSpeedMetersPerSecond =
                request.WindSpeedMetersPerSecond,
            WindDirectionDegrees = request.WindDirectionDegrees,
            Conditions = Normalize(request.Conditions),
            Source = Normalize(request.Source) ?? "Manual",
            Notes = Normalize(request.Notes),
            CreatedAtUtc = DateTime.UtcNow
        };

        _db.WeatherRecords.Add(record);
        await _db.SaveChangesAsync();

        var response = MapResponse(record);

        return CreatedAtAction(
            nameof(GetById),
            new
            {
                farmId,
                weatherRecordId = record.Id
            },
            response);
    }

    // PUT: api/farms/{farmId}/weather-records/{weatherRecordId}
    [HttpPut("{weatherRecordId:guid}")]
    [FarmPermission("weather.manage")]
    public async Task<ActionResult<WeatherRecordResponse>> Update(
        Guid farmId,
        Guid weatherRecordId,
        [FromBody] UpdateWeatherRecordRequest request)
    {
        var farmExists = await _db.Farms
            .AnyAsync(farm => farm.Id == farmId && farm.IsActive);

        if (!farmExists)
            return NotFound(new { message = "Farm not found or inactive." });

        var record = await _db.WeatherRecords
            .FirstOrDefaultAsync(record =>
                record.Id == weatherRecordId &&
                record.FarmId == farmId);

        if (record is null)
            return NotFound(new { message = "Weather record not found." });

        if (!await ValidateFieldAsync(farmId, request.FieldId))
        {
            return BadRequest(new
            {
                message = "The specified field does not exist, is inactive, or does not belong to this farm."
            });
        }

        record.FieldId = request.FieldId;
        record.RecordedAtUtc = request.RecordedAtUtc!.Value.UtcDateTime;
        record.TemperatureCelsius = request.TemperatureCelsius;
        record.HumidityPercentage = request.HumidityPercentage;
        record.RainfallMillimeters = request.RainfallMillimeters;
        record.WindSpeedMetersPerSecond =
            request.WindSpeedMetersPerSecond;
        record.WindDirectionDegrees = request.WindDirectionDegrees;
        record.Conditions = Normalize(request.Conditions);
        record.Source = Normalize(request.Source) ?? record.Source;
        record.Notes = Normalize(request.Notes);

        await _db.SaveChangesAsync();

        return Ok(MapResponse(record));
    }

    private async Task<bool> ValidateFieldAsync(
        Guid farmId,
        Guid? fieldId)
    {
        if (!fieldId.HasValue)
            return true;

        return await _db.Fields
            .AsNoTracking()
            .AnyAsync(field =>
                field.Id == fieldId.Value &&
                field.FarmId == farmId &&
                field.IsActive);
    }

    private static string? Normalize(string? value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? null
            : value.Trim();
    }

    private static WeatherRecordResponse MapResponse(
        WeatherRecord record)
    {
        return new WeatherRecordResponse
        {
            Id = record.Id,
            FarmId = record.FarmId,
            FieldId = record.FieldId,
            RecordedAtUtc = new DateTimeOffset(
                DateTime.SpecifyKind(
                    record.RecordedAtUtc,
                    DateTimeKind.Utc)),
            TemperatureCelsius = record.TemperatureCelsius,
            HumidityPercentage = record.HumidityPercentage,
            RainfallMillimeters = record.RainfallMillimeters,
            WindSpeedMetersPerSecond =
                record.WindSpeedMetersPerSecond,
            WindDirectionDegrees = record.WindDirectionDegrees,
            Conditions = record.Conditions,
            Source = record.Source,
            Notes = record.Notes,
            CreatedAtUtc = new DateTimeOffset(
                DateTime.SpecifyKind(
                    record.CreatedAtUtc,
                    DateTimeKind.Utc))
        };
    }
}
