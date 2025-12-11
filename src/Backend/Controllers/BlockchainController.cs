using Microsoft.AspNetCore.Mvc;
using Backend.Services;
using Backend.Blockchain;

namespace Backend.Controllers;

[ApiController]
[Route("api/[controller]")]
public class BlockchainController : ControllerBase
{
    private readonly BlockchainService _blockchainService;
    private readonly ILogger<BlockchainController> _logger;

    public BlockchainController(BlockchainService blockchainService, ILogger<BlockchainController> logger)
    {
        _blockchainService = blockchainService;
        _logger = logger;
    }

    [HttpGet("sensors")]
    public async Task<ActionResult<List<SensorTokenInfo>>> GetAllSensorTokens()
    {
        try
        {
            var tokens = await _blockchainService.GetAllSensorTokenInfoAsync();
            return Ok(tokens);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get sensor tokens");
            return StatusCode(500, new { error = "Failed to retrieve sensor tokens" });
        }
    }

    [HttpGet("sensors/{sensorId}")]
    public async Task<ActionResult<SensorTokenInfo>> GetSensorToken(string sensorId)
    {
        try
        {
            var token = await _blockchainService.GetSensorTokenInfoAsync(sensorId);
            if (token == null)
                return NotFound(new { error = $"Sensor {sensorId} not found or has no wallet" });

            return Ok(token);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get token info for sensor {SensorId}", sensorId);
            return StatusCode(500, new { error = "Failed to retrieve sensor token" });
        }
    }

    [HttpGet("contract")]
    public async Task<ActionResult<ContractInfo>> GetContractInfo()
    {
        try
        {
            var info = await _blockchainService.GetContractInfoAsync();
            if (info == null)
                return StatusCode(503, new { error = "Blockchain service not initialized" });

            return Ok(info);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get contract info");
            return StatusCode(500, new { error = "Failed to retrieve contract information" });
        }
    }

    [HttpPost("reward/{sensorId}")]
    public async Task<ActionResult> RewardSensor(string sensorId)
    {
        try
        {
            var success = await _blockchainService.RewardSensorAsync(sensorId);
            if (!success)
                return BadRequest(new { error = "Failed to reward sensor" });

            return Ok(new { message = $"Sensor {sensorId} rewarded successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to reward sensor {SensorId}", sensorId);
            return StatusCode(500, new { error = "Failed to reward sensor" });
        }
    }
}
