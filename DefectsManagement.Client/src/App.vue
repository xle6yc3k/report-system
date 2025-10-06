<script setup lang="ts">
import { RouterView, RouterLink, useRoute } from 'vue-router';
import { computed } from 'vue';
import { useAuthStore } from '@/stores/authStore';

const authStore = useAuthStore();
const route = useRoute();

// Показываем экран загрузки только на защищённых маршрутах
const isProtected = computed(() => route.meta?.requiresAuth === true);

/**
 * Асинхронный выход из системы.
 * Использует authStore.logout(), который отправляет запрос /Auth/logout
 * и затем вызывает forceLogout() для очистки состояния и редиректа.
 */
const handleLogout = async () => {
  if (authStore.isAuthLoading) return; // защита от двойного клика
  await authStore.logout();
};
</script>

<template>
  <div id="app-wrapper">
    <!-- Состояние загрузки (только на защищённых страницах) -->
    <div v-if="authStore.isAuthLoading && isProtected" class="loading-state">
      Загрузка состояния аутентификации...
    </div>

    <!-- Когда загрузка завершена -->
    <template v-else>
      <!-- Навигация доступна только авторизованным пользователям -->
      <nav v-if="authStore.isAuthenticated" class="main-nav">
        <RouterLink to="/" class="nav-item">Главная (Дефекты)</RouterLink>
        <RouterLink to="/profile" class="nav-item">Профиль</RouterLink>

        <!-- Кнопка выхода -->
        <button
          @click="handleLogout()"
          :disabled="authStore.isAuthLoading"
          class="btn-logout-nav"
        >
          Выйти ({{ authStore.userInfo?.username }})
        </button>
      </nav>

      <!-- Основной контент -->
      <main class="content">
        <RouterView />
      </main>
    </template>
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

/* Экран загрузки */
.loading-state {
  display: flex;
  justify-content: center;
  align-items: center;
  height: 100vh;
  font-size: 1.5em;
  color: #34495e;
  font-weight: bold;
  background-color: white;
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

.btn-logout-nav:hover:not(:disabled) {
  background-color: #5a7693;
}

.btn-logout-nav:disabled {
  opacity: 0.6;
  cursor: not-allowed;
}

.content {
  flex-grow: 1;
  padding: 0;
}
</style>
