<script setup lang="ts">
import NavBar from "./components/NavBar.vue"
</script>

<template>
  <div class="min-h-screen bg-gray-100 flex flex-col font-sans">
    <NavBar />
    <main class="flex-grow flex flex-col w-full h-[calc(100vh-64px)] overflow-hidden">
      <router-view />
    </main>
  </div>
</template>