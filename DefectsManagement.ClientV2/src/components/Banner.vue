<template>
  <div v-if="message" :class="['banner', `banner-${type}`]">
    {{ message }}
    <button @click="$emit('close')" class="banner-close">&times;</button>
  </div>
</template>

<script setup lang="ts">
defineProps<{
  message: string;
  type?: 'info' | 'error' | 'success' | 'warning';
}>();

defineEmits<{
  close: [];
}>();
</script>

<style scoped>
.banner {
  padding: 12px 16px;
  border-radius: 4px;
  margin-bottom: 16px;
  display: flex;
  justify-content: space-between;
  align-items: center;
  font-size: 14px;
}

.banner-info {
  background-color: #e3f2fd;
  color: #1976d2;
  border: 1px solid #90caf9;
}

.banner-error {
  background-color: #ffebee;
  color: #c62828;
  border: 1px solid #ef9a9a;
}

.banner-success {
  background-color: #e8f5e9;
  color: #2e7d32;
  border: 1px solid #81c784;
}

.banner-warning {
  background-color: #fff3e0;
  color: #ef6c00;
  border: 1px solid #ffb74d;
}

.banner-close {
  background: none;
  border: none;
  font-size: 20px;
  cursor: pointer;
  padding: 0;
  margin-left: 16px;
  color: inherit;
  opacity: 0.7;
}

.banner-close:hover {
  opacity: 1;
}
</style>
