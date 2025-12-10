<template>
  <div>
    <header class="page-header">
      <h1>🔗 Blockchain - Tokeny sensorów</h1>
      <p>System nagradzania tokenami ERC-20 za przesyłane komunikaty</p>
    </header>

    <!-- Contract Info -->
    <div v-if="contractInfo" class="contract-info">
      <h2>📜 Informacje o Smart Contract</h2>
      <div class="info-grid">
        <div class="info-card">
          <div class="info-label">Adres kontraktu</div>
          <div class="info-value mono">{{ contractInfo.contractAddress }}</div>
        </div>
        <div class="info-card">
          <div class="info-label">Administrator</div>
          <div class="info-value mono">{{ contractInfo.adminAddress }}</div>
        </div>
        <div class="info-card">
          <div class="info-label">Całkowita podaż</div>
          <div class="info-value">
            {{ formatNumber(contractInfo.totalSupply) }} SRT
          </div>
        </div>
        <div class="info-card">
          <div class="info-label">Nagroda za wiadomość</div>
          <div class="info-value">
            {{ formatNumber(contractInfo.rewardPerMessage) }} SRT
          </div>
        </div>
        <div class="info-card">
          <div class="info-label">Saldo administratora</div>
          <div class="info-value">
            {{ formatNumber(contractInfo.adminBalance) }} SRT
          </div>
        </div>
      </div>
    </div>

    <!-- Loading State -->
    <div v-if="loading" class="loading">
      <div class="spinner"></div>
      <p>Ładowanie danych z blockchainu...</p>
    </div>

    <!-- Empty State -->
    <div v-else-if="sensors.length === 0" class="empty-state">
      <p>⚠️ Blockchain nie jest jeszcze zainicjalizowany</p>
      <p>Upewnij się, że kontrakt został wdrożony na Anvil</p>
    </div>

    <!-- Sensors Table -->
    <div v-else class="blockchain-table-wrapper">
      <div class="table-header">
        <h2>💰 Portfele sensorów</h2>
        <button class="btn btn-primary" @click="fetchData">🔄 Odśwież</button>
      </div>

      <table>
        <thead>
          <tr>
            <th>Sensor ID</th>
            <th>Typ</th>
            <th>Lokalizacja</th>
            <th>Adres portfela</th>
            <th>Saldo (SRT)</th>
            <th>Łączne nagrody (SRT)</th>
            <th>Liczba wiadomości</th>
          </tr>
        </thead>
        <tbody>
          <tr v-for="sensor in sortedSensors" :key="sensor.sensorId">
            <td>
              <strong>{{ sensor.sensorId }}</strong>
            </td>
            <td>
              <span
                :class="['type-badge', getSensorTypeClass(sensor.sensorId)]"
              >
                {{ getIcon(sensor.sensorId) }}
                {{ getSensorType(sensor.sensorId) }}
              </span>
            </td>
            <td>{{ getLocation(sensor.sensorId) }}</td>
            <td class="mono">{{ formatAddress(sensor.walletAddress) }}</td>
            <td class="number">{{ formatNumber(sensor.balance) }}</td>
            <td class="number">{{ formatNumber(sensor.totalRewards) }}</td>
            <td class="number">{{ sensor.messageCount }}</td>
          </tr>
        </tbody>
      </table>

      <!-- Summary Stats -->
      <div class="summary-stats">
        <div class="stat-card">
          <div class="stat-label">Łączne nagrody wypłacone</div>
          <div class="stat-value">{{ formatNumber(totalRewards) }} SRT</div>
        </div>
        <div class="stat-card">
          <div class="stat-label">Łączna liczba wiadomości</div>
          <div class="stat-value">{{ totalMessages }}</div>
        </div>
        <div class="stat-card">
          <div class="stat-label">Średnia nagroda na sensor</div>
          <div class="stat-value">{{ formatNumber(averageReward) }} SRT</div>
        </div>
      </div>
    </div>
  </div>
</template>

<script>
import { ref, computed, onMounted } from "vue"
import axios from "axios"

