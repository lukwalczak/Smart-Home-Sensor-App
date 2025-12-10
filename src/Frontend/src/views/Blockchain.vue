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
        <button class="btn btn-primary" @click="fetchData">
          <span class="btn-icon">🔄</span>
          <span>Odśwież</span>
        </button>
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
  background: var(--card);
  border-radius: 1rem;
  padding: 1.5rem;
  margin-bottom: 2rem;
  border: 1px solid var(--border);
  box-shadow: var(--shadow);
}

.contract-info h2 {
  margin-top: 0;
  margin-bottom: 1.25rem;
  color: var(--text);
  font-size: 1.3em;
}

.info-grid {
  display: grid;
  grid-template-columns: repeat(auto-fit, minmax(250px, 1fr));
  gap: 1rem;
}

.info-card {
  padding: 1.5rem;
  border-radius: 1rem;
  border: 2px solid;
  border-color: rgba(91, 79, 200, 0.5);
  position: relative;
  overflow: hidden;
  background: linear-gradient(135deg, #5b4fc8 0%, #6b4ba0 50%, #9d5dbd 100%);
  box-shadow: 0 10px 25px rgba(91, 79, 200, 0.4), 0 4px 10px rgba(107, 75, 160, 0.3);
  transition: all 0.3s ease;
}

.info-card:hover {
  transform: translateY(-4px);
  box-shadow: 0 15px 35px rgba(91, 79, 200, 0.5), 0 8px 15px rgba(107, 75, 160, 0.4);
  border-color: rgba(91, 79, 200, 0.8);
}

.info-card::before {
  content: "";
  position: absolute;
  top: 0;
  left: 0;
  right: 0;
  bottom: 0;
  background: linear-gradient(
    135deg,
    rgba(255, 255, 255, 0.15) 0%,
    rgba(255, 255, 255, 0.05) 50%,
    rgba(255, 255, 255, 0) 100%
  );
  border-radius: 1rem;
  z-index: 0;
  pointer-events: none;
  opacity: 0.6;
}

.info-card::after {
  content: "";
  position: absolute;
  top: -50%;
  left: -50%;
  width: 200%;
  height: 200%;
  background: linear-gradient(
    45deg,
    transparent 30%,
    rgba(255, 255, 255, 0.1) 50%,
    transparent 70%
  );
  transform: rotate(45deg);
  z-index: 1;
  pointer-events: none;
}

.info-card > * {
  position: relative;
  z-index: 2;
}

.info-card:nth-child(2) {
  background: linear-gradient(135deg, #d84d8f 0%, #dc3d5a 50%, #e66d5d 100%);
  border-color: rgba(216, 77, 143, 0.5);
  box-shadow: 0 10px 25px rgba(216, 77, 143, 0.4), 0 4px 10px rgba(220, 61, 90, 0.3);
}

.info-card:nth-child(2):hover {
  box-shadow: 0 15px 35px rgba(216, 77, 143, 0.5), 0 8px 15px rgba(220, 61, 90, 0.4);
  border-color: rgba(216, 77, 143, 0.8);
}

.info-card:nth-child(3) {
  background: linear-gradient(135deg, #3d8dd4 0%, #00afc9 50%, #3d8dd4 100%);
  border-color: rgba(61, 141, 212, 0.5);
  box-shadow: 0 10px 25px rgba(61, 141, 212, 0.4), 0 4px 10px rgba(0, 175, 201, 0.3);
}

.info-card:nth-child(3):hover {
  box-shadow: 0 15px 35px rgba(61, 141, 212, 0.5), 0 8px 15px rgba(0, 175, 201, 0.4);
  border-color: rgba(61, 141, 212, 0.8);
}

.info-card:nth-child(4) {
  background: linear-gradient(135deg, #32b366 0%, #2bc5ab 50%, #4ab67a 100%);
  border-color: rgba(50, 179, 102, 0.5);
  box-shadow: 0 10px 25px rgba(50, 179, 102, 0.4), 0 4px 10px rgba(43, 197, 171, 0.3);
}

.info-card:nth-child(4):hover {
  box-shadow: 0 15px 35px rgba(50, 179, 102, 0.5), 0 8px 15px rgba(43, 197, 171, 0.4);
  border-color: rgba(50, 179, 102, 0.8);
}

.info-card:nth-child(5) {
  background: linear-gradient(135deg, #5b4fc8 0%, #6b4ba0 50%, #9d5dbd 100%);
  border-color: rgba(91, 79, 200, 0.5);
  box-shadow: 0 10px 25px rgba(91, 79, 200, 0.4), 0 4px 10px rgba(107, 75, 160, 0.3);
}

.info-card:nth-child(5):hover {
  box-shadow: 0 15px 35px rgba(91, 79, 200, 0.5), 0 8px 15px rgba(107, 75, 160, 0.4);
  border-color: rgba(91, 79, 200, 0.8);
}

.info-label {
  font-size: 0.85em;
  color: rgba(255, 255, 255, 0.9);
  margin-bottom: 0.5rem;
  text-transform: uppercase;
  letter-spacing: 0.5px;
  font-weight: 500;
}

.info-value {
  font-size: 1.1em;
  color: white;
  font-weight: 700;
  word-break: break-all;
  text-shadow: 0 1px 2px rgba(0, 0, 0, 0.2);
}

.mono {
  font-family: "Courier New", monospace;
  font-size: 0.9em;
}

.blockchain-table-wrapper {
  background: var(--card);
  border-radius: 1rem;
  padding: 1.5rem;
  border: 1px solid var(--border);
  box-shadow: var(--shadow);
}

.table-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
  margin-bottom: 1.25rem;
}

.table-header h2 {
  margin: 0;
  color: var(--text);
  font-size: 1.3em;
}

table {
  width: 100%;
  border-collapse: collapse;
  margin-bottom: 1.25rem;
}

th,
td {
  padding: 1rem 1.5rem;
  text-align: left;
  border-bottom: 1px solid var(--border);
}

th {
  background: var(--darker);
  font-weight: 600;
  color: var(--text-muted);
  font-size: 0.9em;
  text-transform: uppercase;
  letter-spacing: 0.5px;
  cursor: default;
}

th:hover {
  color: var(--text);
}

td {
  color: var(--text);
}

tr:hover td {
  background: rgba(99, 102, 241, 0.1);
}

.type-badge {
  display: inline-block;
  padding: 0.25rem 0.75rem;
  border-radius: 0.75rem;
  font-size: 0.85em;
  font-weight: 600;
}

.type-badge.temp {
  background: rgba(245, 87, 108, 0.2);
  color: #f5576c;
}

.type-badge.humidity {
  background: rgba(79, 172, 254, 0.2);
  color: #4facfe;
}

.type-badge.co2 {
  background: rgba(102, 126, 234, 0.2);
  color: #667eea;
}

.type-badge.air-quality {
  background: rgba(67, 233, 123, 0.2);
  color: #43e97b;
}

.number {
  text-align: right;
  font-variant-numeric: tabular-nums;
}

.summary-stats {
  display: grid;
  grid-template-columns: repeat(auto-fit, minmax(200px, 1fr));
  gap: 1rem;
  margin-top: 1.875rem;
  padding-top: 1.25rem;
  border-top: 2px solid var(--border);
}

.stat-card {
  text-align: center;
  padding: 1.5rem;
  background: var(--gradient-1);
  color: white;
  border-radius: 1rem;
  box-shadow: 0 8px 20px rgba(102, 126, 234, 0.4);
  position: relative;
  overflow: hidden;
  border: 2px solid rgba(102, 126, 234, 0.5);
  transition: all 0.3s ease;
}

.stat-card:hover {
  transform: translateY(-3px);
  box-shadow: 0 12px 30px rgba(102, 126, 234, 0.5);
  border-color: rgba(102, 126, 234, 0.8);
}

.stat-card::before {
  content: "";
  position: absolute;
  top: 0;
  left: 0;
  right: 0;
  bottom: 0;
  background: linear-gradient(
    135deg,
    rgba(255, 255, 255, 0.2) 0%,
    rgba(255, 255, 255, 0.05) 100%
  );
  z-index: 0;
  pointer-events: none;
}

.stat-card > * {
  position: relative;
  z-index: 1;
}

.stat-card:nth-child(2) {
  background: var(--gradient-2);
  box-shadow: 0 8px 20px rgba(245, 87, 108, 0.4);
  border-color: rgba(245, 87, 108, 0.5);
}

.stat-card:nth-child(2):hover {
  box-shadow: 0 12px 30px rgba(245, 87, 108, 0.5);
  border-color: rgba(245, 87, 108, 0.8);
}

.stat-card:nth-child(3) {
  background: var(--gradient-3);
  box-shadow: 0 8px 20px rgba(79, 172, 254, 0.4);
  border-color: rgba(79, 172, 254, 0.5);
}

.stat-card:nth-child(3):hover {
  box-shadow: 0 12px 30px rgba(79, 172, 254, 0.5);
  border-color: rgba(79, 172, 254, 0.8);
}

.stat-label {
  font-size: 0.9em;
  opacity: 0.9;
  margin-bottom: 0.625rem;
}

.stat-value {
  font-size: 1.8em;
  font-weight: 700;
}

.btn {
  display: inline-flex;
  align-items: center;
  gap: 0.5rem;
  padding: 0.625rem 1.25rem;
  border: none;
  border-radius: 0.5rem;
  cursor: pointer;
  font-weight: 600;
  transition: all 0.3s;
  font-size: 0.875rem;
}

.btn-primary {
  background: var(--primary);
  color: white;
}

.btn-primary:hover {
  background: var(--primary-hover);
  transform: translateY(-2px);
  box-shadow: 0 4px 12px rgba(99, 102, 241, 0.4);
}

.loading {
  text-align: center;
  padding: 3.75rem 1.25rem;
  color: var(--text-muted);
}

.spinner {
  width: 50px;
  height: 50px;
  border: 3px solid var(--border);
  border-top-color: var(--primary);
  border-radius: 50%;
  animation: spin 1s linear infinite;
  margin: 0 auto 1.25rem;
}

@keyframes spin {
  to {
    transform: rotate(360deg);
  }
}

.empty-state {
  text-align: center;
  padding: 3.75rem 1.25rem;
  color: var(--text-muted);
}

.empty-state p {
  font-size: 1.1em;
  margin: 0.625rem 0;
}
</style>
