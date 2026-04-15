<script setup lang="ts">
import { ref } from 'vue'
import { uploadVideo } from '../apis/VideoService'
import { useAuthStore } from '../stores/auth'

const emit = defineEmits<{
  (e: 'upload-start'): void
  (e: 'upload-success'): void
  (e: 'upload-error', message: string): void
}>()

const authStore = useAuthStore()
const title = ref('')
const description = ref('')
const selectedFile = ref<File | null>(null)
const selectedThumbnail = ref<File | null>(null)
const isUploading = ref(false)
const fileInput = ref<HTMLInputElement | null>(null)
const thumbnailInput = ref<HTMLInputElement | null>(null)

const handleFileChange = (event: Event) => {
  const target = event.target as HTMLInputElement
  if (target.files && target.files.length > 0) {
    selectedFile.value = target.files[0]
  } else {
    selectedFile.value = null
  }
}

const handleThumbnailChange = (event: Event) => {
  const target = event.target as HTMLInputElement
  if (target.files && target.files.length > 0) {
    selectedThumbnail.value = target.files[0]
  } else {
    selectedThumbnail.value = null
  }
}

const generateThumbnail = async (file: File): Promise<Blob | null> => {
  return new Promise((resolve) => {
    const video = document.createElement('video')
    video.src = URL.createObjectURL(file)
    video.crossOrigin = 'anonymous'
    
    video.onloadeddata = () => {
      // Seek to 1 second in or the start of the video
      video.currentTime = Math.min(1, video.duration || 0)
    }

    video.onseeked = () => {
      const canvas = document.createElement('canvas')
      canvas.width = video.videoWidth
      canvas.height = video.videoHeight
      const ctx = canvas.getContext('2d')
      
      if (ctx) {
        ctx.drawImage(video, 0, 0, canvas.width, canvas.height)
        canvas.toBlob((blob) => {
          URL.revokeObjectURL(video.src)
          resolve(blob)
        }, 'image/jpeg', 0.8)
      } else {
        URL.revokeObjectURL(video.src)
        resolve(null)
      }
    }

    video.onerror = () => {
      URL.revokeObjectURL(video.src)
      resolve(null)
    }
  })
}

const submitUpload = async () => {
  if (!title.value.trim() || !selectedFile.value) {
    emit('upload-error', 'Title and video file are required.')
    return
  }

  isUploading.value = true
  emit('upload-start')

  const formData = new FormData()
  formData.append('title', title.value.trim())
  if (description.value.trim()) {
    formData.append('description', description.value.trim())
  }
  formData.append('video', selectedFile.value)

  try {
    if (selectedThumbnail.value) {
      // Use user-provided thumbnail
      formData.append('thumbnail', selectedThumbnail.value, selectedThumbnail.value.name)
    } else {
      // Generate auto-thumbnail only if user didn't provide one
      const thumbnailBlob = await generateThumbnail(selectedFile.value)
      if (thumbnailBlob) {
        formData.append('thumbnail', thumbnailBlob, 'thumbnail.jpg')
      }
    }

    if (authStore.userId) {
      formData.append('publisherId', authStore.userId)
    }

    const success = await uploadVideo(formData, authStore.token || '')
    
    if (success) {
      // Reset form
      title.value = ''
      description.value = ''
      selectedFile.value = null
      selectedThumbnail.value = null
      if (fileInput.value) fileInput.value.value = ''
      if (thumbnailInput.value) thumbnailInput.value.value = ''
      
      emit('upload-success')
    } else {
      emit('upload-error', 'Failed to upload video. Please try again.')
    }
  } catch (error) {
    console.error(error)
    emit('upload-error', 'An unexpected error occurred during upload.')
  } finally {
    isUploading.value = false
  }
}
</script>

