<script setup lang="ts">
import { ref, onMounted, computed } from 'vue'
import { useRouter } from 'vue-router'
import { useAuthStore } from '../stores/auth'
import { ProfileService } from '../apis/ProfileService'
import { VideoStudioService, VideoMetadata } from '../apis/VideoStudioService'

const router = useRouter()
const authStore = useAuthStore()

const firstName = ref('')
const lastName = ref('')
const description = ref('')
const profileUrl = ref('')
const email = computed(() => authStore.userEmail || '')
const isResearcher = computed(() => authStore.userRole === 1 || authStore.userRole === 2 || authStore.userRole === '1' || authStore.userRole === '2')
const publisherId = computed(() => authStore.userId || '')

const isLoading = ref(true)
const isSaving = ref(false)
const successMessage = ref('')
const errorMessage = ref('')

const studioVideos = ref<VideoMetadata[]>([])
const loadingStudio = ref(false)
const studioError = ref('')
const updatingVideoId = ref<string | null>(null)
const currentPage = ref(1)
const totalPages = ref(1)
const pageSize = 10

// Refs for editing current video
const editTitle = ref('')
const editDescription = ref('')
const editVideoFile = ref<File | null>(null)
const editThumbnailFile = ref<File | null>(null)

onMounted(async () => {
  if (!authStore.token) {
    router.push('/login')
    return
  }
  
  try {
    const profile = await ProfileService.getProfile(authStore.token)
    firstName.value = profile.firstName || ''
    lastName.value = profile.lastName || ''
    description.value = profile.description || ''
    profileUrl.value = profile.profileUrl || ''

    if (isResearcher.value && publisherId.value) {
      await loadStudioVideos()
    }
  } catch (error: any) {
    errorMessage.value = 'Failed to load profile parameters: ' + (error.message || error)
  } finally {
    isLoading.value = false
  }
})

const loadStudioVideos = async (page: number = 1) => {
  if (!authStore.token || !publisherId.value) return
  loadingStudio.value = true
  studioError.value = ''
  try {
    const res = await VideoStudioService.getPublisherVideos(publisherId.value, authStore.token, page, pageSize)
    studioVideos.value = res.videos
    currentPage.value = res.page
    totalPages.value = Math.ceil(res.totalCount / res.pageSize) || 1
  } catch (err: any) {
    studioError.value = err.message || 'Failed to load studio videos'
  } finally {
    loadingStudio.value = false
  }
}

const nextPage = () => {
  if (currentPage.value < totalPages.value) {
    loadStudioVideos(currentPage.value + 1)
  }
}

const prevPage = () => {
  if (currentPage.value > 1) {
    loadStudioVideos(currentPage.value - 1)
  }
}

const saveProfile = async () => {
  if (!authStore.token) return
  
  isSaving.value = true
  successMessage.value = ''
  errorMessage.value = ''
  
  try {
    await ProfileService.updateProfile(authStore.token, {
      firstName: firstName.value,
      lastName: lastName.value,
      description: description.value,
      profileUrl: profileUrl.value
    })
    successMessage.value = 'Profile updated successfully!'
  } catch (error: any) {
    errorMessage.value = 'Failed to update profile: ' + (error.message || error)
  } finally {
    isSaving.value = false
  }
}

const startEditingVideo = (video: VideoMetadata) => {
  updatingVideoId.value = (video.id || video.videoId)!
  editTitle.value = video.title
  editDescription.value = video.description || ''
  editVideoFile.value = null
  editThumbnailFile.value = null
}

const cancelEditingVideo = () => {
  updatingVideoId.value = null
  editTitle.value = ''
  editDescription.value = ''
  editVideoFile.value = null
  editThumbnailFile.value = null
}

const handleVideoFileChange = (e: Event) => {
  const target = e.target as HTMLInputElement
  if (target.files && target.files.length > 0) {
    editVideoFile.value = target.files[0]
  }
}

const handleThumbnailFileChange = (e: Event) => {
  const target = e.target as HTMLInputElement
  if (target.files && target.files.length > 0) {
    editThumbnailFile.value = target.files[0]
  }
}

const saveVideoUpdates = async (videoId: string) => {
  if (!authStore.token) return
  try {
    await VideoStudioService.updateMetadata(videoId, editTitle.value, editDescription.value, authStore.token)
    
    if (editVideoFile.value) {
      await VideoStudioService.updateVideoFile(videoId, editVideoFile.value, authStore.token)
    }
    
    if (editThumbnailFile.value) {
      await VideoStudioService.updateThumbnailFile(videoId, editThumbnailFile.value, authStore.token)
    }

    alert('Video updated successfully!')
    cancelEditingVideo()
    await loadStudioVideos()
  } catch (err: any) {
    alert('Failed to update video: ' + err.message)
  }
}

