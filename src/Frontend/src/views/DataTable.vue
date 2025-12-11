<template>
  <div>
    <header class="page-header">
      <div class="header-content">
        <div>
          <h1>📋 Dane sensorów</h1>
          <p>Przeglądaj, filtruj i eksportuj dane</p>
        </div>
        <div v-if="autoRefresh" class="auto-refresh-status-inline">
          <span class="refresh-icon">🔄</span>
          <span>Auto-odświeżanie aktywne</span>
        </div>
      </div>
    </header>

    <div class="data-table-wrapper">

      <div class="table-header">
        <div class="filters">
          <input
            type="date"
            v-model="filters.dateFrom"
            class="filter-input"
            placeholder="Od daty"
          />
          <input
            type="date"
            v-model="filters.dateTo"
            class="filter-input"
            placeholder="Do daty"
          />
          <select
            v-model="filters.sensorType"
            class="filter-select"
            @change="onSensorTypeChange"
          >
            <option value="">Wszystkie typy</option>
            <option v-for="type in sensorTypes" :key="type" :value="type">
              {{ getSensorName(type) }}
            </option>
          </select>
          <select
            v-model="filters.sensorId"
            class="filter-select"
            :disabled="!filters.sensorType && sensorIds.length === 0"
          >
            <option value="">Wszystkie sensory</option>
            <option v-for="id in sensorIds" :key="id" :value="id">
              {{ id }}
            </option>
          </select>
          <button
            :class="['btn', autoRefresh ? 'btn-danger' : 'btn-success']"
            @click="toggleAutoRefresh"
            :title="autoRefresh ? 'Kliknij, aby zatrzymać auto-odświeżanie' : 'Kliknij, aby włączyć auto-odświeżanie'"
          >
            <span class="btn-icon">{{ autoRefresh ? "⏸️" : "▶️" }}</span>
            <span>{{ autoRefresh ? "Pauza" : "Włącz Auto" }}</span>
          </button>
        </div>
        <div class="export-buttons">
          <button class="btn btn-secondary" @click="exportCsv">
            <span class="btn-icon">📄</span>
            <span>CSV</span>
          </button>
          <button class="btn btn-secondary" @click="exportJson">
            <span class="btn-icon">📋</span>
            <span>JSON</span>
          </button>
        </div>
      </div>

      <div v-if="loading" class="loading">
        <div class="spinner"></div>
      </div>

      <table v-else>
        <thead>
          <tr>
            <th
              @click="sortBy('timestamp')"
              :class="{ sorted: sort.field === 'timestamp' }"
            >
              Czas
              {{
                sort.field === "timestamp"
                  ? sort.order === "asc"
                    ? "↑"
                    : "↓"
                  : ""
              }}
            </th>
            <th
              @click="sortBy('sensorType')"
              :class="{ sorted: sort.field === 'sensorType' }"
            >
              Typ
              {{
                sort.field === "sensorType"
                  ? sort.order === "asc"
                    ? "↑"
                    : "↓"
                  : ""
              }}
            </th>
            <th
              @click="sortBy('sensorId')"
              :class="{ sorted: sort.field === 'sensorId' }"
            >
              Sensor
              {{
                sort.field === "sensorId"
                  ? sort.order === "asc"
                    ? "↑"
                    : "↓"
                  : ""
              }}
            </th>
            <th>Lokalizacja</th>
            <th
              @click="sortBy('value')"
              :class="{ sorted: sort.field === 'value' }"
            >
              Wartość
              {{
                sort.field === "value" ? (sort.order === "asc" ? "↑" : "↓") : ""
              }}
            </th>
          </tr>
        </thead>
        <tbody>
          <tr v-for="reading in data" :key="reading.id">
            <td>{{ formatDate(reading.timestamp) }}</td>
            <td>
              <span
                :class="['badge', 'badge-' + reading.sensorType.toLowerCase()]"
              >
                {{ reading.sensorType }}
              </span>
            </td>
            <td>{{ reading.sensorId }}</td>
            <td>{{ reading.location }}</td>
            <td>{{ reading.value.toFixed(2) }} {{ reading.unit }}</td>
          </tr>
        </tbody>
      </table>

      <div class="pagination">
        <button :disabled="page <= 1" @click="goToPage(page - 1)">
          ← Poprzednia
        </button>
        <span>Strona {{ page }} z {{ totalPages }}</span>
        <button :disabled="page >= totalPages" @click="goToPage(page + 1)">
          Następna →
        </button>
      </div>
    </div>
  </div>
