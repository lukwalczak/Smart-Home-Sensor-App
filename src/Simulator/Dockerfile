FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY Simulator.csproj .
RUN dotnet restore

COPY . .
RUN dotnet publish -c Release -o /app

FROM mcr.microsoft.com/dotnet/runtime:8.0
WORKDIR /app
COPY --from=build /app .

ENV MQTT_HOST=mosquitto
ENV MQTT_PORT=1883

ENTRYPOINT ["dotnet", "Simulator.dll"]
