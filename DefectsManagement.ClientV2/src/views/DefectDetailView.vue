<template>
  <div class="defect-detail">
    <div v-if="loading" class="loading">Loading defect...</div>

    <div v-else-if="notFound" class="not-found">
      <h2>Defect not found</h2>
      <p>The defect you're looking for doesn't exist or has been removed.</p>
      <button @click="goBack" class="btn-primary">Go Back</button>
    </div>

    <div v-else-if="defect">
      <div class="header">
        <h1>{{ defect.title }}</h1>
        <div class="header-actions">
          <button
            v-if="canEdit"
            @click="goToEdit"
            class="btn-primary"
          >
            Edit
          </button>
          <button @click="goBack" class="btn-secondary">Back</button>
        </div>
      </div>

      <Banner
        v-if="errorMessage"
        :message="errorMessage"
        type="error"
        @close="errorMessage = ''"
      />

      <div class="defect-content">
        <div class="info-row">
          <span class="info-label">Priority:</span>
          <span :class="['priority-badge', `priority-${defect.priority.toLowerCase()}`]">
            {{ defect.priority }}
          </span>
        </div>

        <div class="info-row">
          <span class="info-label">Status:</span>
          <span class="info-value">{{ defect.status || 'Новая' }}</span>
        </div>

        <div class="info-row">
          <span class="info-label">Assignee:</span>
          <span class="info-value">
            {{ defect.assignee ? defect.assignee.username : 'Unassigned' }}
          </span>
        </div>

        <div class="info-row">
          <span class="info-label">Created:</span>
          <span class="info-value">{{ formatDate(defect.createdAt) }}</span>
        </div>

        <div class="description-section">
          <h3>Description</h3>
          <p>{{ defect.description }}</p>
        </div>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, computed, onMounted } from 'vue';
import { useRoute, useRouter } from 'vue-router';
import { useAuthStore } from '../stores/authStore';
import api from '../http/api';
import Banner from '../components/Banner.vue';

interface User {
  id: number;
  username: string;
}

interface Defect {
  id: number;
  title: string;
  description: string;
  priority: string;
  status: string;
  assignee?: User;
  assigneeId?: number;
  createdAt: string;
}

const route = useRoute();
const router = useRouter();
const authStore = useAuthStore();

const defect = ref<Defect | null>(null);
const loading = ref(false);
const notFound = ref(false);
const errorMessage = ref('');

const canEdit = computed(() => {
  const role = authStore.userInfo?.role;
  return role === 'Engineer' || role === 'Manager';
});

onMounted(async () => {
  await fetchDefect();
});

async function fetchDefect() {
  loading.value = true;
  notFound.value = false;
  errorMessage.value = '';

  try {
    const id = route.params.id;
    const response = await api.get<Defect>(`/Defect/${id}`);
    defect.value = response.data;
  } catch (error: any) {
    if (error.response?.status === 404) {
      notFound.value = true;
    } else {
      errorMessage.value = 'Failed to load defect. Please try again.';
    }
  } finally {
    loading.value = false;
  }
}

function goToEdit() {
  router.push(`/defects/${route.params.id}/edit`);
}

function goBack() {
  router.push('/');
}

function formatDate(dateString: string): string {
  const date = new Date(dateString);
  return date.toLocaleDateString() + ' ' + date.toLocaleTimeString();
}
</script>

<style scoped>
.defect-detail {
  max-width: 900px;
  margin: 0 auto;
  padding: 24px;
}

.loading,
.not-found {
  text-align: center;
  padding: 40px;
}

.not-found h2 {
  color: #333;
  margin-bottom: 16px;
}

.not-found p {
  color: #666;
  margin-bottom: 24px;
}

.header {
  display: flex;
  justify-content: space-between;
  align-items: flex-start;
  margin-bottom: 24px;
  gap: 16px;
}

h1 {
  margin: 0;
  font-size: 28px;
  color: #333;
  flex: 1;
}

.header-actions {
  display: flex;
  gap: 12px;
}

.defect-content {
  background: white;
  padding: 24px;
  border-radius: 8px;
  box-shadow: 0 2px 4px rgba(0, 0, 0, 0.1);
}

.info-row {
  display: flex;
  align-items: center;
  padding: 12px 0;
  border-bottom: 1px solid #f0f0f0;
}

.info-row:last-of-type {
  border-bottom: none;
}

.info-label {
  font-weight: 600;
  color: #555;
  width: 120px;
}

.info-value {
  color: #333;
}

.priority-badge {
  padding: 4px 12px;
  border-radius: 12px;
  font-size: 12px;
  font-weight: 600;
  text-transform: uppercase;
}

.priority-low {
  background-color: #e8f5e9;
  color: #2e7d32;
}

.priority-medium {
  background-color: #fff3e0;
  color: #ef6c00;
}

.priority-high {
  background-color: #ffebee;
  color: #c62828;
}

.description-section {
  margin-top: 24px;
  padding-top: 24px;
  border-top: 1px solid #f0f0f0;
}

.description-section h3 {
  margin: 0 0 12px 0;
  font-size: 18px;
  color: #333;
}

.description-section p {
  margin: 0;
  line-height: 1.6;
  color: #666;
  white-space: pre-wrap;
}

.btn-primary {
  padding: 10px 20px;
  background-color: #1976d2;
  color: white;
  border: none;
  border-radius: 4px;
  font-size: 14px;
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

.btn-secondary {
  padding: 10px 20px;
  background-color: #f5f5f5;
  color: #333;
  border: 1px solid #ddd;
  border-radius: 4px;
  font-size: 14px;
  font-weight: 500;
  cursor: pointer;
  transition: background-color 0.2s;
}

.btn-secondary:hover:not(:disabled) {
  background-color: #e0e0e0;
}

.btn-secondary:disabled {
  opacity: 0.6;
  cursor: not-allowed;
}
</style>
