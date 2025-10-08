// src/stores/projectStore.ts
import { defineStore } from 'pinia'
import { ref, computed } from 'vue'
import { ProjectApi } from '@/http/api'
import type {
  Guid,
  ProjectDto,
  ProjectMemberDto,
  ProjectRole,
} from '@/types/models'

export const useProjectStore = defineStore('projects', () => {
  // --- state
  const items              = ref<ProjectDto[]>([])
  const byId               = ref(new Map<Guid, ProjectDto>())
  const currentId          = ref<Guid | null>(null)

  const isListLoading      = ref(false)
  const loadingById        = ref(new Map<Guid, boolean>())
  const lastError          = ref<string | null>(null)

  const membersByProject   = ref(new Map<Guid, ProjectMemberDto[]>())

  // --- helpers
  const setLoading = (id: Guid, v: boolean) => {
    const m = new Map(loadingById.value)
    m.set(id, v)
    loadingById.value = m
  }

  const upsertProject = (p: ProjectDto) => {
    byId.value.set(p.id, p)
    const idx = items.value.findIndex(x => x.id === p.id)
    if (idx >= 0) items.value[idx] = p
    else items.value = [p, ...items.value]
  }

  const setMembers = (projectId: Guid, members: ProjectMemberDto[]) => {
    const m = new Map(membersByProject.value)
    m.set(projectId, members)
    membersByProject.value = m
  }

  const getMembers = (projectId: Guid) => membersByProject.value.get(projectId) ?? []

  // --- getters
  const current = computed(() => currentId.value ? byId.value.get(currentId.value) ?? null : null)
  const isLoading = (id: Guid) => loadingById.value.get(id) === true
  const hasProjects = computed(() => items.value.length > 0)

  // --- actions
  async function loadAll() {
    isListLoading.value = true
    lastError.value = null
    try {
      const { data } = await ProjectApi.list()
      items.value = data
      byId.value = new Map(data.map(p => [p.id, p]))
      // авто-выбор текущего, если не выбран
      if (!currentId.value && data.length) currentId.value = data[0].id
    } catch (e: any) {
      lastError.value = e?.message ?? 'Failed to load projects'
      throw e
    } finally {
      isListLoading.value = false
    }
  }

  async function loadOne(id: Guid, { force = false } = {}) {
    if (!force && byId.value.has(id)) return byId.value.get(id)!
    setLoading(id, true)
    lastError.value = null
    try {
      const { data } = await ProjectApi.get(id)
      upsertProject(data)
      return data
    } catch (e: any) {
      lastError.value = e?.message ?? 'Failed to load project'
      throw e
    } finally {
      setLoading(id, false)
    }
  }

  function setCurrent(id: Guid | null) {
    currentId.value = id
  }

  async function ensureCurrentSelected() {
    if (!currentId.value) {
      if (!items.value.length) await loadAll()
      if (!currentId.value && items.value.length) currentId.value = items.value[0].id
    }
  }

  async function create(payload: { key: string; name: string; description?: string | null }) {
    lastError.value = null
    const { data } = await ProjectApi.create(payload)
    upsertProject(data)
    currentId.value = data.id
    return data
  }

  async function update(id: Guid, payload: { key?: string; name?: string; description?: string | null }) {
    setLoading(id, true)
    lastError.value = null
    try {
      const { data } = await ProjectApi.update(id, payload)
      upsertProject(data)
      return data
    } catch (e: any) {
      lastError.value = e?.message ?? 'Failed to update project'
      throw e
    } finally {
      setLoading(id, false)
    }
  }

  // --- members
  async function addMembers(projectId: Guid, members: { userId: Guid; role: ProjectRole }[]) {
    setLoading(projectId, true)
    lastError.value = null
    try {
      const { data } = await ProjectApi.addMembers(projectId, members)
      // сервер возвращает массив ProjectMemberDto
      setMembers(projectId, data)
      return data
    } catch (e: any) {
      lastError.value = e?.message ?? 'Failed to add members'
      throw e
    } finally {
      setLoading(projectId, false)
    }
  }

  async function removeMember(projectId: Guid, userId: Guid) {
    setLoading(projectId, true)
    lastError.value = null
    try {
      await ProjectApi.removeMember(projectId, userId)
      // локально обновим кэш
      const cur = getMembers(projectId).filter(m => m.userId !== userId)
      setMembers(projectId, cur)
    } catch (e: any) {
      lastError.value = e?.message ?? 'Failed to remove member'
      throw e
    } finally {
      setLoading(projectId, false)
    }
  }

  // Если позже появится GET участников — добавим тут loadMembers(projectId)

  return {
    // state
    items,
    byId,
    currentId,
    isListLoading,
    loadingById,
    lastError,
    membersByProject,

    // getters
    current,
    isLoading,
    hasProjects,
    getMembers,

    // actions
    loadAll,
    loadOne,
    setCurrent,
    ensureCurrentSelected,
    create,
    update,
    addMembers,
    removeMember,
  }
})
