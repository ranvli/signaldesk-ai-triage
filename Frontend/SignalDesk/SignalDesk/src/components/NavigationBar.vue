<template>
  <nav class="navbar">
    <div class="nav-container">
      <div class="brand">SignalDesk</div>
      <div class="nav-links">
        <router-link to="/">Submit Feedback</router-link>
        <router-link to="/queue">Queue</router-link>
        <router-link to="/dashboard">Dashboard</router-link>
      </div>
    </div>
  </nav>
</template>

<script setup>
</script>

<style scoped>
.navbar {
  background: #2c3e50;
  color: white;
  padding: 1rem 0;
  box-shadow: 0 2px 4px rgba(0,0,0,0.1);
}

.nav-container {
  max-width: 1200px;
  margin: 0 auto;
  padding: 0 2rem;
  display: flex;
  justify-content: space-between;
  align-items: center;
}

.brand {
  font-size: 1.5rem;
  font-weight: bold;
}

.nav-links {
  display: flex;
  gap: 2rem;
}

.nav-links a {
  color: white;
  text-decoration: none;
  padding: 0.5rem 1rem;
  border-radius: 4px;
  transition: background 0.3s;
}

.nav-links a:hover {
  background: rgba(255, 255, 255, 0.1);
}

.nav-links a.router-link-active {
  background: rgba(255, 255, 255, 0.2);
}
</style>
