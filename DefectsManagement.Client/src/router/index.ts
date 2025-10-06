import { createRouter, createWebHistory } from 'vue-router'
import { useAuthStore } from '@/stores/authStore';
import LoginView from '@/views/LoginView.vue';
import HomeView from '@/views/HomeView.vue';

const router = createRouter({
  history: createWebHistory(import.meta.env.BASE_URL),
  routes: [
    { path: '/', name: 'home', component: HomeView, meta: { requiresAuth: true } },
    { path: '/login', name: 'login', component: LoginView, meta: { guestOnly: true } },
  ],
});

router.beforeEach(async (to) => {
  const auth = useAuthStore();
  
  if (auth.isPostLogoutRedirect) {
    if (to.name !== 'login') {
      return { name: 'login', replace: true };
    }
    return true;
  }

  if (!auth.isInitialized) {
    await auth.initialize();
  }

  const needAuth = Boolean(to.meta?.requiresAuth);
  const guestOnly = Boolean(to.meta?.guestOnly);

  if (needAuth && !auth.isAuthenticated) {
    return { name: 'login', query: { redirect: to.fullPath }, replace: true };
  }

  if (guestOnly && auth.isAuthenticated) {
    return { name: 'home', replace: true };
  }

  return true;
});

export default router;
