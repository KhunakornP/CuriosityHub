<script setup lang="ts">
import { useRoute, useRouter } from 'vue-router'
import { useFetch } from '@vueuse/core'

const route = useRoute()
const router = useRouter()

const videoId = route.params.id

// Template API Call to get specific video details
const { data: videoData, isFetching: isFetchingVideo, error: videoError } = useFetch(`https://api.curiosityhub.com/videos/${videoId}`, {
  immediate: false, // Set to true when API is ready
}).json()

// Template API Call to get recommendations
const { data: recommendationsData, isFetching: isFetchingRecommendations, error: recommendationsError } = useFetch(`https://api.curiosityhub.com/videos/${videoId}/recommendations`, {
  immediate: false, // Set to true when API is ready
}).json()

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
        <div class="aspect-video bg-neutral-900 rounded-xl flex items-center justify-center border border-neutral-800 shadow-lg">
          <p class="text-neutral-500">Video Player Placeholder (ID: {{ videoId }})</p>
        </div>
        
        <div class="mt-4">
          <h2 class="text-2xl font-semibold mb-2" v-if="!isFetchingVideo">Video Title Placeholder</h2>
          <div class="flex items-center justify-between mb-4 border-b border-neutral-800 pb-4">
            <div class="flex items-center gap-4 text-sm text-neutral-400">
              <span class="text-white hover:text-blue-400 transition-colors cursor-pointer">Channel Name</span>
              <span>1.2M views</span>
              <span>3 days ago</span>
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
            Description placeholder. This is where the video summary and links go.
            Call to action to subscribe or support the channel.
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
