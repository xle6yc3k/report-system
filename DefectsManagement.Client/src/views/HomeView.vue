<template>
    <div class="home-container">
      <h1 class="text-3xl font-bold mb-4">Главная страница</h1>
      <p class="text-gray-600 mb-6">Это защищенный маршрут, доступный только после входа.</p>
  
      <!-- Добавляем кнопку Logout, если она нужна здесь -->
      <button @click="handleLogout" class="logout-button">Выйти</button>
    </div>
  </template>
  
  <script setup lang="ts">
  import { useAuthStore } from '@/stores/authStore';
  
  const authStore = useAuthStore();
  
  /**
   * Важно: Мы вызываем асинхронную функцию logout из стора.
   */
  const handleLogout = async () => {
    await authStore.logout();
  }
  </script>
  
  <style scoped>
  /* Добавлены базовые стили для красоты */
  .home-container {
      max-width: 800px;
      margin: 40px auto;
      padding: 20px;
      background-color: #ffffff;
      border-radius: 8px;
      box-shadow: 0 4px 12px rgba(0, 0, 0, 0.1);
      text-align: center;
  }
  
  .text-3xl {
      font-size: 1.875rem; 
      color: #34495e;
  }
  
  .font-bold {
      font-weight: 700;
  }
  
  .mb-4 {
      margin-bottom: 1rem;
  }
  
  .text-gray-600 {
      color: #4a5568;
  }
  
  .mb-6 {
      margin-bottom: 1.5rem;
  }
  
  .logout-button {
      background-color: #e53e3e; 
      color: white;
      padding: 10px 20px;
      border: none;
      border-radius: 4px;
      cursor: pointer;
      font-weight: 600;
      transition: background-color 0.3s ease;
  }
  
  .logout-button:hover {
      background-color: #c53030;
  }
  </style>
  