<script setup lang="ts">
import { useFetch } from '@vueuse/core'
import { ref, computed } from 'vue'
import VideoCard, { type Video } from '../components/VideoCard.vue'
import { useRouter } from 'vue-router'

const router = useRouter()

// Template API Call setup using useFetch
// Replace 'https://api.curiosityhub.com/videos/recent' with your actual API endpoint
const { data, isFetching, error } = useFetch('https://api.curiosityhub.com/videos/recent', {
  immediate: false, // Set to true when the API is ready
}).json<{ videos: Video[] }>()

// Placeholder data for demonstration
const mockVideos = ref<Video[]>([
  {
    id: "1",
    title: "Exploring the Deep Ocean: Unseen Wonders",
    thumbnailUrl: "",
    channelName: "CuriosityHub Science",
    views: 1250000,
    publishedAt: "2 days ago"
  },
  {
    id: "2",
    title: "The Future of Artificial Intelligence Explained",
    thumbnailUrl: "",
    channelName: "Tech Insider",
    views: 890000,
    publishedAt: "1 week ago"
  },
  {
    id: "3",
    title: "Culinary Adventures: Street Food in Tokyo",
    thumbnailUrl: "",
    channelName: "Foodie Traveler",
    views: 560000,
    publishedAt: "3 weeks ago"
  },
  {
    id: "4",
    title: "Building a Simple Web App with Vue.js",
    thumbnailUrl: "",
    channelName: "Code Academy",
    views: 120000,
    publishedAt: "1 day ago"
  },
  {
    id: "5",
    title: "Space Exploration: Next Steps for Humanity",
    thumbnailUrl: "",
    channelName: "Astro Science",
    views: 2300000,
    publishedAt: "1 month ago"
  },
  {
    id: "6",
    title: "Mastering TypeScript in 10 Minutes",
    thumbnailUrl: "",
    channelName: "Frontend Master",
    views: 45000,
    publishedAt: "5 hours ago"
  }
])

// Computed property to use fetched data if available, otherwise mock data
const videos = computed(() => data.value?.videos || mockVideos.value)

const navigateToVideo = (id: string) => {
  router.push(`/watch/${id}`)
}
</script>

<template>
  <div class="min-h-screen bg-neutral-900 text-white p-6">
    <header class="flex justify-between items-center mb-8 pb-4 border-b border-gray-800 pt-2">
      <h2 class="text-xl font-semibold text-white">
        Recent Videos
      </h2>
      <nav>
        <!-- Categories/Tabs Template -->
        <ul class="flex gap-4">
          <li class="px-3 py-1 bg-white text-black font-medium rounded-full cursor-pointer text-sm">All</li>
          <li class="px-3 py-1 bg-neutral-800 hover:bg-neutral-700 rounded-full cursor-pointer text-sm transition-colors">Science</li>
          <li class="px-3 py-1 bg-neutral-800 hover:bg-neutral-700 rounded-full cursor-pointer text-sm transition-colors">Technology</li>
          <li class="px-3 py-1 bg-neutral-800 hover:bg-neutral-700 rounded-full cursor-pointer text-sm transition-colors">Education</li>
        </ul>
      </nav>
    </header>

    <main>
      <div v-if="isFetching" class="flex justify-center py-20">
        <div class="animate-spin rounded-full h-12 w-12 border-t-2 border-b-2 border-blue-500"></div>
      </div>
      
      <div v-else-if="error" class="text-red-500 p-4 bg-red-900/20 rounded-lg">
        Error loading videos: {{ error }}
      </div>
      
      <div v-else>
        <div class="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-3 xl:grid-cols-4 2xl:grid-cols-5 gap-4 gap-y-10">
          <VideoCard 
            v-for="video in videos" 
            :key="video.id" 
            :video="video"
            @click="navigateToVideo(video.id)"
          />
        </div>
      </div>
    </main>
  </div>
</template>
