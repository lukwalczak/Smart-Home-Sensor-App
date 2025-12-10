using System.Text;
using System.Text.Json;
using MQTTnet;
using MQTTnet.Client;
using Simulator.Models;

namespace Simulator.Services;

public class SensorSimulationService
{
    private readonly IMqttClient _mqttClient;
    private readonly Dictionary<string, double> _currentValues;
    private readonly Random _random;
    private readonly CancellationTokenSource _cancellationTokenSource;

    public SensorSimulationService(IMqttClient mqttClient)
    {
        _mqttClient = mqttClient;
        _currentValues = new Dictionary<string, double>();
        _random = new Random();
        _cancellationTokenSource = new CancellationTokenSource();
    }

    public void InitializeValues(List<SensorConfig> sensors)
    {
        foreach (var sensor in sensors)
        {
            _currentValues[sensor.SensorId] = (sensor.MinValue + sensor.MaxValue) / 2;
        }
    }

    public async Task StartSimulationAsync(List<SensorConfig> sensors, CancellationToken cancellationToken)
    {
        var tasks = sensors.Select(sensor => Task.Run(async () =>
        {
            var intervalMs = sensor.IntervalMs ?? _random.Next(1000, 10000);

            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    var currentValue = _currentValues[sensor.SensorId];
                    var change = (_random.NextDouble() - 0.5) * 2 * sensor.Volatility;
                    var newValue = Math.Max(sensor.MinValue, Math.Min(sensor.MaxValue, currentValue + change));
                    _currentValues[sensor.SensorId] = newValue;

                    var message = new SensorMessage
                    {
                        SensorType = sensor.SensorType,
                        SensorId = sensor.SensorId,
                        Location = sensor.Location,
                        Value = Math.Round(newValue, 2),
                        Unit = sensor.Unit,
                        Timestamp = DateTime.UtcNow
                    };

                    await PublishMessageAsync(sensor, message);

                    Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] {sensor.SensorId}: {message.Value} {sensor.Unit}");

                    await Task.Delay(intervalMs, cancellationToken);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error for {sensor.SensorId}: {ex.Message}");
                    await Task.Delay(5000, cancellationToken);
                }
            }
        }, cancellationToken));

        await Task.WhenAll(tasks);
    }

    private async Task PublishMessageAsync(SensorConfig sensor, SensorMessage message)
    {
        var payload = JsonSerializer.Serialize(message);
        var topic = $"sensors/{sensor.SensorType}/{sensor.SensorId}";

        await _mqttClient.PublishAsync(new MqttApplicationMessageBuilder()
            .WithTopic(topic)
            .WithPayload(Encoding.UTF8.GetBytes(payload))
            .Build());
    }
}

