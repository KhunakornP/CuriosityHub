import { ref, computed } from 'vue'
import { defineStore } from 'pinia'

export const useAuthStore = defineStore('auth', () => {
  const token = ref<string | null>(localStorage.getItem('token'))
  const userRole = ref<string | null>(localStorage.getItem('userRole'))
  const userEmail = ref<string | null>(localStorage.getItem('userEmail'))

  const isAuthenticated = computed(() => !!token.value)

  function setToken(newToken: string, role: string = 'Viewer', email: string = '') {
    token.value = newToken
    userRole.value = role
    userEmail.value = email
    localStorage.setItem('token', newToken)
    localStorage.setItem('userRole', role)
    if (email) localStorage.setItem('userEmail', email)
  }

  function logout() {
    token.value = null
    userRole.value = null
    userEmail.value = null
    localStorage.removeItem('token')
    localStorage.removeItem('userRole')
    localStorage.removeItem('userEmail')
  }

  return { token, userRole, userEmail, isAuthenticated, setToken, logout }
})