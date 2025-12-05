import { createApp } from 'vue'
import { createRouter, createWebHistory } from 'vue-router'
import App from './App.vue'
import Dashboard from './views/Dashboard.vue'
import DataTable from './views/DataTable.vue'
import Charts from './views/Charts.vue'
import './style.css'

const routes = [
    { path: '/', name: 'Dashboard', component: Dashboard },
    { path: '/data', name: 'DataTable', component: DataTable },
    { path: '/charts', name: 'Charts', component: Charts }
]

const router = createRouter({
    history: createWebHistory(),
    routes
})

createApp(App).use(router).mount('#app')
