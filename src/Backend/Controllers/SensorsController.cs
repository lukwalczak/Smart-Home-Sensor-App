using System.Globalization;
using System.Text;
using CsvHelper;
using CsvHelper.Configuration;
using Microsoft.AspNetCore.Mvc;
using Backend.Models;
using Backend.Services;

namespace Backend.Controllers;

[ApiController]
[Route("api/[controller]")]
public class SensorsController : ControllerBase
{
    private readonly MongoDbService _mongoDb;
    private readonly ILogger<SensorsController> _logger;

    public SensorsController(MongoDbService mongoDb, ILogger<SensorsController> logger)
    {
        _mongoDb = mongoDb;
        _logger = logger;
    }

    [HttpGet]
    public async Task<ActionResult<PaginatedResult<SensorReadingDto>>> GetReadings(
        [FromQuery] DateTime? dateFrom,
        [FromQuery] DateTime? dateTo,
        [FromQuery] string? sensorType,
        [FromQuery] string? sensorId,
        [FromQuery] string sortBy = "timestamp",
        [FromQuery] string sortOrder = "desc",
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50)
    {
        var result = await _mongoDb.GetReadingsAsync(
            dateFrom, dateTo, sensorType, sensorId,
            sortBy, sortOrder, page, pageSize);

        var dto = new PaginatedResult<SensorReadingDto>
        {
            Data = result.Data.Select(r => new SensorReadingDto
            {
                Id = r.Id,
                SensorType = r.SensorType,
                SensorId = r.SensorId,
                Location = r.Location,
                Value = r.Value,
                Unit = r.Unit,
                Timestamp = r.Timestamp
            }).ToList(),
            TotalCount = result.TotalCount,
            Page = result.Page,
            PageSize = result.PageSize
        };

        return Ok(dto);
    }

    [HttpGet("dashboard")]
    public async Task<ActionResult<List<DashboardSensor>>> GetDashboard()
    {
        var data = await _mongoDb.GetDashboardDataAsync();
        return Ok(data);
    }

    [HttpGet("types")]
    public async Task<ActionResult<List<string>>> GetSensorTypes()
    {
        var types = await _mongoDb.GetSensorTypesAsync();
        return Ok(types);
    }

    [HttpGet("ids")]
    public async Task<ActionResult<List<string>>> GetSensorIds([FromQuery] string? sensorType)
    {
        var ids = await _mongoDb.GetSensorIdsAsync(sensorType);
        return Ok(ids);
    }

    [HttpGet("export/csv")]
    public async Task<IActionResult> ExportCsv(
        [FromQuery] DateTime? dateFrom,
        [FromQuery] DateTime? dateTo,
        [FromQuery] string? sensorType,
        [FromQuery] string? sensorId,
        [FromQuery] string sortBy = "timestamp",
        [FromQuery] string sortOrder = "desc")
    {
        var readings = await _mongoDb.GetAllFilteredAsync(
            dateFrom, dateTo, sensorType, sensorId, sortBy, sortOrder);

        var records = readings.Select(r => new
        {
            r.SensorType,
            r.SensorId,
            r.Location,
            r.Value,
            r.Unit,
            Timestamp = r.Timestamp.ToString("yyyy-MM-dd HH:mm:ss")
        });

        using var memoryStream = new MemoryStream();
        using var writer = new StreamWriter(memoryStream, Encoding.UTF8);
        using var csv = new CsvWriter(writer, new CsvConfiguration(CultureInfo.InvariantCulture));

        csv.WriteRecords(records);
        await writer.FlushAsync();

        var bytes = memoryStream.ToArray();
        return File(bytes, "text/csv", $"sensors_{DateTime.Now:yyyyMMdd_HHmmss}.csv");
    }

    [HttpGet("export/json")]
    public async Task<IActionResult> ExportJson(
        [FromQuery] DateTime? dateFrom,
        [FromQuery] DateTime? dateTo,
        [FromQuery] string? sensorType,
        [FromQuery] string? sensorId,
        [FromQuery] string sortBy = "timestamp",
        [FromQuery] string sortOrder = "desc")
    {
        var readings = await _mongoDb.GetAllFilteredAsync(
            dateFrom, dateTo, sensorType, sensorId, sortBy, sortOrder);

        var dto = readings.Select(r => new SensorReadingDto
        {
            Id = r.Id,
            SensorType = r.SensorType,
            SensorId = r.SensorId,
            Location = r.Location,
            Value = r.Value,
            Unit = r.Unit,
            Timestamp = r.Timestamp
        });

        return Ok(dto);
    }
}
