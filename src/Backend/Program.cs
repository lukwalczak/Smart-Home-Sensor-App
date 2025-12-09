using Backend.Services;
using Backend.Hubs;
using Backend.Blockchain;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSignalR();

// MongoDB
builder.Services.AddSingleton<MongoDbService>();

// Blockchain
builder.Services.AddSingleton<BlockchainService>();

// MQTT Background Service
builder.Services.AddSingleton<MqttService>();
builder.Services.AddHostedService(provider => provider.GetRequiredService<MqttService>());

// CORS for Vue.js frontend
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy.WithOrigins("http://localhost:3000", "http://frontend:3000", "http://localhost:5173")
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});

var app = builder.Build();

// Initialize blockchain service after a delay to allow contract deployment
var blockchainService = app.Services.GetRequiredService<BlockchainService>();
var logger = app.Services.GetRequiredService<ILogger<Program>>();

_ = Task.Run(async () =>
{
    try
    {
        logger.LogInformation("Waiting 10 seconds for contract deployment...");
        await Task.Delay(10000);
        logger.LogInformation("Initializing blockchain service...");
        await blockchainService.InitializeAsync();
        logger.LogInformation("Blockchain service initialized successfully");
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Failed to initialize blockchain service");
    }
});

app.UseCors("AllowFrontend");

app.MapControllers();
app.MapHub<SensorHub>("/sensorhub");

app.Run();
