import { createRouter, createWebHistory, RouteRecordRaw } from 'vue-router';
import { useAuthStore } from '../stores/authStore';
import LoginView from '../views/LoginView.vue';
import HomeView from '../views/HomeView.vue';
import DefectDetailView from '../views/DefectDetailView.vue';
import DefectEditView from '../views/DefectEditView.vue';
import SandboxDefectsProjects from '@/views/SandboxDefectsProjects.vue';

const routes: RouteRecordRaw[] = [
  {
    path: '/login',
    name: 'login',
    component: LoginView,
    meta: { guestOnly: true }
  },
  {
    path: '/',
    name: 'home',
    component: HomeView,
    meta: { requiresAuth: true }
  },
  {
    path: '/defects/:id',
    name: 'defect-detail',
    component: DefectDetailView,
    meta: { requiresAuth: true }
  },
  {
    path: '/defects/:id/edit',
    name: 'defect-edit',
    component: DefectEditView,
    meta: { requiresAuth: true }
  },
  {
    path: '/sandbox',
    name: 'sandbox',
    component: SandboxDefectsProjects,
    meta: { requiresAuth: true }
  }  
];

const router = createRouter({
  history: createWebHistory(),
  routes
});

router.beforeEach(async (to, _from, next) => {
  const authStore = useAuthStore();

  if (authStore.isPostLogoutRedirect && to.name !== 'login') {
    return next({ name: 'login', replace: true });
  }

  if (!authStore.isInitialized) {
    await authStore.initialize();
  }

  if (to.meta.requiresAuth && !authStore.isAuthenticated) {
    return next({
      name: 'login',
      query: { redirect: to.fullPath },
      replace: true
    });
  }

  if (to.meta.guestOnly && authStore.isAuthenticated) {
    return next({ path: '/', replace: true });
  }

  next();
});

export default router;
