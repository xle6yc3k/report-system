<script setup lang="ts">
import { onMounted, ref } from 'vue'
import { useProjectStore } from '@/stores/projectStore'
import type { Guid } from '@/types/models'

const ps = useProjectStore()

const showCreate = ref(false)
const keyInput = ref('')
const nameInput = ref('')
const descInput = ref<string | null>('')

onMounted(async () => {
  if (!ps.items.length) await ps.loadAll()
})

function onSelect(id: string) {
  ps.setCurrent(id as Guid)
}
async function createProject() {
  if (!keyInput.value.trim() || !nameInput.value.trim()) return
  const p = await ps.create({ key: keyInput.value.trim(), name: nameInput.value.trim(), description: descInput.value || null })
  ps.setCurrent(p.id)
  keyInput.value = ''
  nameInput.value = ''
  descInput.value = ''
  showCreate.value = false
}
</script>

<template>
  <div class="p-3 border rounded space-y-3">
    <div class="flex items-center gap-3">
      <label>Project:</label>
      <select :value="ps.currentId ?? ''" @change="onSelect(($event.target as HTMLSelectElement).value)">
        <option value="" disabled>Select project…</option>
        <option v-for="p in ps.items" :key="p.id" :value="p.id">{{ p.key }} — {{ p.name }}</option>
      </select>
      <button @click="showCreate = !showCreate">+ New</button>
    </div>

    <div v-if="showCreate" class="grid gap-2 border-t pt-3">
      <input placeholder="KEY (e.g. CORE)" v-model="keyInput" />
      <input placeholder="Name" v-model="nameInput" />
      <input placeholder="Description (optional)" v-model="descInput" />
      <div class="flex gap-2">
        <button @click="createProject">Create</button>
        <button @click="showCreate = false">Cancel</button>
      </div>
    </div>
  </div>
</template>

<style scoped>
select, input, button { padding: 6px; }
</style>
