<template>
  <div class="charts-page">
    <header class="page-header">
      <h1>📈 Wykresy</h1>
      <p>Wizualizacja danych sensorów</p>
    </header>

      <div class="chart-container">
        <div class="table-header">
          <div class="filters">
            <select
              v-model="selectedSensor"
              class="filter-select"
              @change="fetchChartData"
            >
              <option value="">Wybierz sensor</option>
              <option v-for="id in sensorIds" :key="id" :value="id">
                {{ id }}
              </option>
            </select>
            <input
              type="date"
              v-model="dateFrom"
              class="filter-input"
              @change="fetchChartData"
            />
            <input
              type="date"
              v-model="dateTo"
              class="filter-input"
              @change="fetchChartData"
            />
            <button class="btn btn-primary" @click="fetchChartData" title="Odśwież wykres">
              <span class="btn-icon">🔄</span>
              <span>Odśwież</span>
            </button>
          </div>
        </div>

      <div v-if="loading" class="loading">
        <div class="spinner"></div>
      </div>

      <div v-else-if="!selectedSensor" class="empty-state">
        <p>📊 Wybierz sensor, aby zobaczyć wykres</p>
      </div>

      <div v-else-if="chartData.labels.length === 0" class="empty-state">
        <p>📭 Brak danych dla wybranego sensora</p>
      </div>

      <div v-else class="chart-area">
        <Line :data="chartData" :options="chartOptions" />
      </div>
    </div>

    <div class="sensor-grid" v-if="sensorTypes.length > 0">
      <div v-for="type in sensorTypes" :key="type" class="chart-container">
        <div class="chart-header">
          <h3>{{ getIcon(type) }} {{ getSensorName(type) }}</h3>
          <p>Ostatnie 50 odczytów wszystkich sensorów</p>
        </div>
        <div class="type-chart-area">
          <Line
            :data="typeCharts[type] || emptyChart"
            :options="chartOptions"
          />
        </div>
      </div>
    </div>
  </div>
</template>

<script>
import { ref, reactive, onMounted } from "vue"
import axios from "axios"
import { Line } from "vue-chartjs"
import {
  Chart as ChartJS,
  CategoryScale,
  LinearScale,
  PointElement,
  LineElement,
  Title,
  Tooltip,
  Legend,
  Filler,
} from "chart.js"

ChartJS.register(
  CategoryScale,
  LinearScale,
  PointElement,
  LineElement,
  Title,
  Tooltip,
  Legend,
  Filler
)

