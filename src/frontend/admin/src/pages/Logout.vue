<script setup lang="ts">
import { useRouter } from 'vue-router';

const router = useRouter();

function goLogin() {
  router.push('/login');
}
</script>

<template>
  <div class="flex items-center justify-center min-h-[50vh]">
    <div class="bg-white p-8 rounded-lg shadow-md max-w-sm w-full text-center">
      <div class="text-green-500 mb-4 h-16 w-16 mx-auto">
        <svg xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24" stroke-width="1.5" stroke="currentColor">
          <path stroke-linecap="round" stroke-linejoin="round" d="M9 12.75L11.25 15 15 9.75M21 12c0 1.2-.293 2.333-.81 3.32a9.006 9.006 0 0 1-5.83 5.438A8.995 8.995 0 0 1 12 21a8.995 8.995 0 0 1-2.36-.312 9.006 9.006 0 0 1-5.83-5.438A8.998 8.998 0 0 1 3 12c0-1.2.293-2.333.81-3.32a9.006 9.006 0 0 1 5.83-5.438A8.995 8.995 0 0 1 12 3c.82 0 1.613.11 2.36.312a9.006 9.006 0 0 1 5.83 5.438c.517.987.81 2.12.81 3.32A8.998 8.998 0 0 1 21 12z" />
        </svg>
      </div>
      <h2 class="text-2xl font-bold mb-2 text-gray-800">Logged Out</h2>
      <p class="text-gray-600 mb-6">You have been successfully logged out of the admin system.</p>
      <button @click="goLogin" class="w-full flex justify-center py-2 px-4 border border-transparent rounded-md shadow-sm text-sm font-medium text-white bg-blue-600 hover:bg-blue-700 focus:outline-none focus:ring-2 focus:ring-offset-2 focus:ring-blue-500">
        Back to Login
      </button>
    </div>
  </div>
</template>