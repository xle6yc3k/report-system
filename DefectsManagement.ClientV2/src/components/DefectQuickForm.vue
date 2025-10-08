<script setup lang="ts">
import { ref, computed } from 'vue'
import { useDefectStore } from '@/stores/defectStore'
import { useProjectStore } from '@/stores/projectStore'
import type { DefectPriority, Guid } from '@/types/models'

const ds = useDefectStore()
const ps = useProjectStore()

const title = ref('')
const description = ref('')
const priority = ref<DefectPriority>('Medium')
const assignedId = ref<string>('')
const dueDate = ref<string>('') // 'YYYY-MM-DD'
const tags = ref<string>('') // comma-separated

const canCreate = computed(() => !!ps.currentId && title.value.trim() && description.value.trim())

async function submit() {
  if (!ps.currentId) return
  const payload = {
    projectId: ps.currentId as Guid,
    title: title.value.trim(),
    description: description.value.trim(),
    priority: priority.value,
    assignedId: assignedId.value ? (assignedId.value as Guid) : null,
    dueDate: dueDate.value || null,
    tags: tags.value ? tags.value.split(',').map(t => t.trim()).filter(Boolean) : []
  }
  await ds.create(payload) // <-- убрали неиспользуемую переменную

  // очистим форму
  title.value = ''
  description.value = ''
  priority.value = 'Medium'
  assignedId.value = ''
  dueDate.value = ''
  tags.value = ''
}
</script>

<template>
  <div class="p-3 border rounded grid gap-2">
    <h3>Create Defect</h3>
    <div v-if="!ps.currentId" class="text-red-600">Select a project first</div>

    <input placeholder="Title" v-model="title" />
    <textarea placeholder="Description" v-model="description" rows="3"></textarea>

    <div class="flex gap-2 items-center">
      <label>Priority:</label>
      <select v-model="priority">
        <option>Low</option>
        <option>Medium</option>
        <option>High</option>
        <option>Critical</option>
      </select>

      <label>AssignedId:</label>
      <input placeholder="GUID or empty" v-model="assignedId" style="min-width:280px" />

      <label>Due:</label>
      <input type="date" v-model="dueDate" />
    </div>

    <input placeholder="tags: comma,separated" v-model="tags" />

    <button :disabled="!canCreate" @click="submit">Create</button>
  </div>
</template>

<style scoped>
input, textarea, select, button { padding: 6px; }
h3 { margin: 0 0 6px; }
</style>
