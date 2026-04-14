<script setup lang="ts">
import { ref, onMounted } from 'vue'
import VideoCard, { type Video } from '../components/VideoCard.vue'
import { useRouter } from 'vue-router'
import { fetchRecentVideos } from '../apis/VideoService'

const router = useRouter()

const isFetching = ref(true)
const error = ref<string | null>(null)
const videos = ref<Video[]>([])

// Placeholder data for demonstration fallback
const mockVideos: Video[] = [
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
  }
]

onMounted(async () => {
  isFetching.value = true
  error.value = null
  try {
    const data = await fetchRecentVideos()
    if (data) {
      videos.value = data
    } else {
      videos.value = []
    }
  } catch (err) {
    error.value = 'Failed to load videos'
    videos.value = []
  } finally {
    isFetching.value = false
  }
})

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
    </header>

    <main>
      <div v-if="isFetching" class="flex justify-center py-20">
        <div class="animate-spin rounded-full h-12 w-12 border-t-2 border-b-2 border-blue-500"></div>
      </div>
      
      <div v-else-if="error" class="text-red-500 p-4 bg-red-900/20 rounded-lg border border-red-500/50">
        Error loading videos: {{ error }}
      </div>
      
      <div v-else-if="videos.length === 0" class="flex flex-col items-center justify-center py-20 text-neutral-400">
        <svg class="w-16 h-16 mb-4 text-neutral-600" fill="none" stroke="currentColor" viewBox="0 0 24 24" xmlns="http://www.w3.org/2000/svg">
          <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M15 10l4.553-2.276A1 1 0 0121 8.618v6.764a1 1 0 01-1.447.894L15 14M5 18h8a2 2 0 002-2V8a2 2 0 00-2-2H5a2 2 0 00-2 2v8a2 2 0 002 2z"></path>
        </svg>
        <h3 class="text-xl font-medium text-white mb-2">No videos found</h3>
        <p class="text-center max-w-md">There are currently no videos available to stream. Check back later or upload a new video!</p>
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
