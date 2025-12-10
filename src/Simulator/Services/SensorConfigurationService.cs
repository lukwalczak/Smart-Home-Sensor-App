using System.Text.Json;
using Simulator.Models;

namespace Simulator.Services;

public class SensorConfigurationService
{
    public List<SensorConfig> LoadSensorConfigurations(string? singleSensorId = null)
    {
        var configPath = Environment.GetEnvironmentVariable("SENSORS_CONFIG_PATH");
        
        var possiblePaths = new List<string>();
        if (!string.IsNullOrWhiteSpace(configPath))
        {
            possiblePaths.Add(configPath);
        }
        possiblePaths.Add("sensors-config.json");
        possiblePaths.Add(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "sensors-config.json"));
        possiblePaths.Add(Path.Combine(Directory.GetCurrentDirectory(), "sensors-config.json"));
        
        string? foundPath = null;
        foreach (var path in possiblePaths)
        {
            if (File.Exists(path))
            {
                foundPath = path;
                break;
            }
        }
        
        if (foundPath == null)
        {
            var currentDir = Directory.GetCurrentDirectory();
            var baseDir = AppDomain.CurrentDomain.BaseDirectory;
            var errorMsg = $"Sensor configuration file not found. Searched in:\n" +
                          $"  - {string.Join("\n  - ", possiblePaths)}\n" +
                          $"Current directory: {currentDir}\n" +
                          $"Base directory: {baseDir}\n" +
                          $"Files in current directory: {string.Join(", ", Directory.GetFiles(currentDir).Select(Path.GetFileName))}";
            throw new FileNotFoundException(errorMsg);
        }
        
        Console.WriteLine($"Loading sensor configuration from: {foundPath}");
        var sensors = new List<SensorConfig>();

        try
        {
            var jsonContent = File.ReadAllText(foundPath);
            var config = JsonSerializer.Deserialize<SensorConfigFile>(jsonContent, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            if (config?.Sensors == null || config.Sensors.Count == 0)
            {
                throw new InvalidOperationException("Sensor configuration file is empty or invalid.");
            }

            foreach (var sensorData in config.Sensors)
            {
                var envKey = sensorData.SensorId.Replace("-", "_");
                var enabled = GetEnvironmentVariable($"SENSOR_{envKey}_ENABLED", sensorData.Enabled ? "true" : "false") == "true";
                
                if (!enabled) continue;

                sensorData.MinValue = GetEnvironmentVariableAsDouble($"SENSOR_{envKey}_MIN_VALUE", sensorData.MinValue);
                sensorData.MaxValue = GetEnvironmentVariableAsDouble($"SENSOR_{envKey}_MAX_VALUE", sensorData.MaxValue);
                sensorData.Volatility = GetEnvironmentVariableAsDouble($"SENSOR_{envKey}_VOLATILITY", sensorData.Volatility);
                sensorData.IntervalMs = GetEnvironmentVariableAsInt($"SENSOR_{envKey}_INTERVAL_MS", sensorData.IntervalMs);
                sensorData.Enabled = enabled;

                sensors.Add(sensorData);
            }

            if (string.IsNullOrWhiteSpace(singleSensorId) && !string.IsNullOrWhiteSpace(config.SingleSensor))
            {
                singleSensorId = config.SingleSensor;
            }
        }
        catch (JsonException ex)
        {
            throw new InvalidOperationException($"Invalid JSON in sensor configuration file: {ex.Message}", ex);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Error loading sensor configuration: {ex.Message}", ex);
        }

        if (!string.IsNullOrWhiteSpace(singleSensorId))
        {
            sensors = sensors.Where(s => s.SensorId == singleSensorId).ToList();
            if (sensors.Count == 0)
            {
                throw new ArgumentException($"Sensor with ID '{singleSensorId}' not found in configuration.");
            }
        }

        return sensors.Where(s => s.Enabled).ToList();
    }


    private string GetEnvironmentVariable(string key, string defaultValue)
    {
        return Environment.GetEnvironmentVariable(key) ?? defaultValue;
    }

    private double GetEnvironmentVariableAsDouble(string key, double defaultValue)
    {
        var value = Environment.GetEnvironmentVariable(key);
        if (string.IsNullOrWhiteSpace(value) || !double.TryParse(value, out var result))
        {
            return defaultValue;
        }
        return result;
    }

    private int? GetEnvironmentVariableAsInt(string key, int? defaultValue)
    {
        var value = Environment.GetEnvironmentVariable(key);
        if (string.IsNullOrWhiteSpace(value) || !int.TryParse(value, out var result))
        {
            return defaultValue;
        }
        return result;
    }
}

internal class SensorConfigFile
{
    public List<SensorConfig>? Sensors { get; set; }
    public string? SingleSensor { get; set; }
}

