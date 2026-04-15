<script setup lang="ts">
import { ref } from 'vue'
import { useRouter } from 'vue-router'
import { useAuthStore } from '../stores/auth'
import { AuthService } from '../apis/AuthService'

const email = ref('')
const password = ref('')
const error = ref('')
const router = useRouter()
const authStore = useAuthStore()

const register = async () => {
  error.value = ''
  try {
    const data = await AuthService.register(email.value, password.value)
    
    authStore.setToken(data.token, data.role || 'Viewer')
    router.push('/home')
  } catch (err: any) {
    error.value = err.message || 'An error occurred during registration. Please try again.'
  }
}
</script>

<template>
  <div class="flex min-h-[80vh] flex-col justify-center px-6 py-12 lg:px-8 bg-black">
    <div class="sm:mx-auto sm:w-full sm:max-w-sm">
      <h2 class="mt-10 text-center text-2xl/9 font-bold tracking-tight text-white">Register a new account</h2>
    </div>

    <div class="mt-10 sm:mx-auto sm:w-full sm:max-w-sm">
      <form class="space-y-6" @submit.prevent="register">
        <div>
          <label for="email" class="block text-sm/6 font-medium text-white">Email address</label>
          <div class="mt-2">
            <input type="email" name="email" id="email" v-model="email" required class="block w-full rounded-md bg-white/5 border border-white/10 px-3 py-1.5 text-base text-white outline-1 -outline-offset-1 outline-white/10 placeholder:text-gray-500 focus:outline-2 focus:-outline-offset-2 focus:outline-indigo-500 sm:text-sm/6">
          </div>
        </div>

        <div>
          <label for="password" class="block text-sm/6 font-medium text-white">Password</label>
          <div class="mt-2">
            <input type="password" name="password" id="password" v-model="password" required class="block w-full rounded-md bg-white/5 border border-white/10 px-3 py-1.5 text-base text-white outline-1 -outline-offset-1 outline-white/10 placeholder:text-gray-500 focus:outline-2 focus:-outline-offset-2 focus:outline-indigo-500 sm:text-sm/6">
          </div>
        </div>

        <div v-if="error" class="text-red-500 text-sm">
          {{ error }}
        </div>

        <div>
          <button type="submit" class="flex w-full justify-center rounded-md bg-indigo-500 px-3 py-1.5 text-sm/6 font-semibold text-white shadow-xs hover:bg-indigo-400 focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-indigo-500">Register</button>
        </div>
      </form>
      
      <p class="mt-10 text-center text-sm/6 text-gray-400">
        Already a member?
        <RouterLink to="/login" class="font-semibold text-indigo-400 hover:text-indigo-300">Sign in</RouterLink>
      </p>
    </div>
  </div>
</template>