const navigateToUpload = () => {
  router.push('/upload')
}
</script>

<template>
  <div class="min-h-screen bg-black text-white p-6 md:p-12">
    <div class="max-w-2xl mx-auto mt-10">
      <div class="flex items-center justify-between mb-8">
        <h1 class="text-3xl font-bold">Your Profile</h1>
        <button 
          v-if="isResearcher" 
          @click="navigateToUpload"
          class="bg-indigo-600 hover:bg-indigo-500 text-white px-4 py-2 rounded-md font-semibold transition"
        >
          Upload Video
        </button>
      </div>

      <div v-if="isLoading" class="text-gray-400">
        Loading profile information...
      </div>

      <div v-else class="bg-gray-900 rounded-lg p-8 shadow-lg border border-gray-800">
        <form @submit.prevent="saveProfile" class="space-y-6">
          
          <div v-if="successMessage" class="bg-green-900/50 border border-green-500 text-green-300 p-3 rounded">
            {{ successMessage }}
          </div>
          <div v-if="errorMessage" class="bg-red-900/50 border border-red-500 text-red-300 p-3 rounded">
            {{ errorMessage }}
          </div>

          <div class="grid grid-cols-1 md:grid-cols-2 gap-6">
            <div>
              <label for="firstName" class="block text-sm font-medium text-gray-300 mb-2">First Name</label>
              <input 
                id="firstName" 
                v-model="firstName" 
                type="text" 
                required
                class="w-full bg-gray-800 border border-gray-700 rounded-md px-4 py-2 text-white focus:outline-none focus:ring-2 focus:ring-indigo-500"
              />
            </div>
            
            <div>
              <label for="lastName" class="block text-sm font-medium text-gray-300 mb-2">Last Name</label>
              <input 
                id="lastName" 
                v-model="lastName" 
                type="text" 
                required
                class="w-full bg-gray-800 border border-gray-700 rounded-md px-4 py-2 text-white focus:outline-none focus:ring-2 focus:ring-indigo-500"
              />
            </div>
          </div>

          <div>
            <label for="email" class="block text-sm font-medium text-gray-300 mb-2">Email Address</label>
            <input 
              id="email" 
              v-model="email" 
              type="email" 
              disabled
              class="w-full bg-gray-800/50 border border-gray-700 text-gray-500 cursor-not-allowed rounded-md px-4 py-2 focus:outline-none"
            />
            <p class="text-xs text-gray-500 mt-1">Email cannot be changed.</p>
          </div>

          <div>
            <label for="description" class="block text-sm font-medium text-gray-300 mb-2">Bibliography / Bio</label>
            <textarea 
              id="description" 
              v-model="description" 
              rows="4" 
              class="w-full bg-gray-800 border border-gray-700 rounded-md px-4 py-2 text-white focus:outline-none focus:ring-2 focus:ring-indigo-500"
              placeholder="Tell us about yourself..."
            ></textarea>
          </div>

          <div class="pt-4 flex justify-end">
            <button 
              type="submit" 
              :disabled="isSaving"
              class="bg-indigo-600 hover:bg-indigo-500 disabled:opacity-50 text-white px-6 py-2 rounded-md font-semibold transition"
            >
              {{ isSaving ? 'Saving...' : 'Save Profile' }}
            </button>
          </div>
        </form>
      </div>

      <!-- Researcher Video Studio Section -->
      <div v-if="isResearcher" class="mt-12">
        <h2 class="text-2xl font-bold mb-6">Video Studio</h2>
        <div v-if="loadingStudio" class="text-gray-400">Loading your videos...</div>
        <div v-else-if="studioError" class="text-red-400">{{ studioError }}</div>
        <div v-else-if="studioVideos.length === 0" class="text-gray-400">No videos found. Upload one!</div>
        <div v-else class="space-y-6">
          <div v-for="video in studioVideos" :key="(video.id || video.videoId)!" class="bg-gray-900 rounded-lg p-6 shadow-lg border border-gray-800 flex flex-col md:flex-row gap-6">
            <div class="w-full md:w-1/3 flex-shrink-0">
               <img v-if="video.thumbnailUrl" :src="video.thumbnailUrl" alt="Thumbnail" class="w-full h-auto rounded-md object-cover border border-gray-800 aspect-video mix-blend-screen bg-gray-800" />
               <div v-else class="w-full h-auto aspect-video bg-gray-800 rounded-md border border-gray-700 flex items-center justify-center text-gray-500">No Image</div>
            </div>
            <div class="flex-grow flex flex-col justify-center">
              <div v-if="updatingVideoId !== (video.id || video.videoId)">
                <h3 class="text-xl font-bold">{{ video.title }}</h3>
                <p class="text-gray-400 mt-2">{{ video.description || 'No description' }}</p>
                <div class="mt-4 text-sm text-gray-500 flex gap-2 items-center">
                  <span>Views: {{ video.views }}</span>
                  <span class="text-gray-700">&bull;</span>
                  <span>Published: {{ video.publishedAt }}</span>
                </div>
                <div class="mt-4 flex gap-4">
                  <button @click="startEditingVideo(video)" class="bg-blue-600 hover:bg-blue-500 text-white px-4 py-2 rounded-md font-semibold transition shadow-md hover:shadow-lg">
                    Edit Video
                  </button>
                </div>
              </div>

              <div v-else class="space-y-4 border-t-0 p-4 border border-gray-700 rounded-md bg-gray-800/20">
                <h3 class="text-lg font-bold px-1">Edit Video Metadata & Content</h3>
                
                <div>
                  <label class="block text-sm font-medium text-gray-300 mb-1 px-1">Title</label>
                  <input v-model="editTitle" type="text" class="w-full bg-gray-800 border border-gray-700 rounded-md px-4 py-2 text-white focus:outline-none focus:ring-2 focus:ring-indigo-500" required />
                </div>

                <div>
                  <label class="block text-sm font-medium text-gray-300 mb-1 px-1">Description</label>
                  <textarea v-model="editDescription" rows="3" class="w-full bg-gray-800 border border-gray-700 rounded-md px-4 py-2 text-white focus:outline-none focus:ring-2 focus:ring-indigo-500"></textarea>
                </div>

                <div class="pt-2">
                  <label class="block text-sm font-medium text-gray-300 mb-1 px-1">Replace Video File (Optional)</label>
                  <input type="file" accept="video/mp4" @change="handleVideoFileChange" class="w-full text-sm text-gray-400 file:mr-4 file:py-2 file:px-4 file:rounded-md file:border-0 file:text-sm file:font-semibold file:bg-gray-800 file:text-indigo-300 hover:file:bg-gray-700 transition" />
                </div>

                <div class="pt-2">
                  <label class="block text-sm font-medium text-gray-300 mb-1 px-1">Replace Thumbnail (Optional)</label>
                  <input type="file" accept="image/jpeg,image/png,image/webp" @change="handleThumbnailFileChange" class="w-full text-sm text-gray-400 file:mr-4 file:py-2 file:px-4 file:rounded-md file:border-0 file:text-sm file:font-semibold file:bg-gray-800 file:text-indigo-300 hover:file:bg-gray-700 transition" />
                </div>

                <div class="flex gap-4 pt-4 px-1">
                  <button @click="saveVideoUpdates((video.id || video.videoId)!)" class="bg-green-600 hover:bg-green-500 text-white px-5 py-2 rounded-md font-semibold transition shadow-md hover:shadow-lg">
                    Save Changes
                  </button>
                  <button @click="cancelEditingVideo" class="bg-gray-700 hover:bg-gray-600 text-white px-5 py-2 rounded-md font-semibold transition shadow-sm hover:shadow">
                    Cancel
                  </button>
                </div>
              </div>
            </div>
          </div>
          
          <div v-if="totalPages > 1" class="flex justify-between items-center bg-gray-900 border border-gray-800 px-6 py-4 rounded-xl shadow-lg mt-6">
              <button @click="prevPage" :disabled="currentPage === 1" class="px-5 py-2 bg-gray-800 hover:bg-gray-700 disabled:opacity-50 text-white font-medium rounded-md transition shadow">Previous</button>
              <div class="flex gap-2 text-sm text-gray-300 font-medium">Page <span class="text-white">{{ currentPage }}</span> of <span class="text-white">{{ totalPages }}</span></div>
              <button @click="nextPage" :disabled="currentPage === totalPages" class="px-5 py-2 bg-gray-800 hover:bg-gray-700 disabled:opacity-50 text-white font-medium rounded-md transition shadow">Next</button>
          </div>
        </div>
      </div>
    </div>
  </div>
</template>