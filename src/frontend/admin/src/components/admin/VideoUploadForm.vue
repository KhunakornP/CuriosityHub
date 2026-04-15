<script setup lang="ts">
import { ref } from 'vue'
import { useAuthStore } from '../../stores/auth'

const emit = defineEmits<{
  (e: 'upload-start'): void
  (e: 'upload-success'): void
  (e: 'upload-error', message: string): void
}>()

const apiBase = import.meta.env.VITE_API_BASE_URL || 'http://localhost:5001';
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
      formData.append('thumbnail', selectedThumbnail.value, selectedThumbnail.value.name)
    } else {
      const thumbnailBlob = await generateThumbnail(selectedFile.value)
      if (thumbnailBlob) {
        formData.append('thumbnail', thumbnailBlob, 'thumbnail.jpg')
      }
    }

    formData.append('publisherId', "Admin"); // Admin publisher by default or parse from token

    const res = await fetch(`${apiBase}/upload`, {
      method: 'POST',
      headers: {
        'Authorization': `Bearer ${authStore.token}`
      },
      body: formData
    });
    
    if (res.ok) {
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
  <div class="bg-white border border-gray-200 rounded-xl p-6 shadow-sm max-w-2xl w-full mx-auto">
    <h2 class="text-2xl font-bold text-gray-800 mb-6">Upload New Video</h2>
    
    <form @submit.prevent="submitUpload" class="space-y-5">
      <div>
        <label for="title" class="block text-sm font-medium text-gray-700 mb-1">Video Name (Required)</label>
        <input 
          id="title"
          v-model="title"
          type="text" 
          required
          placeholder="Enter a catchy title..."
          class="w-full bg-gray-50 border border-gray-300 text-gray-900 rounded-lg px-4 py-2 focus:outline-none focus:border-blue-500 focus:ring-1 focus:ring-blue-500 transition-colors"
          :disabled="isUploading"
        />
      </div>

      <div>
        <label for="description" class="block text-sm font-medium text-gray-700 mb-1">Description (Optional)</label>
        <textarea 
          id="description"
          v-model="description"
          rows="4" 
          placeholder="Tell viewers about your video..."
          class="w-full bg-gray-50 border border-gray-300 text-gray-900 rounded-lg px-4 py-2 focus:outline-none focus:border-blue-500 focus:ring-1 focus:ring-blue-500 transition-colors resize-none"
          :disabled="isUploading"
        ></textarea>
      </div>

      <div>
        <label for="videoFile" class="block text-sm font-medium text-gray-700 mb-1">Video File (Required)</label>
        <div class="mt-1 flex justify-center px-6 pt-5 pb-6 border-2 border-gray-300 border-dashed rounded-lg hover:border-blue-500 transition-colors bg-gray-50">
          <div class="space-y-1 text-center">
            <div class="flex text-sm text-gray-600 justify-center">
              <label for="videoFile" class="relative cursor-pointer bg-transparent rounded-md font-medium text-blue-600 hover:text-blue-500 focus-within:outline-none px-1">
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
            <p class="text-xs text-gray-500">
              {{ selectedFile ? selectedFile.name : 'MP4, WebM, Ogg up to 500MB' }}
            </p>
          </div>
        </div>
      </div>

      <div>
        <label for="thumbnailFile" class="block text-sm font-medium text-gray-700 mb-1">Thumbnail (Optional)</label>
        <div class="mt-1 flex justify-center px-6 pt-5 pb-6 border-2 border-gray-300 border-dashed rounded-lg hover:border-blue-500 transition-colors bg-gray-50">
          <div class="space-y-1 text-center">
            <div class="flex text-sm text-gray-600 justify-center">
              <label for="thumbnailFile" class="relative cursor-pointer bg-transparent rounded-md font-medium text-blue-600 hover:text-blue-500 focus-within:outline-none px-1">
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
            <p class="text-xs text-gray-500">
              {{ selectedThumbnail ? selectedThumbnail.name : 'JPG, PNG, WebP up to 5MB' }}
            </p>
          </div>
        </div>
      </div>

      <div class="pt-4">
        <button 
          type="submit" 
          class="w-full flex justify-center py-3 px-4 border border-transparent rounded-lg shadow-sm text-sm font-medium text-white bg-blue-600 hover:bg-blue-700 focus:outline-none focus:ring-2 focus:ring-offset-2 focus:ring-blue-500 transition-colors disabled:opacity-50 disabled:cursor-not-allowed"
          :disabled="isUploading || !title || !selectedFile"
        >
          <span v-if="isUploading">Uploading...</span>
          <span v-else>Upload Video</span>
        </button>
      </div>
    </form>
  </div>
</template>