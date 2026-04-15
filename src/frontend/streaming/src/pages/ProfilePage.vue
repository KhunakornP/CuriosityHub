<script setup lang="ts">
import { ref, onMounted, computed } from 'vue'
import { useRouter } from 'vue-router'
import { useAuthStore } from '../stores/auth'
import { ProfileService } from '../apis/ProfileService'

const router = useRouter()
const authStore = useAuthStore()

const firstName = ref('')
const lastName = ref('')
const description = ref('')
const profileUrl = ref('')
const email = computed(() => authStore.userEmail || '')
const isResearcher = computed(() => authStore.userRole === 'Researcher' || authStore.userRole === 'Admin')

const isLoading = ref(true)
const isSaving = ref(false)
const successMessage = ref('')
const errorMessage = ref('')

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
  } catch (error: any) {
    errorMessage.value = 'Failed to load profile parameters: ' + (error.message || error)
  } finally {
    isLoading.value = false
  }
})

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
    </div>
  </div>
</template>