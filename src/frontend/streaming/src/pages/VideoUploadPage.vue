<script setup lang="ts">
import { ref } from 'vue'
import { useRouter } from 'vue-router'
import VideoUploadForm from '../components/VideoUploadForm.vue'

const router = useRouter()
const successMessage = ref('')
const errorMessage = ref('')
const isUploading = ref(false)

const handleUploadStart = () => {
  isUploading.value = true
  successMessage.value = ''
  errorMessage.value = ''
}

const handleUploadSuccess = () => {
  isUploading.value = false
  successMessage.value = 'Video uploaded successfully! It will be available shortly.'
  // Optionally, you might want to redirect to a different page or clear the form
}

const handleUploadError = (msg: string) => {
  isUploading.value = false
  errorMessage.value = msg
}

const navigateToHome = () => {
  router.push('/home')
}
</script>

<template>
  <div class="min-h-screen bg-black p-6 pt-12 flex flex-col items-center">
    <!-- Feedback Alerts -->
    <div v-if="successMessage" class="mb-6 w-full max-w-2xl bg-green-900/20 border border-green-500/50 text-green-400 px-4 py-3 rounded-lg flex items-center justify-between">
      <span>{{ successMessage }}</span>
      <button @click="successMessage = ''" class="text-green-500 hover:text-green-300">✕</button>
    </div>

    <div v-if="errorMessage" class="mb-6 w-full max-w-2xl bg-red-900/20 border border-red-500/50 text-red-400 px-4 py-3 rounded-lg flex items-center justify-between">
      <span>{{ errorMessage }}</span>
      <button @click="errorMessage = ''" class="text-red-500 hover:text-red-300">✕</button>
    </div>

    <!-- Upload Form Component -->
    <VideoUploadForm 
      @upload-start="handleUploadStart"
      @upload-success="handleUploadSuccess"
      @upload-error="handleUploadError"
    />
  </div>
</template>