<script setup lang="ts">
import { ref, onMounted, computed, watch } from 'vue'
import { useRouter, useRoute } from 'vue-router'
import { useAuthStore } from '../stores/auth'
import { ProfileService } from '../apis/ProfileService'

const router = useRouter()
const route = useRoute()
const authStore = useAuthStore()

const firstName = ref('')
const lastName = ref('')
const description = ref('')
const email = ref('')

const targetId = computed(() => route.params.id as string)
const isSelf = computed(() => targetId.value === authStore.userEmail)

const isLoading = ref(true)
const errorMessage = ref('')

const loadProfile = async () => {
  if (!authStore.token) {
    router.push('/login')
    return
  }
  
  if (isSelf.value) {
    // Navigate to their own full profile edit page instead
    router.push('/profile')
    return
  }

  isLoading.value = true
  errorMessage.value = ''
  
  try {
    const profile = await ProfileService.getProfile(authStore.token, targetId.value)
    firstName.value = profile.firstName || ''
    lastName.value = profile.lastName || ''
    description.value = profile.description || ''
    email.value = targetId.value // using the passed target as email display or identifier
  } catch (error: any) {
    errorMessage.value = 'Failed to load user profile: ' + (error.message || error)
  } finally {
    isLoading.value = false
  }
}

watch(() => route.params.id, () => {
  loadProfile()
})

onMounted(() => {
  loadProfile()
})
</script>

<template>
  <div class="min-h-screen bg-black text-white p-6 md:p-12">
    <div class="max-w-2xl mx-auto mt-10">
      <div class="flex items-center justify-between mb-8">
        <h1 class="text-3xl font-bold" v-if="!isLoading && !errorMessage">{{ firstName }} {{ lastName }}'s Profile</h1>
        <h1 class="text-3xl font-bold" v-else>User Profile</h1>
      </div>

      <div v-if="isLoading" class="text-gray-400">
        Loading profile information...
      </div>

      <div v-else-if="errorMessage" class="bg-red-900/50 border border-red-500 text-red-300 p-6 rounded-lg shadow-lg">
        {{ errorMessage }}
      </div>

      <div v-else class="bg-gray-900 rounded-lg p-8 shadow-lg border border-gray-800 space-y-6">
        
        <div class="grid grid-cols-1 md:grid-cols-2 gap-6">
          <div>
            <label class="block text-sm font-medium text-gray-400 mb-1">First Name</label>
            <div class="text-lg font-semibold">{{ firstName }}</div>
          </div>
          
          <div>
            <label class="block text-sm font-medium text-gray-400 mb-1">Last Name</label>
            <div class="text-lg font-semibold">{{ lastName }}</div>
          </div>
        </div>

        <div>
          <label class="block text-sm font-medium text-gray-400 mb-1">User Identifier</label>
          <div class="text-md text-gray-300">{{ email }}</div>
        </div>

        <div>
          <label class="block text-sm font-medium text-gray-400 mb-1">Bibliography / Bio</label>
          <div class="text-md text-gray-200 whitespace-pre-wrap mt-2 bg-gray-800/50 p-4 rounded-md border border-gray-800">
            {{ description || 'This user has not provided a biography yet.' }}
          </div>
        </div>
      </div>
    </div>
  </div>
</template>