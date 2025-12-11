using System.Text;
using System.Text.Json;
using MQTTnet;
using MQTTnet.Client;
using MQTTnet.Extensions.ManagedClient;
using Backend.Models;
using Backend.Hubs;
using Backend.Blockchain;
using Microsoft.AspNetCore.SignalR;

namespace Backend.Services;

public class MqttService : BackgroundService
{
    private readonly ILogger<MqttService> _logger;
    private readonly MongoDbService _mongoDb;
    private readonly IHubContext<SensorHub> _hubContext;
    private readonly BlockchainService _blockchainService;
    private readonly string _mqttHost;
    private readonly int _mqttPort;
    private readonly string _topic;
    private IManagedMqttClient? _mqttClient;

    public MqttService(
        IConfiguration config,
        ILogger<MqttService> logger,
        MongoDbService mongoDb,
        IHubContext<SensorHub> hubContext,
        BlockchainService blockchainService)
    {
        _logger = logger;
        _mongoDb = mongoDb;
        _hubContext = hubContext;
        _blockchainService = blockchainService;
        _mqttHost = config["Mqtt:Host"] ?? "localhost";
        _mqttPort = int.Parse(config["Mqtt:Port"] ?? "1883");
        _topic = config["Mqtt:Topic"] ?? "sensors/#";
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("MQTT Service starting, connecting to {Host}:{Port}", _mqttHost, _mqttPort);

        var factory = new MqttFactory();
        _mqttClient = factory.CreateManagedMqttClient();

        var options = new ManagedMqttClientOptionsBuilder()
            .WithAutoReconnectDelay(TimeSpan.FromSeconds(5))
            .WithClientOptions(new MqttClientOptionsBuilder()
                .WithTcpServer(_mqttHost, _mqttPort)
                .WithClientId($"backend-{Guid.NewGuid():N}")
                .Build())
            .Build();

        _mqttClient.ApplicationMessageReceivedAsync += HandleMessageAsync;

        await _mqttClient.StartAsync(options);
        await _mqttClient.SubscribeAsync(_topic);

        _logger.LogInformation("MQTT Client subscribed to topic: {Topic}", _topic);

        while (!stoppingToken.IsCancellationRequested)
        {
            await Task.Delay(1000, stoppingToken);
        }

        await _mqttClient.StopAsync();
    }

    private async Task HandleMessageAsync(MqttApplicationMessageReceivedEventArgs e)
    {
        try
        {
            var payload = Encoding.UTF8.GetString(e.ApplicationMessage.PayloadSegment);
            var topic = e.ApplicationMessage.Topic;

            _logger.LogDebug("Received message on {Topic}: {Payload}", topic, payload);

            var message = JsonSerializer.Deserialize<SensorMessage>(payload, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            if (message == null)
            {
                _logger.LogWarning("Failed to deserialize message: {Payload}", payload);
                return;
            }

            var reading = new SensorReading
            {
                SensorType = message.SensorType,
                SensorId = message.SensorId,
                Location = message.Location,
                Value = message.Value,
                Unit = message.Unit,
                Timestamp = message.Timestamp
            };

            await _mongoDb.InsertReadingAsync(reading);

            _ = Task.Run(async () =>
            {
                try
                {
                    await _blockchainService.RewardSensorAsync(reading.SensorId);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to reward sensor {SensorId} on blockchain", reading.SensorId);
                }
            });

            await _hubContext.Clients.All.SendAsync("NewReading", new
            {
                reading.SensorType,
                reading.SensorId,
                reading.Location,
                reading.Value,
                reading.Unit,
                reading.Timestamp
            });

            _logger.LogDebug("Processed and saved reading from {SensorId}", reading.SensorId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing MQTT message");
        }
    }
}

public class SensorMessage
{
    public string SensorType { get; set; } = string.Empty;
    public string SensorId { get; set; } = string.Empty;
    public string Location { get; set; } = string.Empty;
    public double Value { get; set; }
    public string Unit { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
}
