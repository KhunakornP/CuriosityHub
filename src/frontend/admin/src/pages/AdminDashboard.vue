<script setup lang="ts">
import { ref } from 'vue';
import VideosTab from '../components/admin/VideosTab.vue';
import UsersTab from '../components/admin/UsersTab.vue';
import CommentsTab from '../components/admin/CommentsTab.vue';
import { useAuthStore } from '../stores/auth';
import { useRouter } from 'vue-router';

const authStore = useAuthStore();
const router = useRouter();

// Simple auth check mock
if (!authStore.isAuthenticated) {
  router.push('/login');
}

const currentTab = ref('videos');

const tabs = [
  { id: 'videos', name: 'Videos' },
  { id: 'users', name: 'Users' },
  { id: 'comments', name: 'Comments' },
];
</script>

<template>
  <div v-if="authStore.isAuthenticated" class="bg-white shadow rounded-lg w-full flex flex-col h-full overflow-hidden">
    <div class="border-b border-gray-200">
      <nav class="-mb-px flex px-6" aria-label="Tabs">
        <button
          v-for="tab in tabs"
          :key="tab.id"
          @click="currentTab = tab.id"
          :class="[
            currentTab === tab.id
              ? 'border-blue-500 text-blue-600'
              : 'border-transparent text-gray-500 hover:text-gray-700 hover:border-gray-300',
            'whitespace-nowrap py-4 px-6 border-b-2 text-base font-medium transition-colors cursor-pointer select-none'
          ]"
        >
          {{ tab.name }}
        </button>
      </nav>
    </div>

    <div class="flex-grow p-6 overflow-y-auto w-full">
      <VideosTab v-if="currentTab === 'videos'" />
      <UsersTab v-if="currentTab === 'users'" />
      <CommentsTab v-if="currentTab === 'comments'" />
    </div>
  </div>
</template>