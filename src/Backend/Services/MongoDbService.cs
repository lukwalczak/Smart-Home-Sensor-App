using MongoDB.Driver;
using MongoDB.Bson;
using Backend.Models;

namespace Backend.Services;

public class MongoDbService
{
    private readonly IMongoCollection<SensorReading> _readings;
    private readonly ILogger<MongoDbService> _logger;

    public MongoDbService(IConfiguration config, ILogger<MongoDbService> logger)
    {
        _logger = logger;
        var connectionString = config["MongoDB:ConnectionString"] ?? "mongodb://localhost:27017";
        var databaseName = config["MongoDB:DatabaseName"] ?? "SmartHome";

        var client = new MongoClient(connectionString);
        var database = client.GetDatabase(databaseName);
        _readings = database.GetCollection<SensorReading>("sensor_readings");

        // Create indexes
        var indexKeys = Builders<SensorReading>.IndexKeys
            .Descending(r => r.Timestamp)
            .Ascending(r => r.SensorType)
            .Ascending(r => r.SensorId);
        _readings.Indexes.CreateOne(new CreateIndexModel<SensorReading>(indexKeys));

        _logger.LogInformation("MongoDB connected to {Database}", databaseName);
    }

    public async Task InsertReadingAsync(SensorReading reading)
    {
        await _readings.InsertOneAsync(reading);
        _logger.LogDebug("Inserted reading for sensor {SensorId}", reading.SensorId);
    }

    public async Task<PaginatedResult<SensorReading>> GetReadingsAsync(
        DateTime? dateFrom, DateTime? dateTo,
        string? sensorType, string? sensorId,
        string sortBy = "timestamp", string sortOrder = "desc",
        int page = 1, int pageSize = 50)
    {
        var filterBuilder = Builders<SensorReading>.Filter;
        var filters = new List<FilterDefinition<SensorReading>>();

        if (dateFrom.HasValue)
            filters.Add(filterBuilder.Gte(r => r.Timestamp, dateFrom.Value));
        if (dateTo.HasValue)
            filters.Add(filterBuilder.Lte(r => r.Timestamp, dateTo.Value));
        if (!string.IsNullOrEmpty(sensorType))
            filters.Add(filterBuilder.Eq(r => r.SensorType, sensorType));
        if (!string.IsNullOrEmpty(sensorId))
            filters.Add(filterBuilder.Eq(r => r.SensorId, sensorId));

        var filter = filters.Count > 0
            ? filterBuilder.And(filters)
            : filterBuilder.Empty;

        var sortDefinition = sortOrder.ToLower() == "asc"
            ? Builders<SensorReading>.Sort.Ascending(sortBy)
            : Builders<SensorReading>.Sort.Descending(sortBy);

        var totalCount = await _readings.CountDocumentsAsync(filter);
        var data = await _readings.Find(filter)
            .Sort(sortDefinition)
            .Skip((page - 1) * pageSize)
            .Limit(pageSize)
            .ToListAsync();

        return new PaginatedResult<SensorReading>
        {
            Data = data,
            TotalCount = (int)totalCount,
            Page = page,
            PageSize = pageSize
        };
    }

    public async Task<List<SensorReading>> GetAllFilteredAsync(
        DateTime? dateFrom, DateTime? dateTo,
        string? sensorType, string? sensorId,
        string sortBy = "timestamp", string sortOrder = "desc")
    {
        var filterBuilder = Builders<SensorReading>.Filter;
        var filters = new List<FilterDefinition<SensorReading>>();

        if (dateFrom.HasValue)
            filters.Add(filterBuilder.Gte(r => r.Timestamp, dateFrom.Value));
        if (dateTo.HasValue)
            filters.Add(filterBuilder.Lte(r => r.Timestamp, dateTo.Value));
        if (!string.IsNullOrEmpty(sensorType))
            filters.Add(filterBuilder.Eq(r => r.SensorType, sensorType));
        if (!string.IsNullOrEmpty(sensorId))
            filters.Add(filterBuilder.Eq(r => r.SensorId, sensorId));

        var filter = filters.Count > 0
            ? filterBuilder.And(filters)
            : filterBuilder.Empty;

        var sortDefinition = sortOrder.ToLower() == "asc"
            ? Builders<SensorReading>.Sort.Ascending(sortBy)
            : Builders<SensorReading>.Sort.Descending(sortBy);

        return await _readings.Find(filter)
            .Sort(sortDefinition)
            .ToListAsync();
    }

    public async Task<List<DashboardSensor>> GetDashboardDataAsync()
    {
        var pipeline = new[]
        {
            new BsonDocument("$sort", new BsonDocument("timestamp", -1)),
            new BsonDocument("$group", new BsonDocument
            {
                { "_id", "$sensorId" },
                { "sensorType", new BsonDocument("$first", "$sensorType") },
                { "location", new BsonDocument("$first", "$location") },
                { "unit", new BsonDocument("$first", "$unit") },
                { "lastValue", new BsonDocument("$first", "$value") },
                { "lastTimestamp", new BsonDocument("$first", "$timestamp") },
                { "recentValues", new BsonDocument("$push", "$value") }
            }),
            new BsonDocument("$project", new BsonDocument
            {
                { "sensorId", "$_id" },
                { "sensorType", 1 },
                { "location", 1 },
                { "unit", 1 },
                { "lastValue", 1 },
                { "lastTimestamp", 1 },
                { "averageValue", new BsonDocument("$avg", 
                    new BsonDocument("$slice", new BsonArray { "$recentValues", 100 })) }
            })
        };

        var results = await _readings.Aggregate<BsonDocument>(pipeline).ToListAsync();
        
        return results.Select(doc => new DashboardSensor
        {
            SensorId = doc["sensorId"].AsString,
            SensorType = doc["sensorType"].AsString,
            Location = doc["location"].AsString,
            Unit = doc["unit"].AsString,
            LastValue = doc["lastValue"].AsDouble,
            LastTimestamp = doc["lastTimestamp"].ToUniversalTime(),
            AverageValue = doc["averageValue"].AsDouble
        }).ToList();
    }

    public async Task<List<string>> GetSensorTypesAsync()
    {
        return await _readings.Distinct<string>("sensorType", Builders<SensorReading>.Filter.Empty).ToListAsync();
    }

    public async Task<List<string>> GetSensorIdsAsync(string? sensorType = null)
    {
        var filter = string.IsNullOrEmpty(sensorType)
            ? Builders<SensorReading>.Filter.Empty
            : Builders<SensorReading>.Filter.Eq(r => r.SensorType, sensorType);
        
        return await _readings.Distinct<string>("sensorId", filter).ToListAsync();
    }
}
