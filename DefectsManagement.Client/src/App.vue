<script setup lang="ts">
import { RouterView } from 'vue-router'
import { useAuthStore } from '@/stores/authStore';

const authStore = useAuthStore();
</script>

<template>
  <div id="app-wrapper">
    
    <nav v-if="authStore.isAuthenticated" class="main-nav">
      <RouterLink to="/" class="nav-item">Главная (Дефекты)</RouterLink>
      <button @click="authStore.logout()" class="btn-logout-nav">
        Выйти ({{ authStore.userInfo?.username }})
      </button>
    </nav>

    <main class="content">
      <RouterView />
    </main>
  </div>
</template>

<style>
body {
    font-family: Arial, sans-serif;
    margin: 0;
    padding: 0;
    background-color: #f4f4f4;
}

#app-wrapper {
    display: flex;
    flex-direction: column;
    min-height: 100vh;
}

.main-nav {
    display: flex;
    align-items: center;
    padding: 15px 30px;
    background-color: #34495e;
    color: white;
    box-shadow: 0 2px 4px rgba(0, 0, 0, 0.2);
}

.nav-item {
    color: white;
    text-decoration: none;
    margin-right: 20px;
    font-weight: bold;
    transition: color 0.2s;
}

.nav-item:hover {
    color: #42b983;
}

.btn-logout-nav {
    margin-left: auto;
    background: none;
    border: 1px solid white;
    color: white;
    padding: 8px 15px;
    cursor: pointer;
    border-radius: 4px;
    transition: background-color 0.2s;
}

.btn-logout-nav:hover {
    background-color: #5a7693;
}

.content {
    flex-grow: 1;
    padding: 0; 
}
</style>