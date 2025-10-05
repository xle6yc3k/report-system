// DefectsManagement.Client/src/http/api.ts

import axios from 'axios';
import { useAuthStore } from '@/stores/authStore';

const apiClient = axios.create({
    baseURL: 'https://localhost:7143/api', 
    headers: {
        'Content-Type': 'application/json',
    },
    withCredentials: true, 
});

apiClient.interceptors.request.use(
    (config) => {
        return config;
    },
    (error) => Promise.reject(error)
);

apiClient.interceptors.response.use(
    (response) => response,
    (error) => {
        if (error.response && error.response.status === 401) {
            const authStore = useAuthStore();
            authStore.logout(); 
        }
        return Promise.reject(error);
    }
);

export default apiClient;