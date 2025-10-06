<script setup lang="ts">
import { computed } from 'vue';
import { useRoute, RouterView } from 'vue-router';
import { useAuthStore } from './stores/authStore';

const route = useRoute();
const authStore = useAuthStore();

const showLoading = computed(() => {
  return authStore.isAuthLoading && route.meta.requiresAuth;
});

function handleLogout() {
  authStore.logout();
}
</script>

<template>
  <div id="app">
    <nav v-if="authStore.isAuthenticated" class="navbar">
      <div class="nav-content">
        <h2 class="nav-title">Defect Tracker</h2>
        <div class="nav-right">
          <span class="user-info">
            {{ authStore.userInfo?.username }} ({{ authStore.userInfo?.role }})
          </span>
          <button
            @click="handleLogout"
            class="btn-logout"
            :disabled="authStore.isAuthLoading"
          >
            Logout
          </button>
        </div>
      </div>
    </nav>

    <div v-if="showLoading" class="loading-overlay">
      <div class="loading-spinner"></div>
      <p>Loading...</p>
    </div>

    <main>
      <RouterView />
    </main>
  </div>
</template>

<style>
* {
  margin: 0;
  padding: 0;
  box-sizing: border-box;
}

body {
  font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, Oxygen, Ubuntu, Cantarell, sans-serif;
  background-color: #f5f5f5;
  color: #333;
}

#app {
  min-height: 100vh;
}

.navbar {
  background-color: white;
  box-shadow: 0 2px 4px rgba(0, 0, 0, 0.1);
  position: sticky;
  top: 0;
  z-index: 100;
}

.nav-content {
  max-width: 1200px;
  margin: 0 auto;
  padding: 16px 24px;
  display: flex;
  justify-content: space-between;
  align-items: center;
}

.nav-title {
  font-size: 20px;
  color: #1976d2;
  margin: 0;
}

.nav-right {
  display: flex;
  align-items: center;
  gap: 16px;
}

.user-info {
  font-size: 14px;
  color: #666;
}

.btn-logout {
  padding: 8px 16px;
  background-color: #f5f5f5;
  color: #333;
  border: 1px solid #ddd;
  border-radius: 4px;
  font-size: 14px;
  cursor: pointer;
  transition: background-color 0.2s;
}

.btn-logout:hover:not(:disabled) {
  background-color: #e0e0e0;
}

.btn-logout:disabled {
  opacity: 0.6;
  cursor: not-allowed;
}

.loading-overlay {
  position: fixed;
  top: 0;
  left: 0;
  right: 0;
  bottom: 0;
  background-color: rgba(255, 255, 255, 0.9);
  display: flex;
  flex-direction: column;
  justify-content: center;
  align-items: center;
  z-index: 9999;
}

.loading-spinner {
  width: 50px;
  height: 50px;
  border: 4px solid #f3f3f3;
  border-top: 4px solid #1976d2;
  border-radius: 50%;
  animation: spin 1s linear infinite;
}

@keyframes spin {
  0% { transform: rotate(0deg); }
  100% { transform: rotate(360deg); }
}

.loading-overlay p {
  margin-top: 16px;
  color: #666;
  font-size: 16px;
}

main {
  min-height: calc(100vh - 60px);
}
</style>
