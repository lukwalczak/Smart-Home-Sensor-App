# 🖥️ IoT Data Center Monitoring with Blockchain Rewards

A demonstration application for monitoring IoT devices in a data center with **blockchain-based token rewards**. The application reads data from 16 sensors via MQTT, stores it in MongoDB, rewards sensors with ERC-20 tokens, and presents data in tabular and chart formats with real-time updates.

## 🆕 Blockchain Features

Each sensor is rewarded with **SensorRewardToken (SRT)** ERC-20 tokens for every message sent:

- **Smart Contract**: ERC-20 token on local Ethereum blockchain (Anvil)
- **Automatic Rewards**: Sensors receive 10 SRT per message
- **Wallet Management**: Each of 16 sensors has its own wallet
- **Admin Dashboard**: View token balances and transaction history

## 📊 Sensor Types

| Type            | Description    | Locations          | Range    | Unit  |
| --------------- | -------------- | ------------------ | -------- | ----- |
| **TEMP**        | Temperature    | Server Room 1-4    | 18-24    | °C    |
| **HUMIDITY**    | Humidity       | Cooling System 1-4 | 40-60    | %     |
| **CO2**         | Carbon Dioxide | UPS Room 1-4       | 400-1000 | ppm   |
| **AIR_QUALITY** | PM2.5          | Air Filter 1-4     | 5-50     | µg/m³ |

## 🛠️ Technology Stack

### Core Technologies

- **Backend**: ASP.NET Core 8 + SignalR
- **Frontend**: Vue.js 3 + Vite + Chart.js
- **Database**: MongoDB 7
- **Message Broker**: Eclipse Mosquitto (MQTT)

### Blockchain Stack

- **Smart Contract**: Solidity 0.8.20 with OpenZeppelin
- **Local Blockchain**: Foundry Anvil (Ethereum-compatible)
- **Contract Framework**: Foundry (Forge for compilation)
- **Backend Integration**: Nethereum 4.19.0
- **Token Standard**: ERC-20

### Infrastructure

- **Containerization**: Docker + docker-compose
- **Development Tools**: Node.js, npm

## 🚀 Getting Started

### Requirements

- Docker Desktop

### Quick Start

```bash
# Clone the repository
git clone https://github.com/lukwalczak/Data-Center-Sensor-App.git
cd Data-Center-Sensor-App

# Start all containers (including blockchain)
docker-compose up -d --build

# Wait ~30 seconds for all services to initialize

# Deploy smart contract to Anvil
docker exec blockchain sh /blockchain/scripts/setup.sh
### Application Access

| Service          | URL                           | Description                    |
| ---------------- | ----------------------------- | ------------------------------ |
| Frontend         | http://localhost:3000         | Main web interface             |
| Backend API      | http://localhost:5000/api     | REST API endpoints             |
| MongoDB          | localhost:27017               | Database                       |
| MQTT Broker      | localhost:1883                | Message broker                 |
| Anvil RPC        | http://localhost:8545         | Ethereum JSON-RPC              |
## 📁 Project Structure

```

Data-Center-Sensor-App/
├── blockchain/ # Blockchain smart contracts
│ ├── contracts/
│ │ └── SensorRewardToken.sol # ERC-20 token contract
│ ├── scripts/
│ │ ├── deploy.js # Deployment script
│ │ └── setup.sh # Container setup script
│ ├── foundry.toml # Forge configuration
│ ├── Dockerfile
│ └── package.json
├── src/
│ ├── Backend/ # ASP.NET Core 8 Web API
│ │ ├── Blockchain/ # Nethereum integration
│ │ ├── Controllers/
│ │ │ ├── BlockchainController.cs # NEW
│ │ │ └── SensorsController.cs
│ │ └── Services/
│ ├── Frontend/ # Vue.js 3 SPA
│ │ └── src/views/
│ │ ├── Blockchain.vue # NEW: Token dashboard
│ │ └── ...
│ └── Simulator/ # .NET Console App
├── docker-compose.yml
├── LICENSE # MIT X11
└── README.md

