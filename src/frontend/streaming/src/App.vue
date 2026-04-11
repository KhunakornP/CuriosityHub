<script setup lang="ts">
import { RouterView } from 'vue-router'
import NavBar from './components/NavBar.vue'
</script>

<template>
  <div class="min-h-screen bg-black text-white flex flex-col">
    <NavBar />
    <div class="flex-grow">
      <RouterView />
    </div>
  </div>
</template>

<style scoped>
</style>
