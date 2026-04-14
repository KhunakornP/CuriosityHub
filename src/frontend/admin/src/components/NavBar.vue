<script setup lang="ts">
import { useAuthStore } from '../stores/auth';
import { useRouter } from 'vue-router';

const authStore = useAuthStore();
const router = useRouter();

function logout() {
  authStore.logout();
  router.push('/logout');
}
</script>

<template>
  <nav class="bg-blue-600 text-white shadow-md select-none">
    <div class="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8">
      <div class="flex items-center justify-between h-16">
        <div class="flex items-center">
          <RouterLink to="/admin" class="font-bold text-xl tracking-tight">CuriosityHub Admin</RouterLink>
        </div>
        <div class="flex items-center space-x-4">
          <span v-if="authStore.isAuthenticated" class="text-sm">Welcome, {{ authStore.user?.username }}</span>
          <button v-if="authStore.isAuthenticated" @click="logout" class="bg-blue-500 hover:bg-blue-700 text-white px-3 py-1 rounded transition-colors text-sm font-medium">Logout</button>
        </div>
      </div>
    </div>
  </nav>
</template>