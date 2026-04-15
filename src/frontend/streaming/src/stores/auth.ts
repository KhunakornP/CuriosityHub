import { ref, computed } from 'vue'
import { defineStore } from 'pinia'

export const useAuthStore = defineStore('auth', () => {
  const token = ref<string | null>(localStorage.getItem('token'))
  const userRole = ref<string | null>(localStorage.getItem('userRole'))

  const isAuthenticated = computed(() => !!token.value)

  function setToken(newToken: string, role: string = 'Viewer') {
    token.value = newToken
    userRole.value = role
    localStorage.setItem('token', newToken)
    localStorage.setItem('userRole', role)
  }

  function logout() {
    token.value = null
    userRole.value = null
    localStorage.removeItem('token')
    localStorage.removeItem('userRole')
  }

  return { token, userRole, isAuthenticated, setToken, logout }
})