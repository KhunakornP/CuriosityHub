<script setup lang="ts">
import { ref } from 'vue';
import { useAuthStore } from '../stores/auth';
import { useRouter } from 'vue-router';
import { AuthService } from '../apis/AuthService';

const authStore = useAuthStore();
const router = useRouter();

const username = ref('');
const password = ref('');
const error = ref('');

async function submitLogin() {
  error.value = '';
  try {
    const data = await AuthService.login(username.value, password.value);
    
    // Log the user into the pinia store using the real token
    authStore.login(data.token, data.role || 'Admin');
    router.push('/admin');
  } catch (err: any) {
    error.value = err.message || 'An error occurred during login. Please try again.';
  }
}
</script>

<template>
  <div class="flex items-center justify-center min-h-[50vh]">
    <div class="bg-white p-8 rounded-lg shadow-md max-w-sm w-full">
      <h2 class="text-2xl font-bold mb-6 text-center text-gray-800">Admin Login</h2>
      
      <form @submit.prevent="submitLogin" class="space-y-4">
        <div>
          <label class="block text-sm font-medium text-gray-700">Username / Email</label>
          <input v-model="username" type="text" required class="mt-1 block w-full px-3 py-2 border border-gray-300 rounded-md shadow-sm focus:outline-none focus:ring-blue-500 focus:border-blue-500">
        </div>
        <div>
          <label class="block text-sm font-medium text-gray-700">Password</label>
          <input v-model="password" type="password" required class="mt-1 block w-full px-3 py-2 border border-gray-300 rounded-md shadow-sm focus:outline-none focus:ring-blue-500 focus:border-blue-500">
        </div>
        
        <div v-if="error" class="text-red-500 text-sm">
          {{ error }}
        </div>

        <button type="submit" class="w-full flex justify-center py-2 px-4 border border-transparent rounded-md shadow-sm text-sm font-medium text-white bg-blue-600 hover:bg-blue-700 focus:outline-none focus:ring-2 focus:ring-offset-2 focus:ring-blue-500">
          Login
        </button>
      </form>
    </div>
  </div>
</template>