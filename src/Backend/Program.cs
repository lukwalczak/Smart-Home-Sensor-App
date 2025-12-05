using Backend.Services;
using Backend.Hubs;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSignalR();

// MongoDB
builder.Services.AddSingleton<MongoDbService>();

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

app.UseCors("AllowFrontend");

app.MapControllers();
app.MapHub<SensorHub>("/sensorhub");

app.Run();
