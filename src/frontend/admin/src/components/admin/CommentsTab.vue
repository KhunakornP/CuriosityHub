<script setup lang="ts">
import { ref, computed } from 'vue';

interface Comment {
  id: string;
  author: string;
  videoId: string;
  parentCommentId: string | null;
  content: string;
  postedAt: string;
  _isDirty?: boolean;
}

const comments = ref<Comment[]>([
  { id: 'c1', author: 'john_doe', videoId: 'v1', parentCommentId: null, content: 'Great tutorial!', postedAt: '2025-01-01 10:00' },
  { id: 'c2', author: 'spam_bot', videoId: 'v1', parentCommentId: 'c1', content: 'Buy cheap watches here!', postedAt: '2025-01-02 14:20' },
  { id: 'c3', author: 'moderator_x', videoId: 'v2', parentCommentId: null, content: 'Thanks for the guide.', postedAt: '2025-01-03 09:15' },
]);

const loading = ref(false);
const filterBy = ref<'all' | 'videoId' | 'parentCommentId'>('all');

const groupedComments = computed(() => {
  if (filterBy.value === 'all') return { 'All Comments': comments.value };
  
  const groups: Record<string, Comment[]> = {};
  comments.value.forEach(c => {
    const key = (filterBy.value === 'videoId' ? c.videoId : c.parentCommentId) || 'None';
    if (!groups[key]) groups[key] = [];
    groups[key].push(c);
  });
  return groups;
});

function markDirty(comment: Comment) {
  comment._isDirty = true;
}

const hasDirty = computed(() => comments.value.some(c => c._isDirty));

function saveChanges() {
  const dirtyComments = comments.value.filter(c => c._isDirty);
  alert(`Saved changes for ${dirtyComments.length} comments`);
  dirtyComments.forEach(c => c._isDirty = false);
}

function deleteComment(comment: Comment) {
  if (confirm(`Delete comment ${comment.id}?`)) {
    comments.value = comments.value.filter(c => c.id !== comment.id);
  }
}
</script>

<template>
  <div class="flex flex-col h-full">
    <div class="flex justify-between items-center mb-4">
      <h3 class="text-lg font-semibold text-gray-800">Manage Comments</h3>
      <div class="space-x-4 flex items-center">
        <select v-model="filterBy" class="bg-white border border-gray-300 rounded px-3 py-2 text-sm text-gray-700 shadow-sm focus:outline-none focus:ring-blue-500 focus:border-blue-500">
          <option value="all">Group By: None</option>
          <option value="videoId">Group By: Video ID</option>
          <option value="parentCommentId">Group By: Parent Comment ID</option>
        </select>
        <button v-if="hasDirty" @click="saveChanges" class="px-4 py-2 bg-green-600 text-white rounded hover:bg-green-700">
          Save All Changes
        </button>
      </div>
    </div>

    <div v-if="loading" class="text-center py-10 text-gray-500">Loading comments...</div>

    <div v-else class="overflow-x-auto bg-white border border-gray-200 rounded-lg">
      <div v-for="(group, key) in groupedComments" :key="key" class="mb-6 last:mb-0">
        <div v-if="filterBy !== 'all'" class="px-6 py-2 bg-gray-100 text-xs font-bold text-gray-600 uppercase border-b border-gray-200">
          {{ filterBy === 'videoId' ? 'Video ID:' : 'Parent ID:' }} {{ key }}
        </div>
        <table class="min-w-full divide-y divide-gray-200">
          <thead v-if="filterBy === 'all'" class="bg-gray-50">
            <tr>
              <th scope="col" class="w-8 px-4 py-3 text-center"></th>
              <th scope="col" class="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">ID</th>
              <th scope="col" class="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">Author</th>
              <th scope="col" class="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">Video ID / Parent ID</th>
              <th scope="col" class="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">Content</th>
              <th scope="col" class="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">Date</th>
              <th scope="col" class="px-6 py-3 text-right text-xs font-medium text-gray-500 uppercase tracking-wider">Actions</th>
            </tr>
          </thead>
          <tbody class="bg-white divide-y divide-gray-200">
            <tr v-for="comment in group" :key="comment.id" class="hover:bg-gray-50 transition-colors">
              <td class="w-8 px-4 py-4 text-center">
                <span v-if="comment._isDirty" class="text-yellow-500 h-2 w-2 inline-block rounded-full bg-yellow-500" title="Modified"></span>
              </td>
              <td class="px-6 py-4 whitespace-nowrap text-sm text-gray-500">{{ comment.id }}</td>
              <td class="px-6 py-4 whitespace-nowrap">
                <input type="text" v-model="comment.author" @input="markDirty(comment)" class="w-full bg-transparent border-b border-transparent focus:border-blue-500 focus:outline-none text-sm text-gray-900 px-1 py-1" />
              </td>
              <td class="px-6 py-4 whitespace-nowrap text-sm text-gray-500 flex flex-col">
                <span>V: {{ comment.videoId }}</span>
                <span v-if="comment.parentCommentId" class="text-xs text-gray-400">P: {{ comment.parentCommentId }}</span>
              </td>
              <td class="px-6 py-4">
                <input type="text" v-model="comment.content" @input="markDirty(comment)" class="w-full bg-transparent border-b border-transparent focus:border-blue-500 focus:outline-none text-sm text-gray-900 px-1 py-1 min-w-[200px]" />
              </td>
              <td class="px-6 py-4 whitespace-nowrap text-sm text-gray-400 select-none">{{ comment.postedAt }}</td>
              <td class="px-6 py-4 whitespace-nowrap text-right text-sm font-medium">
                <button @click="deleteComment(comment)" class="text-red-500 hover:text-red-700 transition" title="Delete Comment">
                  <svg xmlns="http://www.w3.org/2000/svg" class="h-5 w-5 inline" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                    <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M19 7l-.867 12.142A2 2 0 0116.138 21H7.862a2 2 0 01-1.995-1.858L5 7m5 4v6m4-6v6m1-10V4a1 1 0 00-1-1h-4a1 1 0 00-1 1v3M4 7h16" />
                  </svg>
                </button>
              </td>
            </tr>
          </tbody>
        </table>
      </div>
      <div v-if="comments.length === 0" class="px-6 py-10 text-center text-sm text-gray-500">
        No comments found.
      </div>
    </div>
  </div>
</template>