<template>
  <div class="bg-neutral-900 border border-neutral-800 rounded-xl p-6 shadow-lg max-w-2xl w-full mx-auto">
    <h2 class="text-2xl font-bold text-white mb-6">Upload Video</h2>
    
    <form @submit.prevent="submitUpload" class="space-y-5">
      <!-- Title Input -->
      <div>
        <label for="title" class="block text-sm font-medium text-neutral-300 mb-1">Video Name (Required)</label>
        <input 
          id="title"
          v-model="title"
          type="text" 
          required
          placeholder="Enter a catchy title..."
          class="w-full bg-neutral-800 border border-neutral-700 text-white rounded-lg px-4 py-2 focus:outline-none focus:border-blue-500 focus:ring-1 focus:ring-blue-500 transition-colors"
          :disabled="isUploading"
        />
      </div>

      <!-- Description Input -->
      <div>
        <label for="description" class="block text-sm font-medium text-neutral-300 mb-1">Description (Optional)</label>
        <textarea 
          id="description"
          v-model="description"
          rows="4" 
          placeholder="Tell viewers about your video..."
          class="w-full bg-neutral-800 border border-neutral-700 text-white rounded-lg px-4 py-2 focus:outline-none focus:border-blue-500 focus:ring-1 focus:ring-blue-500 transition-colors resize-none"
          :disabled="isUploading"
        ></textarea>
      </div>

      <!-- File Input -->
      <div>
        <label for="videoFile" class="block text-sm font-medium text-neutral-300 mb-1">Video File (Required)</label>
        <div class="mt-1 flex justify-center px-6 pt-5 pb-6 border-2 border-neutral-700 border-dashed rounded-lg hover:border-blue-500 transition-colors bg-neutral-800/50">
          <div class="space-y-1 text-center">
            <svg class="mx-auto h-12 w-12 text-neutral-400" stroke="currentColor" fill="none" viewBox="0 0 48 48" aria-hidden="true">
              <path d="M28 8H12a4 4 0 00-4 4v20m32-12v8m0 0v8a4 4 0 01-4 4H12a4 4 0 01-4-4v-4m32-4l-3.172-3.172a4 4 0 00-5.656 0L28 28M8 32l9.172-9.172a4 4 0 015.656 0L28 28m0 0l4 4m4-24h8m-4-4v8m-12 4h.02" stroke-width="2" stroke-linecap="round" stroke-linejoin="round" />
            </svg>
            <div class="flex text-sm text-neutral-300">
              <label for="videoFile" class="relative cursor-pointer bg-neutral-800 rounded-md font-medium text-blue-500 hover:text-blue-400 focus-within:outline-none px-1">
                <span>Upload a file</span>
                <input 
                  id="videoFile" 
                  ref="fileInput"
                  name="videoFile" 
                  type="file" 
                  accept="video/*" 
                  class="sr-only" 
                  @change="handleFileChange"
                  :disabled="isUploading"
                  required
                />
              </label>
              <p class="pl-1">or drag and drop</p>
            </div>
            <p class="text-xs text-neutral-500">
              {{ selectedFile ? selectedFile.name : 'MP4, WebM, Ogg up to 500MB' }}
            </p>
          </div>
        </div>
      </div>

      <!-- Thumbnail Input -->
      <div>
        <label for="thumbnailFile" class="block text-sm font-medium text-neutral-300 mb-1">Thumbnail (Optional)</label>
        <div class="mt-1 flex justify-center px-6 pt-5 pb-6 border-2 border-neutral-700 border-dashed rounded-lg hover:border-blue-500 transition-colors bg-neutral-800/50">
          <div class="space-y-1 text-center">
            <svg class="mx-auto h-12 w-12 text-neutral-400" stroke="currentColor" fill="none" viewBox="0 0 48 48" aria-hidden="true">
              <path d="M4 16l16-12 16 12m-16 28V20m16 8v16H8V28" stroke-width="2" stroke-linecap="round" stroke-linejoin="round" />
            </svg>
            <div class="flex text-sm text-neutral-300">
              <label for="thumbnailFile" class="relative cursor-pointer bg-neutral-800 rounded-md font-medium text-blue-500 hover:text-blue-400 focus-within:outline-none px-1">
                <span>Upload a thumbnail</span>
                <input 
                  id="thumbnailFile" 
                  ref="thumbnailInput"
                  name="thumbnailFile" 
                  type="file" 
                  accept="image/jpeg, image/png, image/webp" 
                  class="sr-only" 
                  @change="handleThumbnailChange"
                  :disabled="isUploading"
                />
              </label>
              <p class="pl-1">or let us generate one</p>
            </div>
            <p class="text-xs text-neutral-500">
              {{ selectedThumbnail ? selectedThumbnail.name : 'JPG, PNG, WebP up to 5MB' }}
            </p>
          </div>
        </div>
      </div>

      <!-- Submit Button -->
      <div class="pt-4">
        <button 
          type="submit" 
          class="w-full flex justify-center py-3 px-4 border border-transparent rounded-lg shadow-sm text-sm font-medium text-white bg-blue-600 hover:bg-blue-700 focus:outline-none focus:ring-2 focus:ring-offset-2 focus:ring-blue-500 focus:ring-offset-neutral-900 transition-colors disabled:opacity-50 disabled:cursor-not-allowed"
          :disabled="isUploading || !title || !selectedFile"
        >
          <span v-if="isUploading" class="flex items-center gap-2">
            <svg class="animate-spin -ml-1 mr-3 h-5 w-5 text-white" xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24">
              <circle class="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" stroke-width="4"></circle>
              <path class="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4zm2 5.291A7.962 7.962 0 014 12H0c0 3.042 1.135 5.824 3 7.938l3-2.647z"></path>
            </svg>
            Uploading...
          </span>
          <span v-else>Upload Video</span>
        </button>
      </div>
    </form>
  </div>
</template>