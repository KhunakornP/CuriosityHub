<script setup lang="ts">
// Admin Users Tab Component
import { ref, onMounted, computed } from 'vue';
import { useAuthStore } from '../../stores/auth';

const authStore = useAuthStore();
const apiBase = import.meta.env.VITE_API_BASE_URL || 'http://localhost:5001';

interface User {
  id: string;
  firstName: string;
  lastName: string;
  email: string;
  role: number;
  description: string;
  _isDirty?: boolean;
}

const users = ref<User[]>([]);
const loading = ref(false);

const roleMap = {
  0: 'Visitor',
  1: 'Researcher',
  2: 'Admin'
};

const showCreateModal = ref(false);
const newUser = ref({ email: '', password: '', role: 0, firstName: '', lastName: '', description: '' });

onMounted(() => {
  fetchUsers();
});

async function fetchUsers() {
  loading.value = true;
  try {
    const res = await fetch(`${apiBase}/admin/users`, {
      headers: { 'Authorization': `Bearer ${authStore.token}` }
    });
    if (!res.ok) throw new Error('Failed to fetch users');
    const data = await res.json();
    users.value = data.map((u: any) => ({ ...u, _isDirty: false }));
  } catch (err) {
    console.error(err);
    alert('Caught error fetching users.');
  } finally {
    loading.value = false;
  }
}

function markDirty(user: User) {
  user._isDirty = true;
}

const hasDirty = computed(() => users.value.some(u => u._isDirty));

async function saveChanges() {
  const dirtyUsers = users.value.filter(u => u._isDirty);
  let savedCount = 0;
  for (const u of dirtyUsers) {
    try {
      const res = await fetch(`${apiBase}/admin/user/${u.id}`, {
        method: 'PUT',
        headers: {
          'Authorization': `Bearer ${authStore.token}`,
          'Content-Type': 'application/json'
        },
        body: JSON.stringify({
          email: u.email,
          role: parseInt(u.role as any, 10),
          firstName: u.firstName,
          lastName: u.lastName,
          description: u.description
        })
      });
      if (res.ok) {
        u._isDirty = false;
        savedCount++;
      }
    } catch (err) {
      console.error(`Failed to completely save user ${u.id}`, err);
    }
  }
  alert(`Saved changes for ${savedCount}/${dirtyUsers.length} users`);
}

async function deleteUser(user: User) {
  if (confirm(`Are you sure you want to delete user ${user.email}?`)) {
    try {
      const res = await fetch(`${apiBase}/admin/user/${user.id}`, {
        method: 'DELETE',
        headers: { 'Authorization': `Bearer ${authStore.token}` }
      });
      if (res.ok) {
        users.value = users.value.filter(u => u.id !== user.id);
      } else {
        alert('Failed to delete user.');
      }
    } catch (err) {
      console.error(err);
      alert('Error deleting user.');
    }
  }
}

async function handleCreateUser() {
  try {
    const res = await fetch(`${apiBase}/admin/user`, {
      method: 'POST',
      headers: {
        'Authorization': `Bearer ${authStore.token}`,
        'Content-Type': 'application/json'
      },
      body: JSON.stringify({ ...newUser.value, role: parseInt(newUser.value.role as any, 10) })
    });
    if (res.ok) {
      const createdUser = await res.json();
      users.value.push({ ...createdUser, _isDirty: false });
      showCreateModal.value = false;
      newUser.value = { email: '', password: '', role: 0, firstName: '', lastName: '', description: '' };
    } else {
      alert('Failed to create user. Email may exist.');
    }
  } catch (err) {
    console.error(err);
    alert('Error creating user');
  }
}
</script>

