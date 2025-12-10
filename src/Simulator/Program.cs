using MQTTnet;
using MQTTnet.Client;
using Simulator.Models;
using Simulator.Services;

Console.WriteLine("Data Center IoT Sensor Simulator");
Console.WriteLine("====================================");

var mqttHost = Environment.GetEnvironmentVariable("MQTT_HOST") ?? "localhost";
var mqttPort = int.Parse(Environment.GetEnvironmentVariable("MQTT_PORT") ?? "1883");
var singleSensorId = Environment.GetEnvironmentVariable("SIMULATOR_SINGLE_SENSOR");

Console.WriteLine($"Connecting to MQTT broker at {mqttHost}:{mqttPort}...");

var configService = new SensorConfigurationService();
List<SensorConfig> sensors;

try
{
    sensors = configService.LoadSensorConfigurations(singleSensorId);
    
    if (!string.IsNullOrWhiteSpace(singleSensorId))
    {
        Console.WriteLine($"Running in single-sensor mode: {singleSensorId}");
    }
    
    Console.WriteLine($"Loaded {sensors.Count} sensor(s) configuration(s)");
}
catch (Exception ex)
{
    Console.WriteLine($"Error loading sensor configurations: {ex.Message}");
    return;
}

if (sensors.Count == 0)
{
    Console.WriteLine("No enabled sensors found. Check your configuration.");
    return;
}

var factory = new MqttFactory();
using var client = factory.CreateMqttClient();

var options = new MqttClientOptionsBuilder()
    .WithTcpServer(mqttHost, mqttPort)
    .WithClientId($"simulator-{Guid.NewGuid():N}")
    .Build();

var connected = false;
for (int i = 0; i < 30 && !connected; i++)
{
    try
    {
        await client.ConnectAsync(options);
        connected = true;
        Console.WriteLine("Connected to MQTT broker!");
    }
    catch
    {
        Console.WriteLine($"Waiting for MQTT broker... ({i + 1}/30)");
        await Task.Delay(2000);
    }
}

if (!connected)
{
    Console.WriteLine("Failed to connect to MQTT broker after 30 attempts");
    return;
}

Console.WriteLine($"Starting simulation for {sensors.Count} sensor(s)...\n");

var cts = new CancellationTokenSource();

Console.CancelKeyPress += (_, e) =>
{
    e.Cancel = true;
    cts.Cancel();
    Console.WriteLine("\nStopping simulation...");
};

var simulationService = new SensorSimulationService(client);
simulationService.InitializeValues(sensors);

try
{
    await simulationService.StartSimulationAsync(sensors, cts.Token);
}
catch (OperationCanceledException)
{
}
finally
{
    await client.DisconnectAsync();
    Console.WriteLine("Simulator stopped.");
}
