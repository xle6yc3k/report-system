<script setup lang="ts">
import ProjectPicker from '@/components/ProjectPicker.vue'
import DefectQuickForm from '@/components/DefectQuickForm.vue'
import DefectTable from '@/components/DefectTable.vue'
import DefectActions from '@/components/DefectActions.vue'
import { useDefectStore } from '@/stores/defectStore'

const ds = useDefectStore()
</script>

<template>
  <div class="grid gap-4" style="max-width: 1200px; margin: 0 auto; padding: 16px;">
    <h2>Sandbox: Projects & Defects</h2>

    <ProjectPicker />

    <div class="grid md:grid-cols-2 gap-4">
      <DefectQuickForm />
      <DefectTable />
    </div>

    <div v-if="ds.currentId" class="grid gap-2">
      <h3>Selected Defect</h3>
      <DefectActions :id="ds.currentId" />
    </div>
    <div v-else class="opacity-60">Select a defect in the table to test actions</div>
  </div>
</template>

<style scoped>
h2,h3 { margin: 0; }
</style>
