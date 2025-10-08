<script setup lang="ts">
import { onMounted } from 'vue'
import { useDefectStore } from '@/stores/defectStore'
import { useProjectStore } from '@/stores/projectStore'
import type { Guid } from '@/types/models'

const ds = useDefectStore()
const ps = useProjectStore()

onMounted(async () => {
  if (!ps.items.length) await ps.loadAll()
  await ds.loadList()
})
function open(id: string) {
  ds.currentId = id as Guid
  ds.loadOne(id as Guid)
}
</script>

<template>
    <div class="p-3 border rounded">
      <div class="flex items-center justify-between">
        <h3>Defects</h3>
        <button @click="ds.loadList()" :disabled="ds.isListLoading">Reload</button>
      </div>
  
      <div v-if="ds.isListLoading">Loading…</div>
  
      <div v-else-if="ds.list.length === 0" class="opacity-70">
        No defects for the selected project yet.
      </div>
  
      <table v-else class="w-full">
        <thead>
          <tr>
            <th>Created</th>
            <th>Title</th>
            <th>Status</th>
            <th>Priority</th>
            <th>Assignee</th>
            <th>Project</th>
          </tr>
        </thead>
        <tbody>
          <tr v-for="d in ds.list" :key="d.id" @click="open(d.id)" style="cursor:pointer">
            <td>{{ new Date(d.createdAt).toLocaleString() }}</td>
            <td>{{ d.title }}</td>
            <td>{{ d.status }}</td>
            <td>{{ d.priority }}</td>
            <td>{{ d.assignedTo?.username ?? 'Unassigned' }}</td>
            <td>{{ d.projectId }}</td>
          </tr>
        </tbody>
      </table>
    </div>
  </template>  

<style scoped>
table { border-collapse: collapse; }
td, th { border: 1px solid #ddd; padding: 6px; }
h3 { margin: 0 0 6px; }
</style>
