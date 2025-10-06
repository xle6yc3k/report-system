import axios, { AxiosError } from 'axios';
const apiClient = axios.create({
  baseURL: import.meta.env.VITE_API_BASE_URL || 'https://localhost:7143/api',
  headers: { 'Content-Type': 'application/json' },
  withCredentials: true,
});

let isLoggingOut = false;

apiClient.interceptors.response.use(
  (response) => response,
  async (error: AxiosError) => {
    const status = error.response?.status;
    const cfg: any = error.config || {};
    const url = (cfg?.url || '').toLowerCase();

    if (status === 401) {
      // не триггерим logout на /auth/login и /auth/logout
      const isAuthEndpoint =
        url.endsWith('/auth/login') || url.endsWith('/auth/logout');

      if (!isAuthEndpoint) {
        if (!isLoggingOut) {
          isLoggingOut = true;
          try {
            const { useAuthStore } = await import('@/stores/authStore');
            const store = useAuthStore();
            await store.forceLogout('expired');
          } finally {
            setTimeout(() => (isLoggingOut = false), 300);
          }
        }
        // ВАЖНО: отклоняем (чтобы finally в fetchUserInfo отработал и снял isAuthLoading)
        const err: any = new Error('SESSION_EXPIRED');
        err.__silenced401 = true; // маркер, если захочешь игнорить логирование
        return Promise.reject(err);
      }
    }

    return Promise.reject(error);
  }
);

export default apiClient;
