import { createRouter, createWebHistory } from 'vue-router';
import Login from '../pages/Login.vue';
import AdminDashboard from '../pages/AdminDashboard.vue';
import Logout from '../pages/Logout.vue';

const router = createRouter({
  history: createWebHistory('/'),
  routes: [
    {
      path: '/login',
      name: 'Login',
      component: Login,
    },
    {
      path: '/admin',
      name: 'Admin',
      component: AdminDashboard,
      meta: { requiresAuth: false }, // Set to true if auth enforcement is needed
    },
    {
      path: '/logout',
      name: 'Logout',
      component: Logout,
    },
    {
      path: '/:catchAll(.*)',
      redirect: '/admin',
    },
  ],
});

export default router;