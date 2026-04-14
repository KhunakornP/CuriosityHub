import { defineStore } from 'pinia';
import { ref } from 'vue';

export const useAuthStore = defineStore('auth', () => {
  const isAuthenticated = ref(false);
  const user = ref<{ username: string; token: string } | null>(null);

  function login(username: string, token: string = 'mock-token-xyz') {
    isAuthenticated.value = true;
    user.value = { username, token };
  }

  function logout() {
    isAuthenticated.value = false;
    user.value = null;
  }

  return { isAuthenticated, user, login, logout };
});