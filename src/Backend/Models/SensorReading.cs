using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace Backend.Models;

public class SensorReading
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string? Id { get; set; }

    [BsonElement("sensorType")]
    public string SensorType { get; set; } = string.Empty;

    [BsonElement("sensorId")]
    public string SensorId { get; set; } = string.Empty;

    [BsonElement("location")]
    public string Location { get; set; } = string.Empty;

    [BsonElement("value")]
    public double Value { get; set; }

    [BsonElement("unit")]
    public string Unit { get; set; } = string.Empty;

    [BsonElement("timestamp")]
    public DateTime Timestamp { get; set; }
}

public class SensorReadingDto
{
    public string? Id { get; set; }
    public string SensorType { get; set; } = string.Empty;
    public string SensorId { get; set; } = string.Empty;
    public string Location { get; set; } = string.Empty;
    public double Value { get; set; }
    public string Unit { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
}

public class DashboardSensor
{
    public string SensorId { get; set; } = string.Empty;
    public string SensorType { get; set; } = string.Empty;
    public string Location { get; set; } = string.Empty;
    public string Unit { get; set; } = string.Empty;
    public double? LastValue { get; set; }
    public DateTime? LastTimestamp { get; set; }
    public double? AverageValue { get; set; }
}

public class PaginatedResult<T>
{
    public List<T> Data { get; set; } = new();
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);
}
