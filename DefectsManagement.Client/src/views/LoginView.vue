<script setup lang="ts">
import { ref } from 'vue';
import { useAuthStore } from '@/stores/authStore';

const username = ref('');
const password = ref('');
const errorMessage = ref('');
const authStore = useAuthStore();

const handleLogin = async () => {
    errorMessage.value = ''; 
    
    try {
        // Логика login() определена в authStore
        await authStore.login(username.value, password.value);
    } catch (error: any) {
        errorMessage.value = error.message;
    }
};
</script>

<template>
    <div class="login-container">
        <h2>Вход в систему управления дефектами</h2>
        
        <form @submit.prevent="handleLogin" class="login-form">
            <div class="form-group">
                <label for="username">Имя пользователя:</label>
                <input type="text" id="username" v-model="username" required>
            </div>
            
            <div class="form-group">
                <label for="password">Пароль:</label>
                <input type="password" id="password" v-model="password" required>
            </div>
            
            <p v-if="errorMessage" class="error-message">{{ errorMessage }}</p>
            
            <button type="submit" class="btn-login">Войти</button>
        </form>
    </div>
</template>

<style scoped>

.login-container {
    max-width: 400px;
    margin: 50px auto;
    padding: 20px;
    border: 1px solid #ccc;
    border-radius: 8px;
    box-shadow: 0 2px 4px rgba(0, 0, 0, 0.1);
}

.btn-login {
    background-color: #4CAF50;
    color: white;
    padding: 10px 15px;
    border: none;
    border-radius: 4px;
    cursor: pointer;
    font-size: 16px;
    transition: background-color 0.3s;
}
</style>