<script setup lang="ts">
import { computed, onMounted, ref } from 'vue'
import { useDefectStore } from '@/stores/defectStore'
import { useAuthStore } from '@/stores/authStore'
import type { DefectStatus, DefectPriority, Guid } from '@/types/models'

const props = defineProps<{ id: string }>()

const ds = useDefectStore()
const auth = useAuthStore()

const statusVal = ref<DefectStatus>('New')
const priorityVal = ref<DefectPriority>('Medium')
const assignedVal = ref<string>('')
const dueVal = ref<string>('')     // 'YYYY-MM-DD'
const tagsVal = ref<string>('')    // comma separated
const localErr = ref<string | null>(null)

const defect = computed(() => ds.byId.get(props.id as Guid) ?? null)
const loading = computed(() => ds.isLoading(props.id as Guid))

// Разграничение: Engineer может редактировать ТОЛЬКО если он – assignee
const role = computed(() => auth.userInfo?.role ?? 'Observer')
const meId = computed(() => auth.userInfo?.id ?? null)

const canEditByRole = computed(() => {
  if (role.value === 'Manager' || role.value === 'Admin') return true
  if (role.value === 'Engineer') {
    if (!defect.value) return false
    return defect.value.assignedId === meId.value
  }
  return false
})

onMounted(async () => {
  try {
    await ds.loadOne(props.id as Guid)
    const d = defect.value
    if (!d) return
    statusVal.value = d.status
    priorityVal.value = d.priority
    assignedVal.value = d.assignedId ?? ''
    dueVal.value = d.dueDate ?? ''
    tagsVal.value = (d.tags ?? []).join(', ')
  } catch (e: any) {
    localErr.value = e?.message ?? 'Failed to load'
  }
})

async function applyStatus() {
  await ds.setStatus(props.id as Guid, statusVal.value)
}
async function applyPriority() {
  await ds.setPriority(props.id as Guid, priorityVal.value)
}
async function applyAssign() {
  await ds.assign(props.id as Guid, assignedVal.value ? (assignedVal.value as Guid) : null)
}
async function applyDue() {
  await ds.setDueDate(props.id as Guid, dueVal.value || null)
}
async function applyTags() {
  const tags = tagsVal.value ? tagsVal.value.split(',').map(t => t.trim()).filter(Boolean) : []
  await ds.setTags(props.id as Guid, tags)
}
async function removeDefect() {
  await ds.remove(props.id as Guid)
}
</script>

<template>
  <div class="p-3 border rounded grid gap-2">
    <div v-if="localErr" class="text-red-600">{{ localErr }}</div>
    <div v-if="!defect">No defect loaded</div>
    <template v-else>
      <div class="text-sm opacity-70">ID: {{ defect.id }}</div>
      <h3>{{ defect.title }}</h3>
      <div class="text-sm">{{ defect.description }}</div>

      <div v-if="!canEditByRole" class="p-2 bg-yellow-100 border">
        You don’t have permission to edit this defect.
        <div class="text-xs">
          Role: {{ role }}.
          <template v-if="role==='Engineer'">
            You can edit only defects assigned to you.
          </template>
        </div>
      </div>

      <div class="grid gap-2" :class="{ 'opacity-50 pointer-events-none': !canEditByRole || loading }">
        <div class="flex gap-2 items-center">
          <label>Status:</label>
          <select v-model="statusVal">
            <option>New</option>
            <option>InProgress</option>
            <option>InReview</option>
            <option>Closed</option>
            <option>Canceled</option>
          </select>
          <button @click="applyStatus">Apply</button>
        </div>

        <div class="flex gap-2 items-center">
          <label>Priority:</label>
          <select v-model="priorityVal">
            <option>Low</option>
            <option>Medium</option>
            <option>High</option>
            <option>Critical</option>
          </select>
          <button @click="applyPriority">Apply</button>
        </div>

        <div class="flex gap-2 items-center">
          <label>AssignedId:</label>
          <input placeholder="GUID or empty" v-model="assignedVal" style="min-width:280px" />
          <button @click="applyAssign">Apply</button>
          <button @click="assignedVal=''; applyAssign()">Unassign</button>
        </div>

        <div class="flex gap-2 items-center">
          <label>Due date:</label>
          <input type="date" v-model="dueVal" />
          <button @click="applyDue">Apply</button>
        </div>

        <div class="flex gap-2 items-center">
          <label>Tags:</label>
          <input placeholder="comma,separated" v-model="tagsVal" style="min-width:320px" />
          <button @click="applyTags">Apply</button>
        </div>

        <div class="pt-2">
          <button @click="removeDefect">Delete defect</button>
        </div>
      </div>

      <div class="text-xs opacity-60">
        Updated: {{ new Date(defect.updatedAt).toLocaleString() }}
      </div>
    </template>
  </div>
</template>

<style scoped>
input, select, button { padding: 6px; }
h3 { margin: 6px 0; }
</style>
