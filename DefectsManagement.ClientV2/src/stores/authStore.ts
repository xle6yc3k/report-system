import { defineStore } from 'pinia'
import { ref } from 'vue'
import router from '@/router'
import { AuthApi, UserApi } from '@/http/api'
import type { CurrentUserDto } from '@/types/models'

type LogoutReason = 'manual' | 'expired' | 'unknown'

export const useAuthStore = defineStore('auth', () => {
  // ---- state
  const userInfo = ref<CurrentUserDto | null>(null)
  const isAuthenticated = ref(false)
  const isAuthLoading = ref(false)
  const isInitialized = ref(false)
  const isPostLogoutRedirect = ref(false)
  const lastLogoutReason = ref<LogoutReason | null>(null)

  // ---- actions
  async function fetchUserInfo() {
    if (isPostLogoutRedirect.value) {
      // после принудительного логаута не дергаем /User/me
      isAuthLoading.value = false
      return
    }

    isAuthLoading.value = true
    try {
      const { data } = await UserApi.me()
      // data: { id: Guid(string), username: string, name: string, role: 'Engineer'|'Manager'|'Observer'|'Admin' }
      userInfo.value = data
      isAuthenticated.value = true
    } catch (error: any) {
      // 401 от сервера либо «приглушенный» 401 из интерцептора
      if (error?.response?.status === 401 || error?.__silenced401) {
        userInfo.value = null
        isAuthenticated.value = false
      } else {
        // любые прочие ошибки пробрасываем наверх
        throw error
      }
    } finally {
      isAuthLoading.value = false
    }
  }

  async function initialize() {
    if (isInitialized.value) return
    isInitialized.value = true
    await fetchUserInfo()
  }

  async function login(username: string, password: string) {
    isAuthLoading.value = true
    try {
      await AuthApi.login({ username, password })
      await fetchUserInfo()
      const redirect = (router.currentRoute.value.query.redirect as string) || '/'
      await router.replace(redirect)
    } finally {
      isAuthLoading.value = false
    }
  }

  async function logout() {
    try {
      await AuthApi.logout()
    } catch {
      // ignore network/500 при логауте
    }
    forceLogout('manual')
  }

  function forceLogout(reason: LogoutReason) {
    userInfo.value = null
    isAuthenticated.value = false
    isAuthLoading.value = false
    isInitialized.value = false
    isPostLogoutRedirect.value = true
    lastLogoutReason.value = reason

    const query = reason ? { reason } : undefined
    router.replace({ name: 'login', query }).then(() => {
      // флаг снимаем после навигации, чтобы beforeEach не зациклился
      isPostLogoutRedirect.value = false
    })
  }

  return {
    // state
    userInfo,
    isAuthenticated,
    isAuthLoading,
    isInitialized,
    isPostLogoutRedirect,
    lastLogoutReason,
    // actions
    initialize,
    fetchUserInfo,
    login,
    logout,
    forceLogout,
  }
})
