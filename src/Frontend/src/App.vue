<template>
  <div id="app">
    <aside class="sidebar">
      <div class="logo">🖥️ Data Center</div>
      <ul class="nav-links">
        <li>
          <router-link to="/"> <span>📊</span> Dashboard </router-link>
        </li>
        <li>
          <router-link to="/data"> <span>📋</span> Dane </router-link>
        </li>
        <li>
          <router-link to="/charts"> <span>📈</span> Wykresy </router-link>
        </li>
        <li>
          <router-link to="/blockchain">
            <span>🔗</span> Blockchain
          </router-link>
        </li>
      </ul>
    </aside>
    <main class="main-content">
      <router-view />
    </main>
  </div>
</template>

<script>
export default {
  name: "App",
}
</script>