export default {
  name: "Blockchain",
  setup() {
    const sensors = ref([])
    const contractInfo = ref(null)
    const loading = ref(true)

    const API_URL = import.meta.env.PROD ? "" : "http://localhost:5000"

    const sensorMetadata = {
      "TEMP-SERVER-ROOM-1": {
        type: "Temperatura",
        location: "Serwerownia 1",
        icon: "🌡️",
      },
      "TEMP-SERVER-ROOM-2": {
        type: "Temperatura",
        location: "Serwerownia 2",
        icon: "🌡️",
      },
      "TEMP-SERVER-ROOM-3": {
        type: "Temperatura",
        location: "Serwerownia 3",
        icon: "🌡️",
      },
      "TEMP-SERVER-ROOM-4": {
        type: "Temperatura",
        location: "Serwerownia 4",
        icon: "🌡️",
      },
      "HUM-COOLING-1": {
        type: "Wilgotność",
        location: "Chłodzenie 1",
        icon: "💧",
      },
      "HUM-COOLING-2": {
        type: "Wilgotność",
        location: "Chłodzenie 2",
        icon: "💧",
      },
      "HUM-COOLING-3": {
        type: "Wilgotność",
        location: "Chłodzenie 3",
        icon: "💧",
      },
      "HUM-COOLING-4": {
        type: "Wilgotność",
        location: "Chłodzenie 4",
        icon: "💧",
      },
      "CO2-UPS-1": { type: "CO₂", location: "UPS 1", icon: "☁️" },
      "CO2-UPS-2": { type: "CO₂", location: "UPS 2", icon: "☁️" },
      "CO2-UPS-3": { type: "CO₂", location: "UPS 3", icon: "☁️" },
      "CO2-UPS-4": { type: "CO₂", location: "UPS 4", icon: "☁️" },
      "AQ-FILTER-1": {
        type: "Jakość powietrza",
        location: "Filtr powietrza 1",
        icon: "🌿",
      },
      "AQ-FILTER-2": {
        type: "Jakość powietrza",
        location: "Filtr powietrza 2",
        icon: "🌿",
      },
      "AQ-FILTER-3": {
        type: "Jakość powietrza",
        location: "Filtr powietrza 3",
        icon: "🌿",
      },
      "AQ-FILTER-4": {
        type: "Jakość powietrza",
        location: "Filtr powietrza 4",
        icon: "🌿",
      },
    }

    const sortedSensors = computed(() => {
      return [...sensors.value].sort((a, b) => {
        return b.totalRewards - a.totalRewards
      })
    })

    const totalRewards = computed(() => {
      return sensors.value.reduce((sum, s) => sum + s.totalRewards, 0)
    })

    const totalMessages = computed(() => {
      return sensors.value.reduce((sum, s) => sum + s.messageCount, 0)
    })

    const averageReward = computed(() => {
      return sensors.value.length > 0
        ? totalRewards.value / sensors.value.length
        : 0
    })

    const fetchData = async () => {
      loading.value = true
      try {
        const [sensorsRes, contractRes] = await Promise.all([
          axios.get(`${API_URL}/api/blockchain/sensors`),
          axios.get(`${API_URL}/api/blockchain/contract`),
        ])

        sensors.value = sensorsRes.data
        contractInfo.value = contractRes.data
      } catch (error) {
        console.error("Error fetching blockchain data:", error)
      } finally {
        loading.value = false
      }
    }

    const getSensorType = (sensorId) => {
      return sensorMetadata[sensorId]?.type || "Unknown"
    }

    const getLocation = (sensorId) => {
      return sensorMetadata[sensorId]?.location || "Unknown"
    }

    const getIcon = (sensorId) => {
      return sensorMetadata[sensorId]?.icon || "📡"
    }

    const getSensorTypeClass = (sensorId) => {
      if (sensorId.startsWith("TEMP")) return "temp"
      if (sensorId.startsWith("HUM")) return "humidity"
      if (sensorId.startsWith("CO")) return "co"
      if (sensorId.startsWith("AQ")) return "air-quality"
      return ""
    }

    const formatAddress = (address) => {
      return `${address.substring(0, 6)}...${address.substring(
        address.length - 4
      )}`
    }

    const formatNumber = (num) => {
      return Number(num).toLocaleString("pl-PL", {
        minimumFractionDigits: 2,
        maximumFractionDigits: 2,
      })
    }

    onMounted(() => {
      fetchData()
      // Auto refresh every 30 seconds
      setInterval(fetchData, 30000)
    })

    return {
      sensors,
      contractInfo,
      loading,
      sortedSensors,
      totalRewards,
      totalMessages,
      averageReward,
      fetchData,
      getSensorType,
      getLocation,
      getIcon,
      getSensorTypeClass,
      formatAddress,
      formatNumber,
    }
  },
}
</script>

