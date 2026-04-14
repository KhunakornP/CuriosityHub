<script setup lang="ts">
import { ref, onMounted, computed } from 'vue';

interface User {
  id: string;
  username: string;
  email: string;
  role: string;
  _isDirty?: boolean;
}

const users = ref<User[]>([
  { id: 'u1', username: 'admin_master', email: 'admin@curiosityhub.com', role: 'Admin' },
  { id: 'u2', username: 'john_doe', email: 'john@example.com', role: 'User' },
  { id: 'u3', username: 'moderator_x', email: 'mod@example.com', role: 'Moderator' },
]);
const loading = ref(false);
const rolesFromApi = ref<string[]>([]);

onMounted(() => {
  // Mock API for roles
  setTimeout(() => {
    rolesFromApi.value = ['Admin', 'User', 'Moderator', 'Banned'];
  }, 200);
});

function markDirty(user: User) {
  user._isDirty = true;
}

const hasDirty = computed(() => users.value.some(u => u._isDirty));

function saveChanges() {
  const dirtyUsers = users.value.filter(u => u._isDirty);
  alert(`Saved changes for ${dirtyUsers.length} users`);
  dirtyUsers.forEach(u => u._isDirty = false);
}

function deleteUser(user: User) {
  if (confirm(`Are you sure you want to delete user ${user.username}?`)) {
    users.value = users.value.filter(u => u.id !== user.id);
  }
}
</script>

<template>
  <div class="flex flex-col h-full">
    <div class="flex justify-between items-center mb-4">
      <h3 class="text-lg font-semibold text-gray-800">Manage Users</h3>
      <div class="space-x-2">
        <button v-if="hasDirty" @click="saveChanges" class="px-4 py-2 bg-green-600 text-white rounded hover:bg-green-700">
          Save All Changes
        </button>
      </div>
    </div>

    <div v-if="loading" class="text-center py-10 text-gray-500">Loading users...</div>

    <div v-else class="overflow-x-auto bg-white border border-gray-200 rounded-lg">
      <table class="min-w-full divide-y divide-gray-200">
        <thead class="bg-gray-50">
          <tr>
            <th scope="col" class="w-8 px-4 py-3 text-center"></th>
            <th scope="col" class="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">ID</th>
            <th scope="col" class="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">Username</th>
            <th scope="col" class="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">Email</th>
            <th scope="col" class="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">Role</th>
            <th scope="col" class="px-6 py-3 text-right text-xs font-medium text-gray-500 uppercase tracking-wider">Actions</th>
          </tr>
        </thead>
        <tbody class="bg-white divide-y divide-gray-200">
          <tr v-for="user in users" :key="user.id" class="hover:bg-gray-50 transition-colors">
            <td class="w-8 px-4 py-4 text-center">
              <span v-if="user._isDirty" class="text-yellow-500 h-2 w-2 inline-block rounded-full bg-yellow-500" title="Modified"></span>
            </td>
            <td class="px-6 py-4 whitespace-nowrap text-sm text-gray-500">{{ user.id }}</td>
            <td class="px-6 py-4 whitespace-nowrap">
              <input type="text" v-model="user.username" @input="markDirty(user)" class="w-full bg-transparent border-b border-transparent focus:border-blue-500 focus:outline-none text-sm font-medium text-gray-900 px-1 py-1" />
            </td>
            <td class="px-6 py-4 whitespace-nowrap">
              <input type="email" v-model="user.email" @input="markDirty(user)" class="w-full bg-transparent border-b border-transparent focus:border-blue-500 focus:outline-none text-sm text-gray-600 px-1 py-1" />
            </td>
            <td class="px-6 py-4 whitespace-nowrap">
              <select v-model="user.role" @change="markDirty(user)" class="bg-transparent border-b border-gray-300 focus:border-blue-500 focus:outline-none text-sm text-gray-600 px-1 py-1 cursor-pointer">
                <option v-for="role in rolesFromApi" :key="role" :value="role">{{ role }}</option>
              </select>
            </td>
            <td class="px-6 py-4 whitespace-nowrap text-right text-sm font-medium">
              <button @click="deleteUser(user)" class="text-red-500 hover:text-red-700 transition" title="Delete User">
                <svg xmlns="http://www.w3.org/2000/svg" class="h-5 w-5 inline" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                  <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M19 7l-.867 12.142A2 2 0 0116.138 21H7.862a2 2 0 01-1.995-1.858L5 7m5 4v6m4-6v6m1-10V4a1 1 0 00-1-1h-4a1 1 0 00-1 1v3M4 7h16" />
                </svg>
              </button>
            </td>
          </tr>
          <tr v-if="users.length === 0">
            <td colspan="6" class="px-6 py-10 text-center text-sm text-gray-500">No users found.</td>
          </tr>
        </tbody>
      </table>
    </div>
  </div>
</template>