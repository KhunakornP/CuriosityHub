<script setup lang="ts">
import { ref, computed, onMounted } from 'vue';
import { fetchVideos as apiFetchVideos, updateVideoMetadata, deleteVideo as apiDeleteVideo } from '../../apis/VideoService';
import VideoUploadForm from './VideoUploadForm.vue';

interface Video {
  id: string;
  title: string;
  videoId: string;
  description: string;
  owner: string;
  uploadedAt: string;
  views: number;
  _isDirty?: boolean;
}

const videos = ref<Video[]>([]);
const loading = ref(false);
const showCreateModal = ref(false);

const currentPage = ref(1);
const pageSize = ref(10);

async function fetchVideos() {
  loading.value = true;
  try {
    const data = await apiFetchVideos(currentPage.value, pageSize.value);
    
    videos.value = data.map((item: any) => ({
      id: item.videoId || item.id,
      title: item.title,
      description: item.description,
      videoId: item.videoId,
      owner: 'Publisher',
      uploadedAt: new Date().toISOString(),
      views: item.views,
      _isDirty: false
    }));
  } catch (error) {
    console.error('Failed to load videos', error);
  } finally {
    loading.value = false;
  }
}

onMounted(() => {
  fetchVideos();
});

function markDirty(video: Video) {
  video._isDirty = true;
}

const hasDirty = computed(() => videos.value.some(v => v._isDirty));

async function saveChanges() {
  const dirtyVideos = videos.value.filter(v => v._isDirty);
  try {
    await Promise.all(dirtyVideos.map(v => 
      updateVideoMetadata(v.id, v.title, v.description)
    ));
    alert(`Saved changes for ${dirtyVideos.length} videos`);
    dirtyVideos.forEach(v => v._isDirty = false);
  } catch (error) {
    alert('Failed to save some changes: ' + error);
  }
}

async function deleteVideo(video: Video) {
  if (confirm(`Delete video ${video.title}?`)) {
    try {
      const success = await apiDeleteVideo(video.id);
      if (!success) throw new Error('Delete failed');
      videos.value = videos.value.filter(v => v.id !== video.id);
    } catch (error) {
      alert('Failed to delete video: ' + error);
    }
  }
}
</script>

<template>
  <div class="flex flex-col h-full relative">
    <div class="flex justify-between items-center mb-4">
      <h3 class="text-lg font-semibold text-gray-800">Manage Videos</h3>
      <div class="space-x-2">
        <button @click="showCreateModal = true" class="px-4 py-2 bg-blue-600 text-white rounded hover:bg-blue-700 transition">
          Create Video
        </button>        
        <button v-if="hasDirty" @click="saveChanges" class="px-4 py-2 bg-green-600 text-white rounded hover:bg-green-700">
          Save All Changes
        </button>
      </div>
    </div>

    <!-- Create Video Modal -->
    <div v-if="showCreateModal" class="absolute inset-0 bg-black bg-opacity-50 flex items-center justify-center z-10 p-4">
      <div class="bg-transparent w-full max-w-2xl relative">
        <button @click="showCreateModal = false" class="absolute -top-4 -right-4 bg-gray-300 text-black px-3 py-1 rounded-full hover:bg-red-500 hover:text-white transition shadow z-20">X</button>
        <VideoUploadForm @upload-success="() => { showCreateModal = false; fetchVideos(); }" @upload-error="(msg) => alert(msg)" />
      </div>
    </div>

    <div v-if="loading" class="text-center py-10 text-gray-500">Loading videos...</div>

    <div v-else class="overflow-x-auto bg-white border border-gray-200 rounded-lg">
      <table class="min-w-full divide-y divide-gray-200">
        <thead class="bg-gray-50">
          <tr>
            <th scope="col" class="w-8 px-4 py-3 text-center"></th>
            <th scope="col" class="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">Title</th>
            <th scope="col" class="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">Video ID</th>
            <th scope="col" class="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">Description</th>
            <th scope="col" class="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">Owner</th>
            <th scope="col" class="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">Views</th>
            <th scope="col" class="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">Date Uploaded</th>
            <th scope="col" class="px-6 py-3 text-right text-xs font-medium text-gray-500 uppercase tracking-wider">Actions</th>
          </tr>
        </thead>
        <tbody class="bg-white divide-y divide-gray-200">
          <tr v-for="video in videos" :key="video.id" class="hover:bg-gray-50 transition-colors">
            <td class="w-8 px-4 py-4 text-center">
              <span v-if="video._isDirty" class="text-yellow-500 h-2 w-2 inline-block rounded-full bg-yellow-500" title="Modified"></span>
            </td>
            <td class="px-6 py-4 whitespace-nowrap">
              <input type="text" v-model="video.title" @input="markDirty(video)" class="w-full bg-transparent border-b border-transparent focus:border-blue-500 focus:outline-none text-sm font-medium text-gray-900 px-1 py-1" />
            </td>
            <td class="px-6 py-4 whitespace-nowrap truncate max-w-xs text-sm text-gray-600">
              {{ video.videoId }}
            </td>
            <td class="px-6 py-4">
              <input type="text" v-model="video.description" @input="markDirty(video)" class="w-full bg-transparent border-b border-transparent focus:border-blue-500 focus:outline-none text-sm text-gray-600 px-1 py-1 min-w-[200px] truncate" />
            </td>
            <td class="px-6 py-4 whitespace-nowrap text-sm text-gray-400 cursor-not-allowed select-none">{{ video.owner }}</td>
            <td class="px-6 py-4 whitespace-nowrap text-sm text-gray-400 cursor-not-allowed select-none">{{ video.views }}</td>
            <td class="px-6 py-4 whitespace-nowrap text-sm text-gray-400 select-none">{{ new Date(video.uploadedAt).toLocaleDateString() }}</td>
            <td class="px-6 py-4 whitespace-nowrap text-right text-sm font-medium">
              <button @click="deleteVideo(video)" class="text-red-500 hover:text-red-700 transition" title="Delete Video">
                <svg xmlns="http://www.w3.org/2000/svg" class="h-5 w-5 inline" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                  <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M19 7l-.867 12.142A2 2 0 0116.138 21H7.862a2 2 0 01-1.995-1.858L5 7m5 4v6m4-6v6m1-10V4a1 1 0 00-1-1h-4a1 1 0 00-1 1v3M4 7h16" />
                </svg>
              </button>
            </td>
          </tr>
          <tr v-if="videos.length === 0">
            <td colspan="7" class="px-6 py-10 text-center text-sm text-gray-500">No videos found.</td>
          </tr>
        </tbody>
      </table>
    </div>
  </div>
</template>