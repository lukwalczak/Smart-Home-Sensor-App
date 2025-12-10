namespace Simulator.Models;

public class SensorMessage
{
    public string SensorType { get; set; } = string.Empty;
    public string SensorId { get; set; } = string.Empty;
    public string Location { get; set; } = string.Empty;
    public double Value { get; set; }
    public string Unit { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
}

