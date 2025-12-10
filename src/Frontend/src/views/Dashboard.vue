<template>
  <div>
    <header class="page-header">
      <h1>📊 Dashboard</h1>
      <p>Monitorowanie sensorów w czasie rzeczywistym</p>
    </header>

    <div v-if="loading" class="loading">
      <div class="spinner"></div>
      <p>Ładowanie danych...</p>
    </div>

    <div v-else-if="sensors.length === 0" class="empty-state">
      <p>🔌 Oczekiwanie na dane z sensorów...</p>
      <p class="pulse">Upewnij się, że symulator jest uruchomiony</p>
    </div>

    <div v-else class="sensor-grid">
      <div
        v-for="sensor in sortedSensors"
        :key="sensor.sensorId"
        :class="['sensor-card', sensor.sensorType.toLowerCase()]"
      >
        <div class="sensor-header">
          <div class="sensor-icon">{{ getIcon(sensor.sensorType) }}</div>
          <span class="sensor-location">{{ sensor.location }}</span>
        </div>
        <div class="sensor-info">
          <h3>{{ getSensorName(sensor.sensorType) }}</h3>
          <div class="sensor-value">
            {{ formatValue(sensor.lastValue) }}
            <span>{{ sensor.unit }}</span>
          </div>
        </div>
        <div class="sensor-stats">
          <div class="stat">
            <span class="stat-label">Średnia (100)</span>
            <span class="stat-value"
              >{{ formatValue(sensor.averageValue) }} {{ sensor.unit }}</span
            >
          </div>
          <div class="stat">
            <span class="stat-label">Ostatnia aktualizacja</span>
            <span class="stat-value">{{
              formatTime(sensor.lastTimestamp)
            }}</span>
          </div>
        </div>
      </div>
    </div>
  </div>
</template>

<script>
import { ref, computed, onMounted, onUnmounted } from "vue"
import * as signalR from "@microsoft/signalr"
import axios from "axios"

export default {
  name: "Dashboard",
  setup() {
    const sensors = ref([])
    const loading = ref(true)
    let connection = null

    const API_URL = import.meta.env.PROD ? "" : "http://localhost:5000"

    // Define sort order for sensor types
    const typeOrder = { TEMP: 1, HUMIDITY: 2, CO2: 3, AIR_QUALITY: 4 }
    const locationOrder = {
      "Serwerownia 1": 1,
      "Serwerownia 2": 2,
      "Serwerownia 3": 3,
      "Serwerownia 4": 4,
      "Chłodzenie 1": 5,
      "Chłodzenie 2": 6,
      "Chłodzenie 3": 7,
      "Chłodzenie 4": 8,
      "UPS 1": 9,
      "UPS 2": 10,
      "UPS 3": 11,
      "UPS 4": 12,
      "Filtr powietrza 1": 13,
      "Filtr powietrza 2": 14,
      "Filtr powietrza 3": 15,
      "Filtr powietrza 4": 16,
    }

    // Computed property to sort sensors
    const sortedSensors = computed(() => {
      return [...sensors.value].sort((a, b) => {
        // First sort by type
        const typeA = typeOrder[a.sensorType] || 99
        const typeB = typeOrder[b.sensorType] || 99
        if (typeA !== typeB) return typeA - typeB
        // Then sort by location
        const locA = locationOrder[a.location] || 99
        const locB = locationOrder[b.location] || 99
        return locA - locB
      })
    })

    const fetchDashboard = async () => {
      try {
        const response = await axios.get(`${API_URL}/api/sensors/dashboard`)
        sensors.value = response.data
      } catch (error) {
        console.error("Error fetching dashboard:", error)
      } finally {
        loading.value = false
      }
    }

    const setupSignalR = async () => {
      connection = new signalR.HubConnectionBuilder()
        .withUrl(`${API_URL}/sensorhub`)
        .withAutomaticReconnect()
        .build()

      connection.on("NewReading", (reading) => {
        const index = sensors.value.findIndex(
          (s) => s.sensorId === reading.sensorId
        )
        if (index !== -1) {
          sensors.value[index].lastValue = reading.value
          sensors.value[index].lastTimestamp = reading.timestamp
          const current = sensors.value[index]
          current.averageValue =
            (current.averageValue * 99 + reading.value) / 100
        } else {
          fetchDashboard()
        }
      })

      try {
        await connection.start()
        console.log("SignalR connected")
      } catch (error) {
        console.error("SignalR connection error:", error)
      }
    }

    const getIcon = (type) => {
      const icons = { TEMP: "🌡️", HUMIDITY: "💧", CO2: "☁️", AIR_QUALITY: "🌿" }
      return icons[type] || "📡"
    }

    const getSensorName = (type) => {
      const names = {
        TEMP: "Temperatura",
        HUMIDITY: "Wilgotność",
        CO2: "Dwutlenek węgla",
        AIR_QUALITY: "Jakość powietrza PM2.5",
      }
      return names[type] || type
    }

    const formatValue = (value) => {
      if (value === null || value === undefined) return "-"
      return value.toFixed(1)
    }

    const formatTime = (timestamp) => {
      if (!timestamp) return "-"
      return new Date(timestamp).toLocaleTimeString("pl-PL")
    }

    onMounted(async () => {
      await fetchDashboard()
      await setupSignalR()
    })

    onUnmounted(() => {
      if (connection) {
        connection.stop()
      }
    })

    return {
      sensors,
      sortedSensors,
      loading,
      getIcon,
      getSensorName,
      formatValue,
      formatTime,
    }
  },
}
</script>
