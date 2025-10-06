<template>
  <div class="home-view">
    <div class="header">
      <h1>Defects</h1>
      <button
        v-if="canCreateDefect"
        @click="showCreateModal = true"
        class="btn-primary"
      >
        Create Defect
      </button>
    </div>

    <Banner
      v-if="errorMessage"
      :message="errorMessage"
      type="error"
      @close="errorMessage = ''"
    />

    <Banner
      v-if="successMessage"
      :message="successMessage"
      type="success"
      @close="successMessage = ''"
    />

    <div v-if="loading" class="loading">Loading defects...</div>

    <div v-else-if="defects.length === 0" class="empty-state">
      No defects found.
    </div>

    <div v-else class="defects-list">
      <div
        v-for="defect in defects"
        :key="defect.id"
        class="defect-card"
        @click="goToDefect(defect.id)"
      >
        <div class="defect-header">
          <h3>{{ defect.title }}</h3>
          <span :class="['priority-badge', `priority-${defect.priority.toLowerCase()}`]">
            {{ defect.priority }}
          </span>
        </div>
        <p class="defect-description">{{ defect.description }}</p>
        <div class="defect-footer">
          <span class="defect-status">{{ defect.status || 'Новая' }}</span>
          <span class="defect-assignee">
            {{ defect.assignee ? defect.assignee.username : 'Unassigned' }}
          </span>
          <span class="defect-date">{{ formatDate(defect.createdAt) }}</span>
        </div>
      </div>
    </div>

    <div v-if="showCreateModal" class="modal-overlay" @click.self="showCreateModal = false">
      <div class="modal">
        <h2>Create Defect</h2>

        <Banner
          v-if="createError"
          :message="createError"
          type="error"
          @close="createError = ''"
        />

        <form @submit.prevent="handleCreateDefect">
          <div class="form-group">
            <label for="title">Title</label>
            <input
              id="title"
              v-model="newDefect.title"
              type="text"
              required
              :disabled="creating"
            />
          </div>

          <div class="form-group">
            <label for="description">Description</label>
            <textarea
              id="description"
              v-model="newDefect.description"
              rows="4"
              required
              :disabled="creating"
            ></textarea>
          </div>

          <div class="form-group">
            <label for="priority">Priority</label>
            <select
              id="priority"
              v-model="newDefect.priority"
              required
              :disabled="creating"
            >
              <option value="Low">Low</option>
              <option value="Medium">Medium</option>
              <option value="High">High</option>
            </select>
          </div>

          <div class="modal-actions">
            <button
              type="button"
              @click="showCreateModal = false"
              class="btn-secondary"
              :disabled="creating"
            >
              Cancel
            </button>
            <button
              type="submit"
              class="btn-primary"
              :disabled="creating"
            >
              {{ creating ? 'Creating...' : 'Create' }}
            </button>
          </div>
        </form>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, computed, onMounted } from 'vue';
import { useRouter } from 'vue-router';
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
  createdAt: string;
}

const router = useRouter();
const authStore = useAuthStore();

const defects = ref<Defect[]>([]);
const loading = ref(false);
const errorMessage = ref('');
const successMessage = ref('');
const showCreateModal = ref(false);
const creating = ref(false);
const createError = ref('');

const newDefect = ref({
  title: '',
  description: '',
  priority: 'Medium'
});

const canCreateDefect = computed(() => {
  const role = authStore.userInfo?.role;
  return role === 'Engineer' || role === 'Manager';
});

onMounted(async () => {
  await fetchDefects();
});

async function fetchDefects() {
  loading.value = true;
  errorMessage.value = '';

  try {
    const response = await api.get<Defect[]>('/Defect');
    defects.value = response.data;
  } catch (error: any) {
    errorMessage.value = 'Failed to load defects. Please try again.';
  } finally {
    loading.value = false;
  }
}

async function handleCreateDefect() {
  creating.value = true;
  createError.value = '';

  try {
    const response = await api.post<Defect>('/Defect', newDefect.value);
    showCreateModal.value = false;
    router.push(`/defects/${response.data.id}`);
  } catch (error: any) {
    if (error.response?.status === 403) {
      createError.value = "You don't have permission to perform this action.";
    } else {
      createError.value = error.response?.data?.message || 'Failed to create defect. Please try again.';
    }
  } finally {
    creating.value = false;
  }
}

function goToDefect(id: number) {
  router.push(`/defects/${id}`);
}

function formatDate(dateString: string): string {
  const date = new Date(dateString);
  return date.toLocaleDateString() + ' ' + date.toLocaleTimeString();
}
</script>

<style scoped>
.home-view {
  max-width: 1200px;
  margin: 0 auto;
  padding: 24px;
}

.header {
  display: flex;
  justify-content: space-between;
  align-items: center;
  margin-bottom: 24px;
}

h1 {
  margin: 0;
  font-size: 28px;
  color: #333;
}

.loading,
.empty-state {
  text-align: center;
  padding: 40px;
  color: #666;
}

.defects-list {
  display: grid;
  gap: 16px;
}

.defect-card {
  background: white;
  padding: 20px;
  border-radius: 8px;
  box-shadow: 0 2px 4px rgba(0, 0, 0, 0.1);
  cursor: pointer;
  transition: transform 0.2s, box-shadow 0.2s;
}

.defect-card:hover {
  transform: translateY(-2px);
  box-shadow: 0 4px 8px rgba(0, 0, 0, 0.15);
}

.defect-header {
  display: flex;
  justify-content: space-between;
  align-items: flex-start;
  margin-bottom: 12px;
}

.defect-header h3 {
  margin: 0;
  font-size: 18px;
  color: #333;
  flex: 1;
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

.defect-description {
  color: #666;
  margin: 0 0 16px 0;
  line-height: 1.5;
}

.defect-footer {
  display: flex;
  gap: 16px;
  font-size: 14px;
  color: #888;
}

.defect-status {
  font-weight: 500;
  color: #1976d2;
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

.modal-overlay {
  position: fixed;
  top: 0;
  left: 0;
  right: 0;
  bottom: 0;
  background-color: rgba(0, 0, 0, 0.5);
  display: flex;
  justify-content: center;
  align-items: center;
  z-index: 1000;
}

.modal {
  background: white;
  padding: 32px;
  border-radius: 8px;
  max-width: 500px;
  width: 90%;
  max-height: 90vh;
  overflow-y: auto;
}

.modal h2 {
  margin: 0 0 24px 0;
  font-size: 22px;
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

input,
textarea,
select {
  width: 100%;
  padding: 10px 12px;
  border: 1px solid #ddd;
  border-radius: 4px;
  font-size: 14px;
  box-sizing: border-box;
  font-family: inherit;
}

input:focus,
textarea:focus,
select:focus {
  outline: none;
  border-color: #1976d2;
}

input:disabled,
textarea:disabled,
select:disabled {
  background-color: #f5f5f5;
  cursor: not-allowed;
}

.modal-actions {
  display: flex;
  justify-content: flex-end;
  gap: 12px;
  margin-top: 24px;
}
</style>
