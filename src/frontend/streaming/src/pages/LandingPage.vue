<script setup lang="ts">
import { useRouter } from 'vue-router'

const router = useRouter()

const navigateToHome = () => {
  router.push('/home')
}
</script>

<template>
  <div class="min-h-screen flex flex-col items-center justify-center bg-gray-900 text-white p-6">
    <div class="max-w-2xl text-center space-y-6">
      <h1 class="text-5xl font-bold text-blue-500">CuriosityHub</h1>
      <p class="text-xl text-gray-300">
        Discover, share, and enjoy amazing videos around the world.
        Your favorite content, all in one place.
      </p>
      
      <div class="pt-8">
        <button 
          @click="navigateToHome"
          class="px-8 py-3 bg-blue-600 hover:bg-blue-700 text-white rounded-lg font-semibold transition-colors duration-200"
        >
          Explore Videos
        </button>
      </div>
    </div>
  </div>
</template>
