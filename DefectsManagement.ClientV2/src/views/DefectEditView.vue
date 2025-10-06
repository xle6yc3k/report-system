<template>
  <div class="defect-edit">
    <div v-if="loading" class="loading">Loading defect...</div>

    <div v-else-if="notFound" class="not-found">
      <h2>Defect not found</h2>
      <p>The defect you're looking for doesn't exist or has been removed.</p>
      <button @click="goBack" class="btn-primary">Go Back</button>
    </div>

    <div v-else-if="defect">
      <div class="header">
        <h1>Edit Defect</h1>
        <button @click="goBack" class="btn-secondary">Cancel</button>
      </div>

      <Banner
        v-if="permissionDenied"
        message="You don't have permission to perform this action."
        type="error"
      />

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

      <div class="form-container">
        <form @submit.prevent="handleUpdate">
          <div class="form-group">
            <label for="title">Title</label>
            <input
              id="title"
              v-model="form.title"
              type="text"
              required
              :disabled="saving || permissionDenied"
            />
          </div>

          <div class="form-group">
            <label for="description">Description</label>
            <textarea
              id="description"
              v-model="form.description"
              rows="6"
              required
              :disabled="saving || permissionDenied"
            ></textarea>
          </div>

          <div class="form-group">
            <label for="priority">Priority</label>
            <select
              id="priority"
              v-model="form.priority"
              required
              :disabled="saving || permissionDenied"
            >
              <option value="Low">Low</option>
              <option value="Medium">Medium</option>
              <option value="High">High</option>
            </select>
          </div>

          <div class="form-group">
            <label for="status">Status</label>
            <input
              id="status"
              v-model="form.status"
              type="text"
              :disabled="saving || permissionDenied"
            />
          </div>

          <div class="form-group">
            <label for="assignee">Assignee</label>
            <select
              id="assignee"
              v-model="form.assigneeId"
              :disabled="saving || permissionDenied"
            >
              <option :value="null">Unassigned</option>
              <option
                v-for="user in users"
                :key="user.id"
                :value="user.id"
              >
                {{ user.username }}
              </option>
            </select>
            <small class="help-text">Select "Unassigned" to clear the assignee</small>
          </div>

          <div class="form-actions">
            <button
              type="button"
              @click="goBack"
              class="btn-secondary"
              :disabled="saving"
            >
              Cancel
            </button>
            <button
              type="submit"
              class="btn-primary"
              :disabled="saving || permissionDenied"
            >
              {{ saving ? 'Saving...' : 'Save Changes' }}
            </button>
          </div>
        </form>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, onMounted } from 'vue';
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
  assigneeId?: number | null;
  createdAt: string;
}

const route = useRoute();
const router = useRouter();
const authStore = useAuthStore();

const defect = ref<Defect | null>(null);
const users = ref<User[]>([]);
const loading = ref(false);
const notFound = ref(false);
const saving = ref(false);
const errorMessage = ref('');
const successMessage = ref('');
const permissionDenied = ref(false);

const form = ref({
  title: '',
  description: '',
  priority: 'Medium',
  status: '',
  assigneeId: null as number | null
});

onMounted(async () => {
  await Promise.all([fetchDefect(), fetchUsers()]);
  checkPermissions();
});

async function fetchDefect() {
  loading.value = true;
  notFound.value = false;
  errorMessage.value = '';

  try {
    const id = route.params.id;
    const response = await api.get<Defect>(`/Defect/${id}`);
    defect.value = response.data;

    form.value = {
      title: response.data.title,
      description: response.data.description,
      priority: response.data.priority,
      status: response.data.status || 'Новая',
      assigneeId: response.data.assigneeId ?? null
    };
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

async function fetchUsers() {
  try {
    const response = await api.get<User[]>('/User');
    users.value = response.data;
  } catch (error) {
    // Ignore error, assignee selection won't be available
  }
}

function checkPermissions() {
  const role = authStore.userInfo?.role;
  const userId = authStore.userInfo?.id;

  if (role === 'Engineer' && defect.value?.assigneeId !== userId) {
    permissionDenied.value = true;
  }
}

async function handleUpdate() {
  if (permissionDenied.value) return;

  saving.value = true;
  errorMessage.value = '';
  successMessage.value = '';

  try {
    const id = route.params.id;
    const updateData: any = { ...form.value };

    if (updateData.assigneeId === null) {
      updateData.assigneeId = null;
    }

    await api.put(`/Defect/${id}`, updateData);
    successMessage.value = 'Defect updated successfully!';

    await fetchDefect();
  } catch (error: any) {
    if (error.response?.status === 403) {
      permissionDenied.value = true;
      errorMessage.value = "You don't have permission to perform this action.";
    } else {
      errorMessage.value = error.response?.data?.message || 'Failed to update defect. Please try again.';
    }
  } finally {
    saving.value = false;
  }
}

function goBack() {
  router.push(`/defects/${route.params.id}`);
}
</script>

<style scoped>
.defect-edit {
  max-width: 800px;
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
  align-items: center;
  margin-bottom: 24px;
}

h1 {
  margin: 0;
  font-size: 28px;
  color: #333;
}

.form-container {
  background: white;
  padding: 32px;
  border-radius: 8px;
  box-shadow: 0 2px 4px rgba(0, 0, 0, 0.1);
}

.form-group {
  margin-bottom: 24px;
}

label {
  display: block;
  margin-bottom: 8px;
  font-weight: 600;
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

.help-text {
  display: block;
  margin-top: 6px;
  font-size: 12px;
  color: #888;
}

.form-actions {
  display: flex;
  justify-content: flex-end;
  gap: 12px;
  margin-top: 32px;
  padding-top: 24px;
  border-top: 1px solid #f0f0f0;
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
