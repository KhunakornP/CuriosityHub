<script setup lang="ts">
import { RouterLink } from 'vue-router'
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
    </div>
    
    <div class="flex items-center">
      <div 
        class="w-8 h-8 rounded-full bg-gray-700 flex items-center justify-center text-sm font-semibold cursor-not-allowed opacity-50"
        title="Profile (Not implemented yet)"
      >
        P
      </div>
    </div>
  </nav>
</template>
