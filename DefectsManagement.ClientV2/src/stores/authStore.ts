import { defineStore } from 'pinia';
import { ref } from 'vue';
import api from '../http/api';
import router from '../router';

type UserRole = 'Engineer' | 'Manager' | 'Observer' | 'Admin';

interface UserInfo {
  id: number;
  username: string;
  role: UserRole;
}

type LogoutReason = 'manual' | 'expired' | 'unknown' | null;

export const useAuthStore = defineStore('auth', () => {
  const userInfo = ref<UserInfo | null>(null);
  const isAuthenticated = ref(false);
  const isAuthLoading = ref(false);
  const isInitialized = ref(false);
  const isPostLogoutRedirect = ref(false);
  const lastLogoutReason = ref<LogoutReason>(null);

  async function fetchUserInfo() {
    if (isPostLogoutRedirect.value) {
      isAuthLoading.value = false;
      return;
    }

    isAuthLoading.value = true;

    try {
      const response = await api.get<UserInfo>('/User/me');
      userInfo.value = response.data;
      isAuthenticated.value = true;
    } catch (error: any) {
      if (error?.response?.status === 401 || error?.__silenced401) {
        userInfo.value = null;
        isAuthenticated.value = false;
      } else {
        throw error;
      }
    } finally {
      isAuthLoading.value = false;
    }
  }

  async function initialize() {
    if (isInitialized.value) return;

    isInitialized.value = true;
    await fetchUserInfo();
  }

  async function login(username: string, password: string) {
    isAuthLoading.value = true;

    try {
      await api.post('/Auth/login', { username, password });
      await fetchUserInfo();

      const redirect = router.currentRoute.value.query.redirect as string;
      await router.replace(redirect || '/');
    } finally {
      isAuthLoading.value = false;
    }
  }

  async function logout() {
    try {
      await api.post('/Auth/logout');
    } catch (error) {
      // Ignore errors
    }

    forceLogout('manual');
  }

  function forceLogout(reason: 'manual' | 'expired' | 'unknown') {
    userInfo.value = null;
    isAuthenticated.value = false;
    isAuthLoading.value = false;
    isPostLogoutRedirect.value = true;
    isInitialized.value = false;
    lastLogoutReason.value = reason;

    const query = reason ? { reason } : undefined;
    router.replace({ name: 'login', query }).then(() => {
      isPostLogoutRedirect.value = false;
    });
  }

  return {
    userInfo,
    isAuthenticated,
    isAuthLoading,
    isInitialized,
    isPostLogoutRedirect,
    lastLogoutReason,
    initialize,
    fetchUserInfo,
    login,
    logout,
    forceLogout
  };
});
