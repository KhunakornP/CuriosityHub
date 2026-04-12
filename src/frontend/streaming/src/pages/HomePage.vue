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
    if (data && data.length > 0) {
      videos.value = data
    } else {
      // Fallback to mock data if API fails or is empty during dev
      videos.value = mockVideos
    }
  } catch (err) {
    error.value = 'Failed to load videos'
    videos.value = mockVideos
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
