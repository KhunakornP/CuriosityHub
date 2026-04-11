<script setup lang="ts">
import { defineProps } from 'vue'

export interface Video {
  id: string
  title: string
  thumbnailUrl: string
  channelName: string
  views: number
  publishedAt: string
}

const props = defineProps<{
  video: Video
}>()
</script>

<template>
  <div class="cursor-pointer group flex flex-col gap-2">
    <!-- Thumbnail -->
    <div class="aspect-video w-full bg-gray-800 rounded-xl overflow-hidden relative">
      <img 
        v-if="video.thumbnailUrl" 
        :src="video.thumbnailUrl" 
        :alt="video.title"
        class="w-full h-full object-cover group-hover:scale-105 transition-transform duration-200"
      />
      <div v-else class="w-full h-full flex items-center justify-center text-gray-500">
        No Thumbnail
      </div>
    </div>
    
    <!-- Video Info -->
    <div class="flex gap-3 mt-2 pr-2">
      <!-- Channel Avatar Placeholder -->
      <div class="w-9 h-9 rounded-full bg-gray-700 flex-shrink-0"></div>
      
      <div class="flex flex-col overflow-hidden">
        <h3 class="text-sm font-semibold text-white line-clamp-2 leading-tight group-hover:text-blue-400 transition-colors">
          {{ video.title }}
        </h3>
        <p class="text-xs text-gray-400 mt-1 hover:text-gray-300">
          {{ video.channelName }}
        </p>
        <div class="text-xs text-gray-400 flex items-center gap-1">
          <span>{{ video.views.toLocaleString() }} views</span>
          <span class="w-1 h-1 rounded-full bg-gray-500"></span>
          <span>{{ video.publishedAt }}</span>
        </div>
      </div>
    </div>
  </div>
</template>
