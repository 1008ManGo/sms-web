import { defineStore } from 'pinia'
import { ref, computed } from 'vue'

export const useUserStore = defineStore('user', () => {
  const apiKey = ref<string>(localStorage.getItem('user_api_key') || '')
  const adminKey = ref<string>(localStorage.getItem('admin_api_key') || '')
  const username = ref<string>(localStorage.getItem('username') || '')
  const userId = ref<string>(localStorage.getItem('user_id') || '')
  const isAdmin = ref<boolean>(localStorage.getItem('is_admin') === 'true')

  const isLoggedIn = computed(() => !!apiKey.value || !!adminKey.value)

  function setUser(data: { apiKey: string; username?: string; userId?: string; isAdmin?: boolean }) {
    apiKey.value = data.apiKey
    if (data.username) username.value = data.username
    if (data.userId) userId.value = data.userId
    if (data.isAdmin) {
      isAdmin.value = true
      adminKey.value = data.apiKey
      localStorage.setItem('admin_api_key', data.apiKey)
      localStorage.setItem('is_admin', 'true')
    } else {
      localStorage.setItem('user_api_key', data.apiKey)
    }
    localStorage.setItem('username', data.username || '')
    localStorage.setItem('user_id', data.userId || '')
  }

  function setAdminKey(key: string) {
    adminKey.value = key
    localStorage.setItem('admin_api_key', key)
    localStorage.setItem('is_admin', 'true')
    isAdmin.value = true
  }

  function logout() {
    apiKey.value = ''
    adminKey.value = ''
    username.value = ''
    userId.value = ''
    isAdmin.value = false
    localStorage.removeItem('user_api_key')
    localStorage.removeItem('admin_api_key')
    localStorage.removeItem('username')
    localStorage.removeItem('user_id')
    localStorage.removeItem('is_admin')
  }

  return {
    apiKey,
    adminKey,
    username,
    userId,
    isAdmin,
    isLoggedIn,
    setUser,
    setAdminKey,
    logout
  }
})