<style scoped>
.contract-info {
  background: white;
  border-radius: 10px;
  padding: 20px;
  margin-bottom: 30px;
  box-shadow: 0 2px 8px rgba(0, 0, 0, 0.1);
}

.contract-info h2 {
  margin-top: 0;
  margin-bottom: 20px;
  color: #2c3e50;
  font-size: 1.3em;
}

.info-grid {
  display: grid;
  grid-template-columns: repeat(auto-fit, minmax(250px, 1fr));
  gap: 15px;
}

.info-card {
  background: #f8f9fa;
  padding: 15px;
  border-radius: 8px;
  border-left: 4px solid #3498db;
}

.info-label {
  font-size: 0.85em;
  color: #7f8c8d;
  margin-bottom: 5px;
  text-transform: uppercase;
  letter-spacing: 0.5px;
}

.info-value {
  font-size: 1.1em;
  color: #2c3e50;
  font-weight: 600;
  word-break: break-all;
}

.mono {
  font-family: "Courier New", monospace;
  font-size: 0.9em;
}

.blockchain-table-wrapper {
  background: white;
  border-radius: 10px;
  padding: 20px;
  box-shadow: 0 2px 8px rgba(0, 0, 0, 0.1);
}

.table-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
  margin-bottom: 20px;
}

.table-header h2 {
  margin: 0;
  color: #2c3e50;
  font-size: 1.3em;
}

table {
  width: 100%;
  border-collapse: collapse;
  margin-bottom: 20px;
}

th,
td {
  padding: 12px;
  text-align: left;
  border-bottom: 1px solid #ecf0f1;
  color: #2c3e50;
}

th {
  background: #f8f9fa;
  font-weight: 600;
  color: #2c3e50;
  font-size: 0.9em;
  text-transform: uppercase;
  letter-spacing: 0.5px;
  cursor: default;
}

th:hover {
  color: #2c3e50;
}

td {
  color: #2c3e50;
}

tr:hover {
  background: #f8f9fa;
}

.type-badge {
  display: inline-block;
  padding: 4px 10px;
  border-radius: 12px;
  font-size: 0.85em;
  font-weight: 600;
}

.type-badge.temp {
  background: #fee;
  color: #c33;
}

.type-badge.humidity {
  background: #e3f2fd;
  color: #1976d2;
}

.type-badge.co2 {
  background: #fff3e0;
  color: #e65100;
}

.type-badge.air-quality {
  background: #e8f5e9;
  color: #2e7d32;
}

.number {
  text-align: right;
  font-variant-numeric: tabular-nums;
}

.summary-stats {
  display: grid;
  grid-template-columns: repeat(auto-fit, minmax(200px, 1fr));
  gap: 15px;
  margin-top: 30px;
  padding-top: 20px;
  border-top: 2px solid #ecf0f1;
}

.stat-card {
  text-align: center;
  padding: 20px;
  background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
  color: white;
  border-radius: 10px;
}

.stat-label {
  font-size: 0.9em;
  opacity: 0.9;
  margin-bottom: 10px;
}

.stat-value {
  font-size: 1.8em;
  font-weight: 700;
}

.btn {
  padding: 10px 20px;
  border: none;
  border-radius: 6px;
  cursor: pointer;
  font-weight: 600;
  transition: all 0.3s;
}

.btn-primary {
  background: #3498db;
  color: white;
}

.btn-primary:hover {
  background: #2980b9;
  transform: translateY(-2px);
  box-shadow: 0 4px 12px rgba(52, 152, 219, 0.4);
}

.loading {
  text-align: center;
  padding: 60px 20px;
}

.spinner {
  width: 50px;
  height: 50px;
  border: 4px solid #f3f3f3;
  border-top: 4px solid #3498db;
  border-radius: 50%;
  animation: spin 1s linear infinite;
  margin: 0 auto 20px;
}

@keyframes spin {
  to {
    transform: rotate(360deg);
  }
}

.empty-state {
  text-align: center;
  padding: 60px 20px;
  color: #7f8c8d;
}

.empty-state p {
  font-size: 1.1em;
  margin: 10px 0;
}
</style>