<template>
  <div class="flex flex-col h-full relative">
    <div class="flex justify-between items-center mb-4">
      <h3 class="text-lg font-semibold text-gray-800">Manage Users</h3>
      <div class="space-x-2">
        <button @click="showCreateModal = true" class="px-4 py-2 bg-blue-600 text-white rounded hover:bg-blue-700">
          Create User
        </button>
        <button v-if="hasDirty" @click="saveChanges" class="px-4 py-2 bg-green-600 text-white rounded hover:bg-green-700">
          Save All Changes
        </button>
      </div>
    </div>

    <!-- Create User Modal -->
    <div v-if="showCreateModal" class="absolute inset-0 bg-black bg-opacity-50 flex items-center justify-center z-10">
      <div class="bg-white p-6 rounded-lg shadow-xl w-96">
        <h4 class="text-xl font-bold mb-4">Create New User</h4>
        <form @submit.prevent="handleCreateUser" class="space-y-4">
          <div><label class="block text-sm text-gray-900">Email</label><input v-model="newUser.email" type="email" required class="w-full border p-2 rounded focus:ring focus:ring-blue-300 text-gray-900 bg-white"></div>
          <div><label class="block text-sm text-gray-900">Password</label><input v-model="newUser.password" type="password" required class="w-full border p-2 rounded focus:ring focus:ring-blue-300 text-gray-900 bg-white"></div>
          <div><label class="block text-sm text-gray-900">First Name</label><input v-model="newUser.firstName" type="text" class="w-full border p-2 rounded focus:ring focus:ring-blue-300 text-gray-900 bg-white"></div>
          <div><label class="block text-sm text-gray-900">Last Name</label><input v-model="newUser.lastName" type="text" class="w-full border p-2 rounded focus:ring focus:ring-blue-300 text-gray-900 bg-white"></div>
          <div><label class="block text-sm text-gray-900">Role <small>(0=Visitor, 1=Researcher, 2=Admin)</small></label>
            <select v-model="newUser.role" class="w-full border p-2 rounded focus:ring focus:ring-blue-300 text-gray-900 bg-white">
              <option :value="0">Visitor</option>
              <option :value="1">Researcher</option>
              <option :value="2">Admin</option>
            </select>
          </div>
          <div class="flex justify-end space-x-2 mt-4">
            <button type="button" @click="showCreateModal = false" class="px-4 py-2 bg-gray-300 rounded hover:bg-gray-400 text-black">Cancel</button>
            <button type="submit" class="px-4 py-2 bg-blue-600 text-white rounded hover:bg-blue-700">Create</button>
          </div>
        </form>
      </div>
    </div>

    <div v-if="loading" class="text-center py-10 text-gray-500">Loading users...</div>

    <div v-else class="overflow-x-auto bg-white border border-gray-200 rounded-lg">
      <table class="min-w-full divide-y divide-gray-200">
        <thead class="bg-gray-50">
          <tr>
            <th scope="col" class="w-8 px-4 py-3 text-center"></th>
            <th scope="col" class="px-3 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">ID</th>
            <th scope="col" class="px-3 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">First Name</th>
            <th scope="col" class="px-3 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">Last Name</th>
            <th scope="col" class="px-3 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">Email</th>
            <th scope="col" class="px-3 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">Role</th>
            <th scope="col" class="px-3 py-3 text-right text-xs font-medium text-gray-500 uppercase tracking-wider">Actions</th>
          </tr>
        </thead>
        <tbody class="bg-white divide-y divide-gray-200">
          <tr v-for="user in users" :key="user.id" class="hover:bg-gray-50 transition-colors">
            <td class="w-8 px-4 py-4 text-center">
              <span v-if="user._isDirty" class="text-yellow-500 h-2 w-2 inline-block rounded-full bg-yellow-500" title="Modified"></span>
            </td>
            <td class="px-3 py-4 whitespace-nowrap text-xs text-gray-500 text-left" :title="user.id">{{ user.id.substring(0,8) }}...</td>
            <td class="px-3 py-4 whitespace-nowrap text-left">
              <input type="text" v-model="user.firstName" @input="markDirty(user)" class="w-full bg-transparent border-b border-transparent focus:border-blue-500 focus:outline-none text-sm font-medium text-gray-900 px-1 py-1" />
            </td>
            <td class="px-3 py-4 whitespace-nowrap text-left">
              <input type="text" v-model="user.lastName" @input="markDirty(user)" class="w-full bg-transparent border-b border-transparent focus:border-blue-500 focus:outline-none text-sm font-medium text-gray-900 px-1 py-1" />
            </td>
            <td class="px-3 py-4 whitespace-nowrap text-left">
              <input type="email" v-model="user.email" @input="markDirty(user)" class="w-full bg-transparent border-b border-transparent focus:border-blue-500 focus:outline-none text-sm text-gray-600 px-1 py-1" />
            </td>
            <td class="px-3 py-4 whitespace-nowrap text-left">
              <select v-model="user.role" @change="markDirty(user)" class="bg-transparent border-b border-gray-300 focus:border-blue-500 focus:outline-none text-sm text-gray-600 px-1 py-1 cursor-pointer pr-4">
                <option :value="0">Visitor</option>
                <option :value="1">Researcher</option>
                <option :value="2">Admin</option>
              </select>
            </td>
            <td class="px-3 py-4 whitespace-nowrap text-right text-sm font-medium">
              <button @click="deleteUser(user)" class="text-red-500 hover:text-red-700 transition" title="Delete User">
                <svg xmlns="http://www.w3.org/2000/svg" class="h-5 w-5 inline" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                  <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M19 7l-.867 12.142A2 2 0 0116.138 21H7.862a2 2 0 01-1.995-1.858L5 7m5 4v6m4-6v6m1-10V4a1 1 0 00-1-1h-4a1 1 0 00-1 1v3M4 7h16" />
                </svg>
              </button>
            </td>
          </tr>
          <tr v-if="users.length === 0">
            <td colspan="7" class="px-6 py-10 text-center text-sm text-gray-500">No users found.</td>
          </tr>
        </tbody>
      </table>
    </div>
  </div>
</template>
