import { createRouter, createWebHistory } from 'vue-router'
import { useAuthStore } from '../stores/authStore'

const routes = [
  { path: '/login', name: 'Login', component: () => import('../views/LoginView.vue'), meta: { public: true } },
  { path: '/',                   name: 'Dashboard',     component: () => import('../views/DashboardView.vue') },
  { path: '/invoices',           name: 'InvoiceList',   component: () => import('../views/InvoiceListView.vue') },
  { path: '/invoices/new',       name: 'InvoiceCreate', component: () => import('../views/InvoiceFormView.vue') },
  { path: '/invoices/:id',       name: 'InvoiceDetail', component: () => import('../views/InvoiceDetailView.vue') },
  { path: '/invoices/:id/edit',  name: 'InvoiceEdit',   component: () => import('../views/InvoiceFormView.vue') },
  { path: '/invoices/:id/pdf',   name: 'InvoicePdf',    component: () => import('../views/InvoicePdfView.vue') },
  { path: '/clients',            name: 'Clients',       component: () => import('../views/ClientsView.vue') },
  { path: '/reports',            name: 'Reports',       component: () => import('../views/ReportsView.vue') },
]

const router = createRouter({ history: createWebHistory(), routes })

router.beforeEach((to) => {
  const auth = useAuthStore()
  if (!to.meta.public && !auth.isAuthenticated) {
    return { name: 'Login' }
  }
  if (to.name === 'Login' && auth.isAuthenticated) {
    return { name: 'Dashboard' }
  }
})

export default router
