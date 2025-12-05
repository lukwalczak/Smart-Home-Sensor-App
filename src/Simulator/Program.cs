using System.Text;
using System.Text.Json;
using MQTTnet;
using MQTTnet.Client;

Console.WriteLine("🏠 Smart Home IoT Sensor Simulator");
Console.WriteLine("==================================");

var mqttHost = Environment.GetEnvironmentVariable("MQTT_HOST") ?? "localhost";
var mqttPort = int.Parse(Environment.GetEnvironmentVariable("MQTT_PORT") ?? "1883");

Console.WriteLine($"Connecting to MQTT broker at {mqttHost}:{mqttPort}...");

// Define 16 sensors (4 types x 4 instances)
var sensors = new List<SensorConfig>
{
    // Temperature sensors
    new("TEMP", "TEMP-SALON", "Salon", "°C", 18, 26, 0.5),
    new("TEMP", "TEMP-SYPIALNIA", "Sypialnia", "°C", 17, 24, 0.3),
    new("TEMP", "TEMP-KUCHNIA", "Kuchnia", "°C", 19, 28, 0.8),
    new("TEMP", "TEMP-LAZIENKA", "Łazienka", "°C", 20, 30, 0.6),
    
    // Humidity sensors
    new("HUMIDITY", "HUM-SALON", "Salon", "%", 35, 55, 2.0),
    new("HUMIDITY", "HUM-SYPIALNIA", "Sypialnia", "%", 40, 60, 1.5),
    new("HUMIDITY", "HUM-KUCHNIA", "Kuchnia", "%", 45, 75, 3.0),
    new("HUMIDITY", "HUM-LAZIENKA", "Łazienka", "%", 50, 85, 4.0),
    
    // Carbon monoxide sensors
    new("CO", "CO-KUCHNIA", "Kuchnia", "ppm", 0, 15, 1.0),
    new("CO", "CO-GARAZ", "Garaż", "ppm", 0, 25, 2.0),
    new("CO", "CO-PIWNICA", "Piwnica", "ppm", 0, 10, 0.5),
    new("CO", "CO-KORYTARZ", "Korytarz", "ppm", 0, 8, 0.3),
    
    // Air quality (PM2.5) sensors
    new("AIR_QUALITY", "AQ-SALON", "Salon", "µg/m³", 5, 35, 3.0),
    new("AIR_QUALITY", "AQ-SYPIALNIA", "Sypialnia", "µg/m³", 3, 25, 2.0),
    new("AIR_QUALITY", "AQ-KUCHNIA", "Kuchnia", "µg/m³", 10, 50, 5.0),
    new("AIR_QUALITY", "AQ-ZEWNATRZ", "Zewnątrz", "µg/m³", 15, 80, 8.0)
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
