import { createRouter, createWebHistory, RouteRecordRaw } from 'vue-router'
import { useUserStore } from '../stores/user'
import AdminLayout from '../components/AdminLayout.vue'

const routes: RouteRecordRaw[] = [
  {
    path: '/login',
    name: 'Login',
    component: () => import('../views/login/Login.vue'),
    meta: { requiresAuth: false }
  },
  {
    path: '/',
    redirect: '/dashboard'
  },
  {
    path: '/',
    component: AdminLayout,
    meta: { requiresAuth: true },
    children: [
      {
        path: 'dashboard',
        name: 'Dashboard',
        component: () => import('../views/admin/Dashboard.vue'),
        meta: { requiresAuth: true, isAdmin: true }
      },
      {
        path: 'users',
        name: 'Users',
        component: () => import('../views/admin/Users.vue'),
        meta: { requiresAuth: true, isAdmin: true }
      },
      {
        path: 'channels',
        name: 'Channels',
        component: () => import('../views/admin/Channels.vue'),
        meta: { requiresAuth: true, isAdmin: true }
      },
      {
        path: 'alerts',
        name: 'Alerts',
        component: () => import('../views/admin/Alerts.vue'),
        meta: { requiresAuth: true, isAdmin: true }
      },
      {
        path: 'settings',
        name: 'Settings',
        component: () => import('../views/admin/Settings.vue'),
        meta: { requiresAuth: true, isAdmin: true }
      },
      {
        path: 'sms',
        name: 'SmsSend',
        component: () => import('../views/user/SmsSend.vue'),
        meta: { requiresAuth: true }
      },
      {
        path: 'sms/history',
        name: 'SmsHistory',
        component: () => import('../views/user/SmsHistory.vue'),
        meta: { requiresAuth: true }
      }
    ]
  }
]

const router = createRouter({
  history: createWebHistory(),
  routes
})

router.beforeEach((to, from, next) => {
  const userStore = useUserStore()
  
  if (to.meta.requiresAuth && !userStore.isLoggedIn) {
    next('/login')
  } else if (to.meta.isAdmin && !userStore.isAdmin) {
    next('/sms')
  } else {
    next()
  }
})

export default router