export default {
  name: "Charts",
  components: { Line },
  setup() {
    const loading = ref(false)
    const sensorIds = ref([])
    const sensorTypes = ref([])
    const selectedSensor = ref("")
    const dateFrom = ref("")
    const dateTo = ref("")

    const API_URL = import.meta.env.PROD ? "" : "http://localhost:5000"

    const colors = {
      TEMP: { border: "#f5576c", background: "rgba(245, 87, 108, 0.1)" },
      HUMIDITY: { border: "#4facfe", background: "rgba(79, 172, 254, 0.1)" },
      CO2: { border: "#667eea", background: "rgba(102, 126, 234, 0.1)" },
      AIR_QUALITY: { border: "#43e97b", background: "rgba(67, 233, 123, 0.1)" },
    }

    const emptyChart = {
      labels: [],
      datasets: [],
    }

    const chartData = ref({ ...emptyChart })
    const typeCharts = reactive({})

    const chartOptions = {
      responsive: true,
      maintainAspectRatio: false,
      plugins: {
        legend: {
          labels: { color: "#cdd6f4" },
        },
      },
      scales: {
        x: {
          ticks: { color: "#a6adc8" },
          grid: { color: "rgba(69, 71, 90, 0.5)" },
        },
        y: {
          ticks: { color: "#a6adc8" },
          grid: { color: "rgba(69, 71, 90, 0.5)" },
        },
      },
    }

    const fetchSensorIds = async () => {
      try {
        const response = await axios.get(`${API_URL}/api/sensors/ids`)
        sensorIds.value = response.data
      } catch (error) {
        console.error("Error fetching IDs:", error)
      }
    }

    const fetchSensorTypes = async () => {
      try {
        const response = await axios.get(`${API_URL}/api/sensors/types`)
        sensorTypes.value = response.data
        // Fetch charts for each type
        for (const type of response.data) {
          await fetchTypeChart(type)
        }
      } catch (error) {
        console.error("Error fetching types:", error)
      }
    }

    const fetchChartData = async () => {
      if (!selectedSensor.value) return

      loading.value = true
      try {
        const params = new URLSearchParams()
        params.append("sensorId", selectedSensor.value)
        if (dateFrom.value) params.append("dateFrom", dateFrom.value)
        if (dateTo.value) params.append("dateTo", dateTo.value + "T23:59:59")
        params.append("sortBy", "timestamp")
        params.append("sortOrder", "asc")
        params.append("pageSize", "100")

        const response = await axios.get(`${API_URL}/api/sensors?${params}`)
        const readings = response.data.data

        if (readings.length > 0) {
          const type = readings[0].sensorType
          const color = colors[type] || {
            border: "#6366f1",
            background: "rgba(99, 102, 241, 0.1)",
          }

          chartData.value = {
            labels: readings.map((r) =>
              new Date(r.timestamp).toLocaleTimeString("pl-PL")
            ),
            datasets: [
              {
                label: `${selectedSensor.value} (${readings[0].unit})`,
                data: readings.map((r) => r.value),
                borderColor: color.border,
                backgroundColor: color.background,
                fill: true,
                tension: 0.4,
              },
            ],
          }
        } else {
          chartData.value = { ...emptyChart }
        }
      } catch (error) {
        console.error("Error fetching chart data:", error)
      } finally {
        loading.value = false
      }
    }

    const fetchTypeChart = async (type) => {
      try {
        const params = new URLSearchParams()
        params.append("sensorType", type)
        params.append("sortBy", "timestamp")
        params.append("sortOrder", "desc")
        params.append("pageSize", "50")

        const response = await axios.get(`${API_URL}/api/sensors?${params}`)
        const readings = response.data.data.reverse()

        if (readings.length > 0) {
          const color = colors[type] || {
            border: "#6366f1",
            background: "rgba(99, 102, 241, 0.1)",
          }

          typeCharts[type] = {
            labels: readings.map((r) =>
              new Date(r.timestamp).toLocaleTimeString("pl-PL")
            ),
            datasets: [
              {
                label: `${type} (${readings[0].unit})`,
                data: readings.map((r) => r.value),
                borderColor: color.border,
                backgroundColor: color.background,
                fill: true,
                tension: 0.4,
              },
            ],
          }
        }
      } catch (error) {
        console.error("Error fetching type chart:", error)
      }
    }

    const getIcon = (type) => {
      const icons = {
        TEMP: "🌡️",
        HUMIDITY: "💧",
        CO2: "☁️",
        AIR_QUALITY: "🌿",
      }
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

    onMounted(async () => {
      await fetchSensorIds()
      await fetchSensorTypes()
    })

    return {
      loading,
      sensorIds,
      sensorTypes,
      selectedSensor,
      dateFrom,
      dateTo,
      chartData,
      typeCharts,
      emptyChart,
      chartOptions,
      fetchChartData,
      getIcon,
      getSensorName,
    }
  },
}
</script>

<style scoped>
.charts-page {
  display: flex;
  flex-direction: column;
  gap: 1.25rem;
  width: 100%;
}

.charts-page .page-header {
  margin-bottom: 1.25rem;
}

.charts-page .chart-container {
  padding: 1rem;
  margin-bottom: 0.75rem;
  width: 100%;
}

.charts-page .chart-header h3 {
  font-size: 0.95rem;
}

.charts-page .chart-header p {
  font-size: 0.85rem;
}

.chart-area {
  height: 280px;
  width: 100%;
}

.type-chart-area {
  height: 200px;
  width: 100%;
}

.charts-page .sensor-grid {
  grid-template-columns: repeat(2, minmax(0, 1fr));
  gap: 1rem;
  width: 100%;
}

@media (max-width: 900px) {
  .charts-page .sensor-grid {
    grid-template-columns: 1fr;
  }
}

.charts-page :deep(canvas) {
  width: 100% !important;
}

</style>
