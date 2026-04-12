<script setup lang="ts">
import { useRoute, useRouter } from 'vue-router'
import { ref, onMounted, onUnmounted } from 'vue'
import { fetchVideoDetails, fetchVideoStreamBlobUrl } from '../apis/VideoService'

const route = useRoute()
const router = useRouter()

const videoId = route.params.id as string

const isFetchingVideo = ref(true)
const videoData = ref<any>(null)
const videoUrl = ref<string | null>(null)
const videoError = ref<string | null>(null)

// For cleanup of Object URL to prevent memory leaks
onUnmounted(() => {
  if (videoUrl.value) {
    URL.revokeObjectURL(videoUrl.value)
  }
})

onMounted(async () => {
  isFetchingVideo.value = true
  try {
    const [details, streamUrl] = await Promise.all([
      fetchVideoDetails(videoId),
      fetchVideoStreamBlobUrl(videoId)
    ])
    
    if (details) videoData.value = details
    if (streamUrl) videoUrl.value = streamUrl
    
  } catch (err) {
    videoError.value = 'Failed to load video details'
    console.error(err)
  } finally {
    isFetchingVideo.value = false
  }
})

const navigateToHome = () => {
  router.push('/home')
}
</script>

<template>
  <div class="min-h-screen bg-black text-white p-6 pt-4">
    <header class="mb-4 flex justify-end items-center">
      <button class="px-4 py-2 text-sm bg-neutral-800 rounded-full hover:bg-neutral-700 transition" @click="navigateToHome">
        Back to Home
      </button>
    </header>

    <div class="flex flex-col lg:flex-row gap-6">
      <!-- Main Video Area -->
      <div class="lg:w-3/4 flex-grow">
        <div class="aspect-video bg-neutral-900 rounded-xl flex items-center justify-center border border-neutral-800 shadow-lg overflow-hidden">
          <div v-if="isFetchingVideo" class="animate-spin rounded-full h-8 w-8 border-t-2 border-b-2 border-blue-500"></div>
          <video v-else-if="videoUrl" :src="videoUrl" controls autoplay class="w-full h-full object-cover">
            Your browser does not support the video tag.
          </video>
          <p v-else class="text-neutral-500">Video not found or failed to load (ID: {{ videoId }})</p>
        </div>
        
        <div class="mt-4">
          <h2 class="text-2xl font-semibold mb-2" v-if="!isFetchingVideo">
            {{ videoData?.metadata?.title || 'Video Title Placeholder' }}
          </h2>
          <div class="flex items-center justify-between mb-4 border-b border-neutral-800 pb-4">
            <div class="flex items-center gap-4 text-sm text-neutral-400">
              <span class="text-white hover:text-blue-400 transition-colors cursor-pointer">
                {{ videoData?.metadata?.channelName || 'Unknown Channel' }}
              </span>
              <span>{{ videoData?.views?.count || '0' }} views</span>
              <span>{{ videoData?.metadata?.publishedAt || 'Unknown Date' }}</span>
            </div>
            <div class="flex gap-2">
              <button class="px-4 py-2 bg-neutral-800 hover:bg-neutral-700 rounded-full flex items-center gap-2 transition-colors">
                <span class="text-sm">Like</span>
              </button>
              <button class="px-4 py-2 bg-neutral-800 hover:bg-neutral-700 rounded-full flex items-center gap-2 transition-colors">
                <span class="text-sm">Share</span>
              </button>
            </div>
          </div>
          <div class="bg-neutral-900 p-4 rounded-xl text-sm leading-relaxed text-neutral-300">
            {{ videoData?.metadata?.description || 'Description placeholder. This is where the video summary and links go.' }}
          </div>
          
          <!-- Comment Section -->
          <div class="mt-8 border-t border-neutral-800 pt-6">
            <h3 class="text-xl font-semibold mb-4">Comments Placeholder</h3>
            
            <div class="flex gap-4 mb-8">
              <div class="w-10 h-10 rounded-full bg-neutral-700 flex-shrink-0"></div>
              <div class="flex-grow">
                <textarea 
                  class="w-full bg-transparent border-b border-neutral-700 focus:border-blue-500 outline-none resize-none py-1 text-sm text-neutral-200 placeholder-neutral-500" 
                  rows="2" 
                  placeholder="Add a public comment (Not implemented yet)..."
                ></textarea>
                <div class="flex justify-end mt-2">
                  <button class="px-4 py-1.5 bg-neutral-800 hover:bg-neutral-700 text-sm font-medium rounded-full opacity-50 cursor-not-allowed">
                    Comment
                  </button>
                </div>
              </div>
            </div>

            <!-- Mock Comments -->
            <div class="space-y-6">
              <div v-for="i in 3" :key="i" class="flex gap-4">
                <div class="w-10 h-10 rounded-full bg-neutral-800 flex-shrink-0"></div>
                <div>
                  <div class="flex items-center gap-2 mb-1">
                    <span class="text-sm font-semibold text-white">@User{{ i }}</span>
                    <span class="text-xs text-neutral-500">{{ i }} days ago</span>
                  </div>
                  <p class="text-sm text-neutral-300">
                    This is a placeholder comment. It looks like a great video, thanks for sharing this content!
                  </p>
                  <div class="flex items-center gap-4 mt-2 text-neutral-400">
                    <button class="hover:text-blue-400"><span class="text-xs font-medium">Like</span></button>
                    <button class="hover:text-blue-400"><span class="text-xs font-medium">Reply</span></button>
                  </div>
                </div>
              </div>
            </div>
          </div>
        </div>
      </div>
      
      <!-- Side Recommendations Panel -->
      <div class="lg:w-1/4 flex-shrink-0 flex flex-col gap-4">
        <h3 class="font-semibold text-lg border-b border-neutral-800 pb-2">Up Next</h3>
        
        <div v-for="i in 5" :key="i" class="flex gap-2 group cursor-pointer">
          <div class="w-40 aspect-video bg-neutral-800 rounded-lg flex-shrink-0 relative overflow-hidden group-hover:scale-105 transition-transform duration-200">
             <!-- Thumbnail Placeholder -->
          </div>
          <div class="flex flex-col py-1">
            <h4 class="text-sm font-semibold line-clamp-2 leading-tight group-hover:text-blue-400">
              Recommended Video Title {{ i }}
            </h4>
            <p class="text-xs text-neutral-400 mt-1">Channel {{ i }}</p>
            <p class="text-xs text-neutral-500">500K views</p>
          </div>
        </div>
      </div>
    </div>
  </div>
</template>
