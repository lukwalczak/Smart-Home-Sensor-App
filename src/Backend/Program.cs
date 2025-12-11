using Backend.Services;
using Backend.Hubs;
using Backend.Blockchain;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSignalR();

builder.Services.AddSingleton<MongoDbService>();

builder.Services.AddSingleton<BlockchainService>();

builder.Services.AddSingleton<MqttService>();
builder.Services.AddHostedService(provider => provider.GetRequiredService<MqttService>());

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
