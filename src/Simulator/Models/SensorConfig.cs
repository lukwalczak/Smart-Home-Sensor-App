using System.Text.Json.Serialization;

namespace Simulator.Models;

public class SensorConfig
{
    [JsonPropertyName("sensorType")]
    public string SensorType { get; set; } = string.Empty;
    
    [JsonPropertyName("sensorId")]
    public string SensorId { get; set; } = string.Empty;
    
    [JsonPropertyName("location")]
    public string Location { get; set; } = string.Empty;
    
    [JsonPropertyName("unit")]
    public string Unit { get; set; } = string.Empty;
    
    [JsonPropertyName("enabled")]
    public bool Enabled { get; set; } = true;
    
    [JsonPropertyName("minValue")]
    public double MinValue { get; set; }
    
    [JsonPropertyName("maxValue")]
    public double MaxValue { get; set; }
    
    [JsonPropertyName("volatility")]
    public double Volatility { get; set; }
    
    [JsonPropertyName("intervalMs")]
    public int? IntervalMs { get; set; }
}

