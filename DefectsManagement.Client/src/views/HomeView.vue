<template>
  <div>
    <h1>Home</h1>
  </div>
  <button @click="logout">Logout</button>
</template>

<script setup lang="ts">
import {useAuthStore} from '@/stores/authStore';

const authStore = useAuthStore();

const logout = () => {
  authStore.logout();
}
</script>

<style scoped></style>