import { createRouter, createWebHistory } from 'vue-router'
import SubmitFeedbackView from '../views/SubmitFeedbackView.vue'
import QueueView from '../views/QueueView.vue'
import DashboardView from '../views/DashboardView.vue'

const routes = [
  {
    path: '/',
    name: 'submit',
    component: SubmitFeedbackView
  },
  {
    path: '/queue',
    name: 'queue',
    component: QueueView
  },
  {
    path: '/dashboard',
    name: 'dashboard',
    component: DashboardView
  }
]

const router = createRouter({
  history: createWebHistory(),
  routes
})

export default router
