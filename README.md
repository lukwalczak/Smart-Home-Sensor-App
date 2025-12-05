# 🏠 IoT Smart Home Monitoring

A demonstration application for monitoring IoT devices in a smart home. The application reads data from 16 sensors via MQTT, stores it in MongoDB, and presents it in tabular and chart formats.

## 📊 Sensor Types

| Type            | Description     | Locations                               | Range | Unit  |
| --------------- | --------------- | --------------------------------------- | ----- | ----- |
| **TEMP**        | Temperature     | Living Room, Bedroom, Kitchen, Bathroom | 15-35 | °C    |
| **HUMIDITY**    | Humidity        | Living Room, Bedroom, Kitchen, Bathroom | 20-80 | %     |
| **CO**          | Carbon Monoxide | Kitchen, Garage, Basement, Hallway      | 0-100 | ppm   |
| **AIR_QUALITY** | PM2.5           | Living Room, Bedroom, Kitchen, Outdoor  | 0-500 | µg/m³ |

## 🛠️ Technology Stack

- **Backend**: ASP.NET Core 8 + SignalR
- **Frontend**: Vue.js 3 + Vite + Chart.js
- **Database**: MongoDB 7
- **Message Broker**: Eclipse Mosquitto (MQTT)
- **Containerization**: Docker + docker-compose

## 🚀 Getting Started

### Requirements

- Docker Desktop

### Quick Start

```bash
# Clone the repository
git clone https://github.com/your-username/SI.NET-Project.git
cd SI.NET-Project

# Start all containers
docker-compose up -d --build

# Check status
docker-compose ps
```

### Application Access

| Service     | URL                       |
| ----------- | ------------------------- |
| Frontend    | http://localhost:3000     |
| Backend API | http://localhost:5000/api |
| MongoDB     | localhost:27017           |
| MQTT Broker | localhost:1883            |

## 📁 Project Structure

```
SI.NET-Project/
├── src/
│   ├── Backend/           # ASP.NET Core 8 Web API
│   ├── Frontend/          # Vue.js 3 SPA
│   └── Simulator/         # .NET Console App (data generator)
├── docker-compose.yml
├── LICENSE               # MIT X11
└── README.md
```

## 📡 API Endpoints

| Method | Endpoint                   | Description                |
| ------ | -------------------------- | -------------------------- |
| GET    | `/api/sensors`             | List readings with filters |
| GET    | `/api/sensors/export/csv`  | Export to CSV              |
| GET    | `/api/sensors/export/json` | Export to JSON             |
| GET    | `/api/sensors/dashboard`   | Dashboard data             |

### Filter Parameters

- `dateFrom` - start date (ISO 8601)
- `dateTo` - end date (ISO 8601)
- `sensorType` - sensor type (TEMP, HUMIDITY, CO, AIR_QUALITY)
- `sensorId` - sensor instance ID
- `sortBy` - sort field (timestamp, value)
- `sortOrder` - sort direction (asc, desc)
- `page` - page number
- `pageSize` - page size

## 📜 License

MIT X11 - see [LICENSE](LICENSE) file for details
