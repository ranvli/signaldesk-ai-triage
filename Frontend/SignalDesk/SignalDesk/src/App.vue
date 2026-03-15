<template>
  <div id="app">
    <NavigationBar />
    <main class="container">
      <router-view />
    </main>
  </div>
</template>

<script setup>
import NavigationBar from './components/NavigationBar.vue'
</script>

<style scoped>
#app {
  min-height: 100vh;
  background: #f5f5f5;
}

.container {
  max-width: 1200px;
  margin: 0 auto;
  padding: 2rem;
}
</style>
