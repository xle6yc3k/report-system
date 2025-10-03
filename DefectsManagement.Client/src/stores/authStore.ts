// DefectsManagement.Client/src/stores/authStore.ts

import { defineStore } from 'pinia';
import apiClient from '@/http/api';
import router from '@/router';

// Типы данных
export interface UserInfo {
    id: number;
    username: string;
    name: string;
    role: 'Engineer' | 'Manager' | 'Observer' | 'Admin';
}
export interface AuthState {
    token: string | null;
    userInfo: UserInfo | null;
}

// Вспомогательная функция для декодирования JWT-токена
const decodeToken = (token: string): UserInfo | null => {
    try {
        const payloadBase64 = token.split('.')[1];
        if (!payloadBase64) throw new Error('Invalid token format');
        const decoded = JSON.parse(atob(payloadBase64));

        return {
            id: Number(decoded.nameid), 
            username: decoded.sub,      
            name: decoded.name || decoded.sub, 
            role: decoded.role as UserInfo['role'],
        };
    } catch (e) {
        console.error('Ошибка при декодировании токена:', e);
        return null;
    }
};


export const useAuthStore = defineStore('auth', {
    state: (): AuthState => ({
        // Загружаем токен из localStorage при старте
        token: localStorage.getItem('jwt_token') || null,
        userInfo: null,
    }),
    
    getters: {
        isAuthenticated: (state) => !!state.token,
        userRole: (state) => state.userInfo?.role,
    },

    actions: {
        // Инициализация при старте приложения
        initialize() {
            if (this.token) {
                this.userInfo = decodeToken(this.token);
            }
        },

        async login(username: string, password: string) {
            try {
                const response = await apiClient.post('/Auth/login', { username, password });
                const token = response.data.token;

                if (token) {
                    this.token = token;
                    this.userInfo = decodeToken(token);
                    localStorage.setItem('jwt_token', token);
                    router.push('/');
                }
            } catch (error: any) {
                console.error('Ошибка входа:', error.response?.data || error.message);
                throw new Error(error.response?.data?.message || 'Неверный логин или пароль');
            }
        },

        logout() {
            this.token = null;
            this.userInfo = null;
            localStorage.removeItem('jwt_token');
            router.push('/login');
        }
    },
});