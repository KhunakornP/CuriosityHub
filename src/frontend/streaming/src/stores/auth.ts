import { ref, computed } from 'vue'
import { defineStore } from 'pinia'

export const useAuthStore = defineStore('auth', () => {
  const token = ref<string | null>(localStorage.getItem('token'))
  const userRole = ref<string | number | null>(localStorage.getItem('userRole'))
  const userEmail = ref<string | null>(localStorage.getItem('userEmail'))
  const userId = ref<string | null>(localStorage.getItem('userId'))

  const isAuthenticated = computed(() => !!token.value)

  function setToken(newToken: string, role: string | number = 0, email: string = '', id: string = '') {
    token.value = newToken
    userRole.value = role
    userEmail.value = email
    userId.value = id
    localStorage.setItem('token', newToken)
    localStorage.setItem('userRole', role.toString())
    if (email) localStorage.setItem('userEmail', email)
    if (id) localStorage.setItem('userId', id)
  }

  function logout() {
    token.value = null
    userRole.value = null
    userEmail.value = null
    userId.value = null
    localStorage.removeItem('token')
    localStorage.removeItem('userRole')
    localStorage.removeItem('userEmail')
    localStorage.removeItem('userId')
  }

  return { token, userRole, userEmail, userId, isAuthenticated, setToken, logout }
})