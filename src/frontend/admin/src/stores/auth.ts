import { defineStore } from 'pinia';
import { ref, computed } from 'vue';

export const useAuthStore = defineStore('auth', () => {
  const token = ref<string | null>(localStorage.getItem('adminToken'));
  const userRole = ref<string | null>(localStorage.getItem('adminUserRole'));

  const isAuthenticated = computed(() => !!token.value);
  const user = computed(() => isAuthenticated.value ? { username: 'Admin', token: token.value! } : null);

  function login(newToken: string, role: string = 'Admin') {
    token.value = newToken;
    userRole.value = role;
    localStorage.setItem('adminToken', newToken);
    localStorage.setItem('adminUserRole', role);
  }

  function logout() {
    token.value = null;
    userRole.value = null;
    localStorage.removeItem('adminToken');
    localStorage.removeItem('adminUserRole');
  }

  return { token, isAuthenticated, user, userRole, login, logout };
});