</template>

<script>
import { ref, reactive, onMounted, onUnmounted } from "vue"
import axios from "axios"

export default {
  name: "DataTable",
  setup() {
    const data = ref([])
    const loading = ref(true)
    const sensorTypes = ref([])
    const sensorIds = ref([])
    const page = ref(1)
    const pageSize = 20
    const totalCount = ref(0)
    const totalPages = ref(1)
    const autoRefresh = ref(true)
    let refreshInterval = null

    const API_URL = import.meta.env.PROD ? "" : "http://localhost:5000"

    const filters = reactive({
      dateFrom: "",
      dateTo: "",
      sensorType: "",
      sensorId: "",
    })

    const sort = reactive({
      field: "timestamp",
      order: "desc",
    })

    const buildQueryParams = () => {
      const params = new URLSearchParams()
      if (filters.dateFrom) params.append("dateFrom", filters.dateFrom)
      if (filters.dateTo) params.append("dateTo", filters.dateTo + "T23:59:59")
      if (filters.sensorType) params.append("sensorType", filters.sensorType)
      if (filters.sensorId) params.append("sensorId", filters.sensorId)
      params.append("sortBy", sort.field)
      params.append("sortOrder", sort.order)
      params.append("page", page.value)
      params.append("pageSize", pageSize)
      return params.toString()
    }

    const fetchData = async () => {
      loading.value = true
      try {
        const response = await axios.get(
          `${API_URL}/api/sensors?${buildQueryParams()}`
        )
        data.value = response.data.data
        totalCount.value = response.data.totalCount
        totalPages.value = response.data.totalPages
      } catch (error) {
        console.error("Error fetching data:", error)
      } finally {
        loading.value = false
      }
    }

    const fetchDataSilent = async () => {
      try {
        const response = await axios.get(
          `${API_URL}/api/sensors?${buildQueryParams()}`
        )
        data.value = response.data.data
        totalCount.value = response.data.totalCount
        totalPages.value = response.data.totalPages
      } catch (error) {
        console.error("Error fetching data:", error)
      }
    }

    const startAutoRefresh = () => {
      if (refreshInterval) clearInterval(refreshInterval)
      if (autoRefresh.value) {
        refreshInterval = setInterval(() => {
          if (autoRefresh.value) {
            fetchDataSilent()
          }
        }, 1000)
      }
    }

    const toggleAutoRefresh = () => {
      autoRefresh.value = !autoRefresh.value
      if (autoRefresh.value) {
        startAutoRefresh()
      } else {
        if (refreshInterval) {
          clearInterval(refreshInterval)
          refreshInterval = null
        }
      }
    }

    const fetchSensorTypes = async () => {
      try {
        const response = await axios.get(`${API_URL}/api/sensors/types`)
        sensorTypes.value = response.data
      } catch (error) {
        console.error("Error fetching types:", error)
      }
    }

    const fetchSensorIds = async () => {
      try {
        const url = filters.sensorType
          ? `${API_URL}/api/sensors/ids?sensorType=${filters.sensorType}`
          : `${API_URL}/api/sensors/ids`
        const response = await axios.get(url)
        sensorIds.value = response.data
        if (filters.sensorId && !sensorIds.value.includes(filters.sensorId)) {
          filters.sensorId = ""
        }
      } catch (error) {
        console.error("Error fetching IDs:", error)
      }
    }

    const sortBy = (field) => {
      if (sort.field === field) {
        sort.order = sort.order === "asc" ? "desc" : "asc"
      } else {
        sort.field = field
        sort.order = "desc"
      }
      fetchData()
    }

    const goToPage = (newPage) => {
      page.value = newPage
      fetchData()
    }

    const exportCsv = () => {
      const params = buildQueryParams().replace(/page=\d+&pageSize=\d+/, "")
      window.open(`${API_URL}/api/sensors/export/csv?${params}`, "_blank")
    }

    const exportJson = () => {
      const params = buildQueryParams().replace(/page=\d+&pageSize=\d+/, "")
      window.open(`${API_URL}/api/sensors/export/json?${params}`, "_blank")
    }

    const onSensorTypeChange = async () => {
      await fetchSensorIds()
      if (filters.sensorId && !sensorIds.value.includes(filters.sensorId)) {
        filters.sensorId = ""
      }
      fetchData()
    }

    const getSensorName = (type) => {
      const names = {
        TEMP: "Temperatura",
        HUMIDITY: "Wilgotność",
        CO2: "Dwutlenek węgla",
        AIR_QUALITY: "Jakość powietrza",
      }
      return names[type] || type
    }

    const formatDate = (timestamp) => {
      return new Date(timestamp).toLocaleString("pl-PL")
    }

    onMounted(async () => {
      await fetchSensorTypes()
      await fetchSensorIds()
      await fetchData()
      if (autoRefresh.value) {
        startAutoRefresh()
      }
    })

    onUnmounted(() => {
      if (refreshInterval) {
        clearInterval(refreshInterval)
      }
    })

    return {
      data,
      loading,
      filters,
      sort,
      sensorTypes,
      sensorIds,
      page,
      totalPages,
      autoRefresh,
      fetchData,
      fetchSensorIds,
      onSensorTypeChange,
      sortBy,
      goToPage,
      exportCsv,
      exportJson,
      getSensorName,
      formatDate,
      toggleAutoRefresh,
    }
  },
}
</script>

