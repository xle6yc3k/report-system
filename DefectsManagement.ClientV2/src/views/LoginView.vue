<template>
  <div class="login-container">
    <div class="login-box">
      <h1>Sign In</h1>

      <Banner
        v-if="showSessionExpired"
        message="Session expired. Please sign in again."
        type="warning"
        @close="showSessionExpired = false"
      />

      <Banner
        v-if="errorMessage"
        :message="errorMessage"
        type="error"
        @close="errorMessage = ''"
      />

      <form @submit.prevent="handleLogin">
        <div class="form-group">
          <label for="username">Username</label>
          <input
            id="username"
            v-model="username"
            type="text"
            required
            :disabled="authStore.isAuthLoading"
          />
        </div>

        <div class="form-group">
          <label for="password">Password</label>
          <input
            id="password"
            v-model="password"
            type="password"
            required
            :disabled="authStore.isAuthLoading"
          />
        </div>

        <button
          type="submit"
          class="btn-primary"
          :disabled="authStore.isAuthLoading"
        >
          {{ authStore.isAuthLoading ? 'Signing in...' : 'Sign In' }}
        </button>
      </form>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, onMounted } from 'vue';
import { useRoute } from 'vue-router';
import { useAuthStore } from '../stores/authStore';
import Banner from '../components/Banner.vue';

const route = useRoute();
const authStore = useAuthStore();

const username = ref('');
const password = ref('');
const errorMessage = ref('');
const showSessionExpired = ref(false);

onMounted(() => {
  if (route.query.reason === 'expired') {
    showSessionExpired.value = true;
  }
});

async function handleLogin() {
  errorMessage.value = '';

  try {
    await authStore.login(username.value, password.value);
  } catch (error: any) {
    if (error.response?.status === 401) {
      errorMessage.value = 'Invalid username or password.';
    } else {
      errorMessage.value = error.response?.data?.message || 'An error occurred. Please try again.';
    }
  }
}
</script>

<style scoped>
.login-container {
  display: flex;
  justify-content: center;
  align-items: center;
  min-height: 100vh;
  background-color: #f5f5f5;
}

.login-box {
  background: white;
  padding: 40px;
  border-radius: 8px;
  box-shadow: 0 2px 10px rgba(0, 0, 0, 0.1);
  width: 100%;
  max-width: 400px;
}

h1 {
  margin: 0 0 24px 0;
  font-size: 24px;
  text-align: center;
  color: #333;
}

.form-group {
  margin-bottom: 20px;
}

label {
  display: block;
  margin-bottom: 8px;
  font-weight: 500;
  color: #555;
}

input {
  width: 100%;
  padding: 10px 12px;
  border: 1px solid #ddd;
  border-radius: 4px;
  font-size: 14px;
  box-sizing: border-box;
}

input:focus {
  outline: none;
  border-color: #1976d2;
}

input:disabled {
  background-color: #f5f5f5;
  cursor: not-allowed;
}

.btn-primary {
  width: 100%;
  padding: 12px;
  background-color: #1976d2;
  color: white;
  border: none;
  border-radius: 4px;
  font-size: 16px;
  font-weight: 500;
  cursor: pointer;
  transition: background-color 0.2s;
}

.btn-primary:hover:not(:disabled) {
  background-color: #1565c0;
}

.btn-primary:disabled {
  background-color: #90caf9;
  cursor: not-allowed;
}
</style>
