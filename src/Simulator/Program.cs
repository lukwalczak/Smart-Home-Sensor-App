using System.Text;
using System.Text.Json;
using MQTTnet;
using MQTTnet.Client;

Console.WriteLine("🖥️ Data Center IoT Sensor Simulator");
Console.WriteLine("====================================");

var mqttHost = Environment.GetEnvironmentVariable("MQTT_HOST") ?? "localhost";
var mqttPort = int.Parse(Environment.GetEnvironmentVariable("MQTT_PORT") ?? "1883");

Console.WriteLine($"Connecting to MQTT broker at {mqttHost}:{mqttPort}...");

// Define 16 sensors (4 types x 4 instances)
var sensors = new List<SensorConfig>
{
    // Temperature sensors (Server rooms)
    new("TEMP", "TEMP-SERVER-ROOM-1", "Serwerownia 1", "°C", 18, 24, 0.5),
    new("TEMP", "TEMP-SERVER-ROOM-2", "Serwerownia 2", "°C", 18, 24, 0.4),
    new("TEMP", "TEMP-SERVER-ROOM-3", "Serwerownia 3", "°C", 19, 25, 0.6),
    new("TEMP", "TEMP-SERVER-ROOM-4", "Serwerownia 4", "°C", 18, 23, 0.5),

    // Humidity sensors (Cooling systems)
    new("HUMIDITY", "HUM-COOLING-1", "Chłodzenie 1", "%", 40, 60, 2.0),
    new("HUMIDITY", "HUM-COOLING-2", "Chłodzenie 2", "%", 40, 60, 1.8),
    new("HUMIDITY", "HUM-COOLING-3", "Chłodzenie 3", "%", 42, 62, 2.2),
    new("HUMIDITY", "HUM-COOLING-4", "Chłodzenie 4", "%", 40, 58, 1.9),

    // Carbon dioxide sensors (UPS rooms)
    new("CO2", "CO2-UPS-1", "UPS 1", "ppm", 400, 1000, 50.0),
    new("CO2", "CO2-UPS-2", "UPS 2", "ppm", 400, 1000, 45.0),
    new("CO2", "CO2-UPS-3", "UPS 3", "ppm", 450, 1100, 55.0),
    new("CO2", "CO2-UPS-4", "UPS 4", "ppm", 400, 950, 40.0),

    // Air quality (PM2.5) sensors (Air filters)
    new("AIR_QUALITY", "AQ-FILTER-1", "Filtr powietrza 1", "µg/m³", 5, 30, 3.0),
    new("AIR_QUALITY", "AQ-FILTER-2", "Filtr powietrza 2", "µg/m³", 5, 25, 2.5),
    new("AIR_QUALITY", "AQ-FILTER-3", "Filtr powietrza 3", "µg/m³", 8, 35, 3.5),
    new("AIR_QUALITY", "AQ-FILTER-4", "Filtr powietrza 4", "µg/m³", 5, 28, 2.8)
};

// Initialize current values
var currentValues = sensors.ToDictionary(s => s.SensorId, s => (s.MinValue + s.MaxValue) / 2);

var factory = new MqttFactory();
using var client = factory.CreateMqttClient();

var options = new MqttClientOptionsBuilder()
    .WithTcpServer(mqttHost, mqttPort)
    .WithClientId($"simulator-{Guid.NewGuid():N}")
    .Build();

// Connect with retry
var connected = false;
for (int i = 0; i < 30 && !connected; i++)
{
    try
    {
        await client.ConnectAsync(options);
        connected = true;
        Console.WriteLine("✅ Connected to MQTT broker!");
    }
    catch
    {
        Console.WriteLine($"⏳ Waiting for MQTT broker... ({i + 1}/30)");
        await Task.Delay(2000);
    }
}

if (!connected)
{
    Console.WriteLine("❌ Failed to connect to MQTT broker after 30 attempts");
    return;
}

Console.WriteLine($"📡 Starting simulation for {sensors.Count} sensors...\n");

var random = new Random();
var cts = new CancellationTokenSource();

Console.CancelKeyPress += (_, e) =>
{
    e.Cancel = true;
    cts.Cancel();
    Console.WriteLine("\n🛑 Stopping simulation...");
};

// Create tasks for each sensor with different intervals
var tasks = sensors.Select(sensor => Task.Run(async () =>
{
    // Random interval between 1-10 seconds per sensor
    var intervalMs = random.Next(1000, 10000);

    while (!cts.Token.IsCancellationRequested)
    {
        try
        {
            // Update value with random walk
            var currentValue = currentValues[sensor.SensorId];
            var change = (random.NextDouble() - 0.5) * 2 * sensor.Volatility;
            var newValue = Math.Max(sensor.MinValue, Math.Min(sensor.MaxValue, currentValue + change));
            currentValues[sensor.SensorId] = newValue;

            var message = new SensorMessage
            {
                SensorType = sensor.SensorType,
                SensorId = sensor.SensorId,
                Location = sensor.Location,
                Value = Math.Round(newValue, 2),
                Unit = sensor.Unit,
                Timestamp = DateTime.UtcNow
            };

            var payload = JsonSerializer.Serialize(message);
            var topic = $"sensors/{sensor.SensorType}/{sensor.SensorId}";

            await client.PublishAsync(new MqttApplicationMessageBuilder()
                .WithTopic(topic)
                .WithPayload(Encoding.UTF8.GetBytes(payload))
                .Build());

            Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] {sensor.SensorId}: {message.Value} {sensor.Unit}");

            await Task.Delay(intervalMs, cts.Token);
        }
        catch (OperationCanceledException)
        {
            break;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error for {sensor.SensorId}: {ex.Message}");
            await Task.Delay(5000, cts.Token);
        }
    }
}));

await Task.WhenAll(tasks);
await client.DisconnectAsync();
Console.WriteLine("👋 Simulator stopped.");

record SensorConfig(string SensorType, string SensorId, string Location, string Unit, double MinValue, double MaxValue, double Volatility);

class SensorMessage
{
    public string SensorType { get; set; } = string.Empty;
    public string SensorId { get; set; } = string.Empty;
    public string Location { get; set; } = string.Empty;
    public double Value { get; set; }
    public string Unit { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
}
