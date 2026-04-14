<script setup lang="ts">
import { ref, computed } from 'vue'

const props = defineProps<{
  placeholder?: string
  buttonText?: string
  isPosting?: boolean
  error?: string | null
}>()

const emit = defineEmits<{
  (e: 'submit', text: string): void
  (e: 'cancel'): void
}>()

const text = ref('')
const charLimit = 1000

const isValid = computed(() => {
  return text.value.trim().length > 0 && text.value.length <= charLimit
})

const handleSubmit = () => {
  if (!isValid.value || props.isPosting) return
  emit('submit', text.value.trim())
  text.value = ''
}
</script>

<template>
  <div class="flex gap-4">
    <div class="w-10 h-10 flex-shrink-0 rounded-full bg-neutral-700"></div>
    <div class="flex-grow">
      <textarea 
        class="w-full bg-transparent border-b border-neutral-700 focus:border-blue-500 outline-none resize-none py-1 text-sm text-neutral-200 placeholder-neutral-500" 
        rows="2" 
        v-model="text"
        :maxlength="charLimit"
        :placeholder="placeholder || 'Add a public comment...'"
        :disabled="isPosting"
      ></textarea>
      
      <div class="flex justify-between items-center mt-2">
        <span class="text-xs text-neutral-500">{{ text.length }} / {{ charLimit }}</span>
        
        <div class="flex items-center gap-4">
          <span v-if="error" class="text-xs text-red-500">{{ error }}</span>
          
          <button 
            v-if="!!$attrs.onCancel"
            @click="emit('cancel'); text = ''"
            class="px-4 py-1.5 text-neutral-400 hover:text-white text-sm font-medium transition-colors"
          >
            Cancel
          </button>
          
          <button 
            @click="handleSubmit"
            :disabled="!isValid || isPosting"
            class="px-4 py-1.5 bg-neutral-800 hover:bg-neutral-700 text-sm font-medium rounded-full cursor-pointer disabled:opacity-50 disabled:cursor-not-allowed transition-colors"
          >
            <span v-if="isPosting" class="flex items-center gap-2">
              <div class="w-3 h-3 border-2 border-white border-t-transparent rounded-full animate-spin"></div>
              Posting...
            </span>
            <span v-else>{{ buttonText || 'Comment' }}</span>
          </button>
        </div>
      </div>
    </div>
  </div>
</template>
