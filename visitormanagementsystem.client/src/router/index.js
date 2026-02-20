// src/router/index.js
import { createRouter, createWebHistory } from 'vue-router'
import VisitorRegistration from '@/components/VisitorRegistration.vue'
import AdminDashboard from '@/components/AdminDashboard.vue' // Import your new page
import SecurityScanner from '@/components/SecurityScanner.vue'

const routes = [
  {
    path: '/',
    name: 'Home',
    component: VisitorRegistration
  },
  {
    path: '/admin',
    name: 'Admin',
    component: AdminDashboard
  },
  {
    path: '/scan',
    name: 'Security',
    component: SecurityScanner
  },
]

const router = createRouter({
  history: createWebHistory(),
  routes
})

export default router
