// src/stores/defectStore.ts
import { defineStore } from 'pinia'
import { ref, computed } from 'vue'
import { DefectApi } from '@/http/api'
import type {
  DefectDto,
  Guid,
  DefectPriority,
  DefectStatus,
} from '@/types/models'
import { useProjectStore } from '@/stores/projectStore'
import { useAuthStore } from '@/stores/authStore'

export const useDefectStore = defineStore('defects', () => {
  // --- state
  const items = ref<DefectDto[]>([])
  const byId = ref(new Map<Guid, DefectDto>())
  const currentId = ref<Guid | null>(null)
  const isListLoading = ref(false)
  const loadingById = ref(new Map<Guid, boolean>())
  const lastError = ref<string | null>(null)
  const lastForbidden = ref(false)

  // --- helpers
  const setDefect = (d: DefectDto) => {
    byId.value.set(d.id, d)
    const idx = items.value.findIndex(x => x.id === d.id)
    if (idx >= 0) items.value[idx] = d
  }
  const setLoading = (id: Guid, v: boolean) => {
    const m = new Map(loadingById.value)
    m.set(id, v)
    loadingById.value = m
  }

  // --- getters
  const projectStore = useProjectStore()
  const authStore = useAuthStore()

  const current = computed(() => currentId.value ? byId.value.get(currentId.value) || null : null)

  const list = computed(() => {
    const pid = projectStore.currentId
    if (!pid) return items.value
    return items.value.filter(x => x.projectId === pid)
  })

  const myAssigned = computed(() => {
    const me = authStore.userInfo?.id
    if (!me) return []
    return list.value.filter(x => x.assignedId === me)
  })

  const isLoading = (id: Guid) => loadingById.value.get(id) === true

  // --- actions
  async function loadList() {
    isListLoading.value = true
    lastError.value = null
    lastForbidden.value = false
    try {
      const { data } = await DefectApi.list()
      items.value = data
      for (const d of data) {
        const prev = byId.value.get(d.id)
        if (!prev) byId.value.set(d.id, d)
      }
    } catch (e: any) {
      lastError.value = e?.message ?? 'Failed to load defects'
      lastForbidden.value = e?.response?.status === 403
      throw e
    } finally {
      isListLoading.value = false
    }
  }

  async function loadOne(id: Guid, { force = false } = {}) {
    if (!force && byId.value.has(id)) return byId.value.get(id)!
    setLoading(id, true)
    lastError.value = null
    lastForbidden.value = false
    try {
      const { data } = await DefectApi.get(id)
      setDefect(data)
      return data
    } catch (e: any) {
      lastError.value = e?.message ?? 'Failed to load defect'
      lastForbidden.value = e?.response?.status === 403
      throw e
    } finally {
      setLoading(id, false)
    }
  }

  async function create(payload: {
    projectId: Guid
    title: string
    description: string
    priority: DefectPriority
    assignedId?: Guid | null
    dueDate?: string | null
    tags?: string[]
  }) {
    lastError.value = null
    lastForbidden.value = false
    const { data } = await DefectApi.create(payload)
    items.value = [data, ...items.value]
    setDefect(data)
    currentId.value = data.id
    return data
  }

  async function update(id: Guid, payload: {
    title?: string
    description?: string
    priority?: DefectPriority
    status?: DefectStatus
    assignedId?: Guid | null
    dueDate?: string | null
    tags?: string[]
  }) {
    setLoading(id, true)
    lastError.value = null
    lastForbidden.value = false
    try {
      const { data } = await DefectApi.update(id, payload)
      setDefect(data)
      return data
    } catch (e: any) {
      lastError.value = e?.message ?? 'Failed to update defect'
      lastForbidden.value = e?.response?.status === 403
      throw e
    } finally {
      setLoading(id, false)
    }
  }

  async function remove(id: Guid) {
    setLoading(id, true)
    lastError.value = null
    lastForbidden.value = false
    try {
      await DefectApi.delete(id)
      byId.value.delete(id)
      items.value = items.value.filter(x => x.id !== id)
      if (currentId.value === id) currentId.value = null
    } catch (e: any) {
      lastError.value = e?.message ?? 'Failed to delete defect'
      lastForbidden.value = e?.response?.status === 403
      throw e
    } finally {
      setLoading(id, false)
    }
  }

  // --- partial updates
  async function assign(id: Guid, assignedId: Guid | null) {
    setLoading(id, true)
    lastError.value = null
    lastForbidden.value = false
    try {
      const { data } = await DefectApi.assign(id, assignedId)
      setDefect(data)
      return data
    } catch (e: any) {
      lastError.value = e?.message ?? 'Failed to assign defect'
      lastForbidden.value = e?.response?.status === 403
      throw e
    } finally {
      setLoading(id, false)
    }
  }

  async function setStatus(id: Guid, status: DefectStatus) {
    setLoading(id, true)
    lastError.value = null
    lastForbidden.value = false
    try {
      const { data } = await DefectApi.setStatus(id, status)
      setDefect(data)
      return data
    } catch (e: any) {
      lastError.value = e?.message ?? 'Failed to change status'
      lastForbidden.value = e?.response?.status === 403
      throw e
    } finally {
      setLoading(id, false)
    }
  }

  async function setPriority(id: Guid, priority: DefectPriority) {
    setLoading(id, true)
    lastError.value = null
    lastForbidden.value = false
    try {
      const { data } = await DefectApi.setPriority(id, priority)
      setDefect(data)
      return data
    } catch (e: any) {
      lastError.value = e?.message ?? 'Failed to change priority'
      lastForbidden.value = e?.response?.status === 403
      throw e
    } finally {
      setLoading(id, false)
    }
  }

  async function setDueDate(id: Guid, dueDate: string | null) {
    setLoading(id, true)
    lastError.value = null
    lastForbidden.value = false
    try {
      const { data } = await DefectApi.setDueDate(id, dueDate)
      setDefect(data)
      return data
    } catch (e: any) {
      lastError.value = e?.message ?? 'Failed to change due date'
      lastForbidden.value = e?.response?.status === 403
      throw e
    } finally {
      setLoading(id, false)
    }
  }

  async function setTags(id: Guid, tags: string[]) {
    setLoading(id, true)
    lastError.value = null
    lastForbidden.value = false
    try {
      const { data } = await DefectApi.setTags(id, tags)
      setDefect(data)
      return data
    } catch (e: any) {
      lastError.value = e?.message ?? 'Failed to update tags'
      lastForbidden.value = e?.response?.status === 403
      throw e
    } finally {
      setLoading(id, false)
    }
  }

  async function refresh(id: Guid) {
    return loadOne(id, { force: true })
  }

  return {
    // state
    items,
    byId,
    currentId,
    isListLoading,
    loadingById,
    lastError,
    lastForbidden,

    // getters
    current,
    list,
    myAssigned,
    isLoading,

    // actions
    loadList,
    loadOne,
    refresh,
    create,
    update,
    remove,
    assign,
    setStatus,
    setPriority,
    setDueDate,
    setTags,
  }
})
