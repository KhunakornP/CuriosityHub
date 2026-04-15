<script setup lang="ts">
import { ref, computed, onMounted } from 'vue';
import { fetchComments as apiFetchComments, updateComment as apiUpdateComment, deleteComment as apiDeleteComment, createComment as apiCreateComment } from '../../apis/CommentService';

interface Comment {
  id: string;
  author: string;
  videoId: string;
  parentCommentId: string | null;
  content: string;
  postedAt: string;
  _isDirty?: boolean;
}

const comments = ref<Comment[]>([]);
const loading = ref(false);
const showCreateModal = ref(false);
const newComment = ref({ videoId: '', text: '' });
const filterBy = ref<'all' | 'videoId' | 'parentCommentId'>('all');

const currentPage = ref(1);
const pageSize = ref(100); // Higher page size for grouped views right now

async function fetchComments() {
  loading.value = true;
  try {
    const data = await apiFetchComments(currentPage.value, pageSize.value);
    
    comments.value = data.map((item: any) => ({
      id: item.id,
      author: item.userId || 'Unknown',
      videoId: item.videoId,
      parentCommentId: item.parentCommentId,
      content: item.text,
      postedAt: new Date(item.createdAt).toLocaleString(),
      _isDirty: false
    }));
  } catch (error) {
    console.error('Failed to load comments', error);
  } finally {
    loading.value = false;
  }
}

onMounted(() => {
  fetchComments();
});

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

async function saveChanges() {
  const dirtyComments = comments.value.filter(c => c._isDirty);
  try {
    await Promise.all(dirtyComments.map(c => 
      apiUpdateComment(c.id, c.content)
    ));
    alert(`Saved changes for ${dirtyComments.length} comments`);
    dirtyComments.forEach(c => c._isDirty = false);
  } catch (error) {
    alert('Failed to save some comments: ' + error);
  }
}

async function deleteComment(comment: Comment) {
  if (confirm(`Delete comment ${comment.id}?`)) {
    try {
      const success = await apiDeleteComment(comment.id);
      if (!success) throw new Error('Delete failed');
      comments.value = comments.value.filter(c => c.id !== comment.id);
    } catch (error) {
      alert('Failed to delete comment: ' + error);
    }
  }
}

async function handleCreateComment() {
  if (!newComment.value.videoId || !newComment.value.text) {
    alert("Video ID and Comment text are required");
    return;
  }
  const created = await apiCreateComment(newComment.value.videoId, newComment.value.text, "Admin");
  if (created) {
    showCreateModal.value = false;
    newComment.value = { videoId: '', text: '' };
    fetchComments(); // Refresh list
  } else {
    alert("Failed to create comment");
  }
}
</script>

<template>
  <div class="flex flex-col h-full relative">
    <div class="flex justify-between items-center mb-4">
      <h3 class="text-lg font-semibold text-gray-800">Manage Comments</h3>
      <div class="space-x-4 flex items-center">
        <select v-model="filterBy" class="bg-white border border-gray-300 rounded px-3 py-2 text-sm text-gray-700 shadow-sm focus:outline-none focus:ring-blue-500 focus:border-blue-500">
          <option value="all">Group By: None</option>
          <option value="videoId">Group By: Video ID</option>
          <option value="parentCommentId">Group By: Parent Comment ID</option>
        </select>
        <button @click="showCreateModal = true" class="px-4 py-2 bg-blue-600 text-white rounded hover:bg-blue-700 transition">
          Create Comment
        </button>
        <button v-if="hasDirty" @click="saveChanges" class="px-4 py-2 bg-green-600 text-white rounded hover:bg-green-700">
          Save All Changes
        </button>
      </div>
    </div>

    <!-- Create Comment Modal -->
    <div v-if="showCreateModal" class="absolute inset-0 bg-black bg-opacity-50 flex items-center justify-center z-10 p-4">
      <div class="bg-white p-6 rounded-lg shadow-xl w-full max-w-md relative">
        <button @click="showCreateModal = false" class="absolute -top-4 -right-4 bg-gray-300 text-black px-3 py-1 rounded-full hover:bg-red-500 hover:text-white transition shadow z-20">X</button>
        <h4 class="text-xl font-bold mb-4 text-gray-800">Add New Comment</h4>
        <form @submit.prevent="handleCreateComment" class="space-y-4">
          <div>
            <label class="block text-sm text-gray-900 mb-1">Video ID</label>
            <input v-model="newComment.videoId" type="text" required class="w-full border border-gray-300 p-2 rounded focus:ring focus:ring-blue-300 text-gray-900 bg-white">
          </div>
          <div>
            <label class="block text-sm text-gray-900 mb-1">Comment Text</label>
            <textarea v-model="newComment.text" required rows="3" class="w-full border border-gray-300 p-2 rounded focus:ring focus:ring-blue-300 text-gray-900 bg-white"></textarea>
          </div>
          <div class="flex justify-end space-x-2 mt-4 pt-2">
            <button type="button" @click="showCreateModal = false" class="px-4 py-2 bg-gray-300 rounded hover:bg-gray-400 text-black">Cancel</button>
            <button type="submit" class="px-4 py-2 bg-blue-600 text-white rounded hover:bg-blue-700">Create</button>
          </div>
        </form>
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