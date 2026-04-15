<script setup lang="ts">
import { computed } from 'vue'
import { RouterLink, useRouter } from 'vue-router'
import { useAuthStore } from '../stores/auth'

const authStore = useAuthStore()
const router = useRouter()

const isResearcher = computed(() => authStore.userRole === 'Researcher' || authStore.userRole === 'Admin')

const displayEmail = computed(() => {
  if (!authStore.userEmail) return ''
  return authStore.userEmail.length > 20 
    ? authStore.userEmail.substring(0, 20) + '...' 
    : authStore.userEmail
})

const profileIcon = computed(() => {
  return authStore.userEmail ? authStore.userEmail.charAt(0).toUpperCase() : 'U'
})

const logout = () => {
  authStore.logout()
  router.push('/login')
}
</script>

<template>
  <nav class="bg-gray-950 text-white px-6 py-4 flex justify-between items-center sticky top-0 z-50 border-b border-gray-800">
    <div class="flex items-center gap-8">
      <RouterLink to="/" class="text-2xl font-bold bg-clip-text text-transparent bg-gradient-to-r from-blue-400 to-purple-500 hover:opacity-80 transition-opacity">
        CuriosityHub
      </RouterLink>
      <RouterLink to="/home" class="text-sm font-medium text-gray-300 hover:text-white transition-colors" active-class="text-white">
        Home
      </RouterLink>
      <RouterLink v-if="authStore.isAuthenticated" to="/profile" class="text-sm font-medium text-gray-300 hover:text-white transition-colors" active-class="text-white">
        Profile
      </RouterLink>
    </div>
    
    <div class="flex items-center gap-6">
      <span v-if="authStore.isAuthenticated && authStore.userEmail" class="text-sm font-medium text-gray-300 mr-2">
        Welcome, {{ displayEmail }}
      </span>

      <RouterLink 
        v-if="authStore.isAuthenticated && isResearcher"
        to="/upload" 
        class="bg-blue-600 hover:bg-blue-700 text-white text-sm font-medium py-1.5 px-4 rounded-full transition-colors flex items-center gap-2"
      >
        <span class="text-lg leading-none mb-0.5">+</span> Upload
      </RouterLink>

      <template v-if="authStore.isAuthenticated">
        <button @click="logout" class="text-sm font-medium text-gray-300 hover:text-white transition-colors">
          Logout
        </button>
        <RouterLink 
          to="/profile"
          class="w-8 h-8 rounded-full bg-indigo-600 flex items-center justify-center text-sm font-semibold hover:bg-indigo-500 transition-colors cursor-pointer"
          :title="authStore.userEmail || 'Profile'"
        >
          {{ profileIcon }}
        </RouterLink>
      </template>
      <template v-else>
        <RouterLink to="/login" class="text-sm font-medium text-gray-300 hover:text-white transition-colors">
          Login
        </RouterLink>
        <RouterLink to="/register" class="bg-indigo-600 hover:bg-indigo-700 text-white text-sm font-medium py-1.5 px-4 rounded-full transition-colors">
          Sign Up
        </RouterLink>
      </template>
    </div>
  </nav>
</template>
