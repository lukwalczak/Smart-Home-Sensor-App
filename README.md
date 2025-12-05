# 🏠 IoT Smart Home Monitoring

Demonstracyjna aplikacja do monitorowania urządzeń IoT w inteligentnym domu. Aplikacja odczytuje dane z 16 sensorów poprzez MQTT, zapisuje w MongoDB i prezentuje w formie tabelarycznej oraz wykresów.

## 📊 Typy Czujników

| Typ | Opis | Lokalizacje | Zakres | Jednostka |
|-----|------|-------------|--------|-----------|
| **TEMP** | Temperatura | Salon, Sypialnia, Kuchnia, Łazienka | 15-35 | °C |
| **HUMIDITY** | Wilgotność | Salon, Sypialnia, Kuchnia, Łazienka | 20-80 | % |
| **CO** | Tlenek węgla | Kuchnia, Garaż, Piwnica, Korytarz | 0-100 | ppm |
| **AIR_QUALITY** | PM2.5 | Salon, Sypialnia, Kuchnia, Zewnątrz | 0-500 | µg/m³ |

## 🛠️ Stack Technologiczny

- **Backend**: ASP.NET Core 8 + SignalR
- **Frontend**: Vue.js 3 + Vite + Chart.js
- **Database**: MongoDB 7
- **Message Broker**: Eclipse Mosquitto (MQTT)
- **Containerization**: Docker + docker-compose

## 🚀 Uruchomienie

### Wymagania
- Docker Desktop

### Quick Start

```bash
# Klonuj repozytorium
git clone https://github.com/your-username/SI.NET-Project.git
cd SI.NET-Project

# Uruchom wszystkie kontenery
docker-compose up -d --build

# Sprawdź status
docker-compose ps
```

### Dostęp do aplikacji

| Serwis | URL |
|--------|-----|
| Frontend | http://localhost:3000 |
| Backend API | http://localhost:5000/api |
| MongoDB | localhost:27017 |
| MQTT Broker | localhost:1883 |

## 📁 Struktura Projektu

```
SI.NET-Project/
├── src/
│   ├── Backend/           # ASP.NET Core 8 Web API
│   ├── Frontend/          # Vue.js 3 SPA
│   └── Simulator/         # .NET Console App (generator danych)
├── docker-compose.yml
├── LICENSE               # MIT X11
└── README.md
```

## 📡 API Endpoints

| Metoda | Endpoint | Opis |
|--------|----------|------|
| GET | `/api/sensors` | Lista odczytów z filtrami |
| GET | `/api/sensors/export/csv` | Eksport do CSV |
| GET | `/api/sensors/export/json` | Eksport do JSON |
| GET | `/api/sensors/dashboard` | Dane do dashboardu |

### Parametry filtrowania

- `dateFrom` - data początkowa (ISO 8601)
- `dateTo` - data końcowa (ISO 8601)
- `sensorType` - typ czujnika (TEMP, HUMIDITY, CO, AIR_QUALITY)
- `sensorId` - ID instancji czujnika
- `sortBy` - pole sortowania (timestamp, value)
- `sortOrder` - kierunek (asc, desc)
- `page` - numer strony
- `pageSize` - rozmiar strony

## 📜 Licencja

MIT X11 - szczegóły w pliku [LICENSE](LICENSE)
