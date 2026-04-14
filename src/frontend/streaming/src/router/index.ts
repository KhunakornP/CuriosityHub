import { createRouter, createWebHistory } from 'vue-router'
import LandingPage from '../pages/LandingPage.vue'
import HomePage from '../pages/HomePage.vue'
import VideoStreamingPage from '../pages/VideoStreamingPage.vue'
import VideoUploadPage from '../pages/VideoUploadPage.vue'

const router = createRouter({
  history: createWebHistory(import.meta.env.BASE_URL),
  routes: [
    {
      path: '/',
      name: 'landing',
      component: LandingPage,
    },
    {
      path: '/home',
      name: 'home',
      component: HomePage,
    },
    {
      path: '/watch/:id',
      name: 'watch',
      component: VideoStreamingPage,
    },
    {
      path: '/upload',
      name: 'upload',
      component: VideoUploadPage,
    },
  ],
})

export default router
