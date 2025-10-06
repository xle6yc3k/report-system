import { defineStore } from 'pinia';
import apiClient from '@/http/api';
import router from '@/router';
import axios from 'axios';

export interface UserInfo {
  id: number;
  username: string;
  role: 'Engineer' | 'Manager' | 'Observer' | 'Admin';
}
export interface AuthState {
  userInfo: UserInfo | null;
  isAuthenticated: boolean;
  isAuthLoading: boolean;
  // добавлено
  isInitialized: boolean;
  isPostLogoutRedirect: boolean;
  lastLogoutReason: 'manual' | 'expired' | 'unknown' | null;
}

export const useAuthStore = defineStore('auth', {
  state: (): AuthState => ({
    userInfo: null,
    isAuthenticated: false,
    isAuthLoading: false,      // стартуем как false, init сам включит загрузку
    isInitialized: false,      // добавлено
    isPostLogoutRedirect: false, // добавлено
    lastLogoutReason: null,    // добавлено
  }),

  getters: {
    userRole: (state) => state.userInfo?.role,
  },

  actions: {
    async fetchUserInfo() {
      // Критично: если мы в пост-редиректе, НЕ дергаем /User/me
      if (this.isPostLogoutRedirect) {
        this.isAuthLoading = false;
        return;
      }

      this.isAuthLoading = true;
      try {
        const response = await apiClient.get('/User/me');
        this.userInfo = response.data as UserInfo;
        this.isAuthenticated = true;
      } catch (error) {
        if (axios.isAxiosError(error) && error.response?.status === 401) {
          this.isAuthenticated = false;
          this.userInfo = null;
        } else {
          console.error('Ошибка при загрузке информации о пользователе:', error);
          this.isAuthenticated = false;
          this.userInfo = null;
        }
      } finally {
        this.isAuthLoading = false;
      }
    },

    async initialize() {
      if (this.isInitialized) return;
      this.isInitialized = true;
      await this.fetchUserInfo();
    },

    async login(username: string, password: string) {
      this.isAuthLoading = true;
      try {
        await apiClient.post('/Auth/login', { username, password });
        await this.fetchUserInfo();
        if (this.isAuthenticated) {
          await router.replace('/'); // replace вместо push
          return true;
        }
        this.userInfo = null;
        this.isAuthenticated = false;
        throw new Error('Неверный логин или пароль');
        } catch (error: any) {
            if (error?.__silenced401) {
            // сессия истекла, всё уже обработали через forceLogout
            this.isAuthenticated = false;
            this.userInfo = null;
            } else if (axios.isAxiosError(error) && error.response?.status === 401) {
            this.isAuthenticated = false;
            this.userInfo = null;
            } else {
            console.error('Ошибка при загрузке информации о пользователе:', error);
            this.isAuthenticated = false;
            this.userInfo = null;
            }
        } finally {
            this.isAuthLoading = false;
        }      
    },

    async logout() {
      try {
        await apiClient.post('/Auth/logout');
      } catch (error) {
        console.warn('Ошибка при запросе на выход (может, cookie уже недействителен).', error);
      } finally {
        await this.forceLogout('manual');
      }
    },

    async forceLogout(reason: 'manual' | 'expired' | 'unknown' = 'unknown') {
      this.lastLogoutReason = reason;

      // локальная очистка
      this.userInfo = null;
      this.isAuthenticated = false;

      // ставим "стоп-кран" до завершения навигации
      this.isPostLogoutRedirect = true;

      // сбросим флаг инициализации — пусть на /login мы инициализируемся заново при необходимости
      this.isInitialized = false;

      try {
        await router.replace({ name: 'login', query: reason ? { reason } : undefined });
      } finally {
        // снимаем стоп-кран после навигации
        this.isPostLogoutRedirect = false;
      }
    },
  },
});