```├── Backend/           # ASP.NET Core 8 Web API
│   ├── Frontend/          # Vue.js 3 SPA
│   └── Simulator/         # .NET Console App (data generator)
├── docker-compose.yml
├── LICENSE               # MIT X11
└── README.md
```

## 🔗 Blockchain Architecture

### Smart Contract (SensorRewardToken.sol)

The ERC-20 smart contract manages token distribution:

- **rewardSensor()**: Automatically rewards sensors for messages
- **getSensorStats()**: Returns balance, total rewards, and message count
- **Token Symbol**: SRT (SensorRewardToken)
- **Reward Amount**: 10 SRT per message

### Data Flow with Blockchain

```
Sensor → MQTT → Backend → MongoDB
                   ↓
              Blockchain Service
                   ↓
         Smart Contract (rewardSensor)
                   ↓
     Transfer 10 SRT to sensor wallet
```

## 📡 API Endpoints

### Sensor Data Endpoints

| Method | Endpoint                   | Description                |
| ------ | -------------------------- | -------------------------- |
| GET    | `/api/sensors`             | List readings with filters |
| GET    | `/api/sensors/export/csv`  | Export to CSV              |
| GET    | `/api/sensors/export/json` | Export to JSON             |
| GET    | `/api/sensors/dashboard`   | Dashboard data             |

### 🆕 Blockchain Endpoints

| Method | Endpoint                       | Description                   |
| ------ | ------------------------------ | ----------------------------- |
| GET    | `/api/blockchain/sensors`      | All sensor token balances     |
| GET    | `/api/blockchain/sensors/{id}` | Single sensor token info      |
| GET    | `/api/blockchain/contract`     | Smart contract information    |
| POST   | `/api/blockchain/reward/{id}`  | Manually reward sensor (test) |

### Filter Parameters

- `dateFrom` - start date (ISO 8601)
- `dateTo` - end date (ISO 8601)
- `sensorType` - sensor type (TEMP, HUMIDITY, CO2, AIR_QUALITY)
- `sensorId` - sensor instance ID
- `sortBy` - sort field (timestamp, value)
- `sortOrder` - sort direction (asc, desc)
- `page` - page number
- `pageSize` - page size

## 🧪 Testing Blockchain Integration

### Check Contract Deployment

```bash
docker exec blockchain cat /blockchain/deployment.json
```

### Monitor Token Rewards

```bash
docker logs -f backend | grep -i reward
```

### Query Sensor Balances

```bash
curl http://localhost:5000/api/blockchain/sensors
```

### Manual Reward (Testing)

```bash
curl -X POST http://localhost:5000/api/blockchain/reward/TEMP-SERVER-ROOM-1
```

## 🔒 Security Notes

⚠️ **This is a demonstration project using a local blockchain**

- Uses default Anvil test accounts with public private keys
- **NOT FOR PRODUCTION USE**
- For production: Use proper key management (Azure Key Vault, AWS KMS)
- For production: Use testnets (Sepolia, Goerli) or mainnet

## 📜 License

MIT X11 - see [LICENSE](LICENSE) file for details

## 🎓 Academic Project Requirements

### ✅ Core Requirements (Grade 3.0)

- [x] ASP.NET Core 8+ backend
- [x] Public GitHub repository
- [x] MIT X11 license
- [x] All components containerized
- [x] Complete docker-compose.yml
- [x] MQTT message broker
- [x] MongoDB NoSQL database
- [x] Data filtering and sorting
- [x] CSV/JSON export
- [x] Frontend with charts
- [x] 16 sensors, 4 types

### ✅ Real-time Dashboard (+0.5)

- [x] SignalR WebSocket integration
- [x] Auto-refresh without page reload
- [x] Latest values and averages

### ✅ Blockchain Extension

- [x] **ERC-20 Smart Contract** in Solidity
- [x] **Token rewards** for each sensor message
- [x] **Anvil** local Ethereum blockchain
- [x] **Nethereum** integration in .NET
- [x] **Admin interface** for token balances
- [x] **Automatic integration** with MQTT flow

---

**Built with ❤️ for IoT and Blockchain**
