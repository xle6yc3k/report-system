// DefectsManagement.Client/src/stores/authStore.ts

import { defineStore } from 'pinia';
import apiClient from '@/http/api';
import router from '@/router';

// Типы данных
export interface UserInfo {
    id: number; // Соответствует вашему int ID в БД
    username: string;
    // name: string; // Удалено, так как JWT не всегда содержит name, но содержит username.
    role: 'Engineer' | 'Manager' | 'Observer' | 'Admin';
}
export interface AuthState {
    // УДАЛЕНО: token: string | null;
    userInfo: UserInfo | null;
    isAuthenticated: boolean; // Добавляем явно, чтобы не зависеть от token
}

// УДАЛЯЕМ вспомогательную функцию decodeToken, т.к. мы больше не декодируем токен на фронте.
// const decodeToken = (token: string): UserInfo | null => { ... };


export const useAuthStore = defineStore('auth', {
    state: (): AuthState => ({
        // УДАЛЕНО: token: localStorage.getItem('jwt_token') || null,
        userInfo: null,
        isAuthenticated: false, // Изначально не аутентифицирован
    }),
    
    getters: {
        // УДАЛЕНО: isAuthenticated: (state) => !!state.token,
        userRole: (state) => state.userInfo?.role,
    },

    actions: {
        // НОВОЕ: Метод для получения информации о пользователе с сервера
        async fetchUserInfo() {
            try {
                const response = await apiClient.get('/User/me');
                this.userInfo = response.data as UserInfo;
                this.isAuthenticated = true; // Успешно получили данные = аутентифицированы
            } catch (e) {
                // Если запрос к /User/me не удался (например, 401), значит куки не действительны.
                this.logout();
            }
        },

        // Обновляем initialize
        initialize() {
            // Теперь мы всегда проверяем состояние аутентификации на сервере
            // при загрузке приложения.
            this.fetchUserInfo(); 
        },

        async login(username: string, password: string) {
            try {
                // 1. Отправляем запрос на вход. Сервер установит куку.
                await apiClient.post('/Auth/login', { username, password });
                
                // 2. Если запрос успешен, делаем второй запрос, чтобы получить данные пользователя
                // и подтвердить, что кука была принята браузером и отправлена обратно.
                await this.fetchUserInfo(); 

                // 3. Если все прошло успешно, переходим на главную
                router.push('/');
                
            } catch (error: any) {
                console.error('Ошибка входа:', error.response?.data || error.message);
                
                // Убедимся, что состояние сброшено в случае неудачи
                this.isAuthenticated = false; 
                this.userInfo = null; 

                throw new Error(error.response?.data?.message || 'Неверный логин или пароль');
            }
        },

        logout() {
            // Опционально: можно добавить запрос к API для удаления куки на стороне сервера
            // apiClient.post('/Auth/logout'); 
            
            this.userInfo = null;
            this.isAuthenticated = false; // Очищаем состояние
            // УДАЛЕНО: localStorage.removeItem('jwt_token'); 
            router.push('/login');
        }
    },
});