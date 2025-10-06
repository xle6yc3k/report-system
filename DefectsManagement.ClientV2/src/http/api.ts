import axios from 'axios';

let isLoggingOut = false;

const api = axios.create({
  baseURL: import.meta.env.VITE_API_BASE_URL,
  withCredentials: true,
  headers: {
    'Content-Type': 'application/json'
  }
});

api.interceptors.response.use(
  (response) => response,
  async (error) => {
    const url = error.config?.url || '';
    const status = error.response?.status;

    if (status === 401 && !url.endsWith('/Auth/login') && !url.endsWith('/Auth/logout')) {
      if (!isLoggingOut) {
        isLoggingOut = true;

        const { useAuthStore } = await import('../stores/authStore');
        const authStore = useAuthStore();
        authStore.forceLogout('expired');

        setTimeout(() => {
          isLoggingOut = false;
        }, 300);
      }

      return Promise.reject({
        message: 'SESSION_EXPIRED',
        __silenced401: true
      });
    }

    return Promise.reject(error);
  }
);

export default api;
