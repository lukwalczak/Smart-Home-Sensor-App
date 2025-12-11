# send-sensor-value.ps1
param(
    [Parameter(Mandatory=$true)]
    [string]$SensorId,

    [Parameter(Mandatory=$true)]
    [double]$Value
)

# Mapowanie sensorów (z sensors-config.json)
$sensorMap = @{
    "TEMP-SERVER-ROOM-1" = @{Type="TEMP"; Location="Serwerownia 1"; Unit="°C"}
    "TEMP-SERVER-ROOM-2" = @{Type="TEMP"; Location="Serwerownia 2"; Unit="°C"}
    "TEMP-SERVER-ROOM-3" = @{Type="TEMP"; Location="Serwerownia 3"; Unit="°C"}
    "TEMP-SERVER-ROOM-4" = @{Type="TEMP"; Location="Serwerownia 4"; Unit="°C"}
    "HUM-COOLING-1" = @{Type="HUMIDITY"; Location="Chłodzenie 1"; Unit="%"}
    "HUM-COOLING-2" = @{Type="HUMIDITY"; Location="Chłodzenie 2"; Unit="%"}
    "HUM-COOLING-3" = @{Type="HUMIDITY"; Location="Chłodzenie 3"; Unit="%"}
    "HUM-COOLING-4" = @{Type="HUMIDITY"; Location="Chłodzenie 4"; Unit="%"}
    "CO2-UPS-1" = @{Type="CO2"; Location="UPS 1"; Unit="ppm"}
    "CO2-UPS-2" = @{Type="CO2"; Location="UPS 2"; Unit="ppm"}
    "CO2-UPS-3" = @{Type="CO2"; Location="UPS 3"; Unit="ppm"}
    "CO2-UPS-4" = @{Type="CO2"; Location="UPS 4"; Unit="ppm"}
    "AQ-FILTER-1" = @{Type="AIR_QUALITY"; Location="Filtr powietrza 1"; Unit="µg/m³"}
    "AQ-FILTER-2" = @{Type="AIR_QUALITY"; Location="Filtr powietrza 2"; Unit="µg/m³"}
    "AQ-FILTER-3" = @{Type="AIR_QUALITY"; Location="Filtr powietrza 3"; Unit="µg/m³"}
    "AQ-FILTER-4" = @{Type="AIR_QUALITY"; Location="Filtr powietrza 4"; Unit="µg/m³"}
}

if (-not $sensorMap.ContainsKey($SensorId)) {
    Write-Host "Error: Unknown sensor ID: $SensorId" -ForegroundColor Red
    exit 1
}

$sensor = $sensorMap[$SensorId]
$timestamp = (Get-Date).ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ssZ")

$json = @{
    SensorType = $sensor.Type
    SensorId = $SensorId
    Location = $sensor.Location
    Value = $Value
    Unit = $sensor.Unit
    Timestamp = $timestamp
} | ConvertTo-Json -Compress

$topic = "sensors/$($sensor.Type)/$SensorId"

docker exec mosquitto mosquitto_pub -h localhost -t $topic -m $json

Write-Host "Sent value $Value to sensor $SensorId" -ForegroundColor Green