<style scoped>
.header-content {
  display: flex;
  justify-content: space-between;
  align-items: center;
  gap: 2rem;
}

.auto-refresh-status-inline {
  display: flex;
  align-items: center;
  gap: 0.5rem;
  padding: 0.625rem 1.25rem;
  background: linear-gradient(135deg, rgba(34, 197, 94, 0.25) 0%, rgba(16, 185, 129, 0.25) 100%);
  border: 2px solid rgba(34, 197, 94, 0.5);
  border-radius: 0.75rem;
  color: #22c55e;
  font-size: 0.875rem;
  font-weight: 600;
  box-shadow: 0 6px 16px rgba(34, 197, 94, 0.4);
  white-space: nowrap;
}

.data-table-wrapper {
  position: relative;
}

.refresh-icon {
  animation: spin 2s linear infinite;
  display: inline-block;
  font-size: 1rem;
}

@keyframes spin {
  from {
    transform: rotate(0deg);
  }
  to {
    transform: rotate(360deg);
  }
}

.btn-icon {
  display: inline-flex;
  align-items: center;
  justify-content: center;
  font-size: 1rem;
  line-height: 1;
}

.btn-success {
  background: linear-gradient(135deg, #22c55e 0%, #16a34a 100%);
  color: white;
}
.btn-success:hover {
  background: linear-gradient(135deg, #16a34a 0%, #15803d 100%);
  transform: translateY(-1px);
  box-shadow: 0 4px 12px rgba(34, 197, 94, 0.4);
}
.btn-danger {
  background: linear-gradient(135deg, #ef4444 0%, #dc2626 100%);
  color: white;
}
.btn-danger:hover {
  background: linear-gradient(135deg, #dc2626 0%, #b91c1c 100%);
  transform: translateY(-1px);
  box-shadow: 0 4px 12px rgba(239, 68, 68, 0.4);
}
</style>
