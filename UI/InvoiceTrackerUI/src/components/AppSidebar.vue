<template>
  <aside :class="['sidebar', { 'sidebar--collapsed': collapsed }]">

    <!-- Toggle button — sits outside the clipped content area -->
    <button class="collapse-btn" @click="collapsed = !collapsed" :title="collapsed ? 'Expand' : 'Collapse'">
      {{ collapsed ? '›' : '‹' }}
    </button>

    <!-- Clipped content wrapper -->
    <div class="sidebar-inner">
      <!-- Brand -->
      <div class="brand">
        <span class="brand-logo">⬡</span>
        <div v-if="!collapsed" class="brand-text">
          <p class="brand-name">TALLY</p>
          <p class="brand-tier">Small Business Tier</p>
        </div>
      </div>

      <!-- Nav -->
      <nav class="nav">
        <router-link to="/" class="nav-item" exact-active-class="nav-item--active" :title="collapsed ? 'Dashboard' : ''">
          <span class="nav-icon">⊞</span>
          <span v-if="!collapsed" class="nav-label">Dashboard</span>
        </router-link>
        <router-link to="/invoices" class="nav-item" active-class="nav-item--active" :title="collapsed ? 'Invoices' : ''">
          <span class="nav-icon">◧</span>
          <span v-if="!collapsed" class="nav-label">Invoices</span>
        </router-link>
        <router-link to="/clients" class="nav-item" active-class="nav-item--active" :title="collapsed ? 'Clients' : ''">
          <span class="nav-icon">◎</span>
          <span v-if="!collapsed" class="nav-label">Clients</span>
        </router-link>
        <router-link to="/reports" class="nav-item" active-class="nav-item--active" :title="collapsed ? 'Reports' : ''">
          <span class="nav-icon">▦</span>
          <span v-if="!collapsed" class="nav-label">Reports</span>
        </router-link>
      </nav>

      <div class="spacer" />

      <!-- Footer -->
      <div class="sidebar-footer">
        <div class="user-row">
          <span class="avatar" :title="collapsed ? (auth.user?.name || '') : ''">{{ initials }}</span>
          <template v-if="!collapsed">
            <div class="user-info">
              <p class="user-name">{{ auth.user?.name || 'User' }}</p>
              <p class="user-email">{{ auth.user?.email }}</p>
            </div>
            <button class="btn-logout" @click="logout" title="Sign out">⏻</button>
          </template>
        </div>
        <button v-if="collapsed" class="btn-logout-icon" @click="logout" title="Sign out">⏻</button>
        <router-link to="/invoices/new" class="btn-new-invoice" :title="collapsed ? 'New Invoice' : ''">
          <span>+</span>
          <span v-if="!collapsed"> New Invoice</span>
        </router-link>
      </div>
    </div>
  </aside>
</template>

<script setup lang="ts">
import { ref, computed } from 'vue'
import { useRouter } from 'vue-router'
import { useAuthStore } from '@/stores/authStore'

const auth      = useAuthStore()
const router    = useRouter()
const collapsed = ref(false)

const initials = computed(() => {
  const name = auth.user?.name || auth.user?.email || ''
  return name.split(' ').map((w: string) => w[0]).join('').toUpperCase().slice(0, 2)
})

async function logout() {
  await auth.logout()
  router.push('/login')
}
</script>

<style scoped>
.sidebar {
  width: 220px;
  flex-shrink: 0;
  background: #fff;
  border-right: 1px solid #e0e0e0;
  display: flex;
  flex-direction: column;
  height: 100vh;
  position: sticky;
  top: 0;
  transition: width .2s ease;
  overflow: visible;
}

.sidebar--collapsed { width: 64px; }

.sidebar-inner {
  display: flex;
  flex-direction: column;
  flex: 1;
  padding: 24px 16px;
  overflow: hidden;
}

.sidebar--collapsed .sidebar-inner {
  padding: 24px 10px;
}

/* Toggle button */
.collapse-btn {
  position: absolute;
  top: 20px;
  right: -12px;
  width: 24px;
  height: 24px;
  background: #fff;
  border: 1px solid #e0e0e0;
  border-radius: 50%;
  color: #666;
  font-size: 14px;
  line-height: 1;
  cursor: pointer;
  display: flex;
  align-items: center;
  justify-content: center;
  z-index: 10;
  transition: color .15s, border-color .15s;
  box-shadow: 0 1px 4px rgba(0,0,0,.08);
}
.collapse-btn:hover { color: #1a1a1a; border-color: #1a1a1a; }

/* Brand */
.brand {
  display: flex;
  align-items: center;
  gap: 10px;
  padding: 0 4px;
  margin-bottom: 32px;
  margin-top: 8px;
  min-width: 0;
}
.brand-logo {
  font-size: 22px;
  color: #1e293b;
  line-height: 1;
  flex-shrink: 0;
}
.brand-name {
  font-size: 14px;
  font-weight: 700;
  letter-spacing: .5px;
  color: #1a1a1a;
  white-space: nowrap;
}
.brand-tier {
  font-size: 10px;
  color: #999;
  letter-spacing: .3px;
  white-space: nowrap;
}

/* Nav */
.nav {
  display: flex;
  flex-direction: column;
  gap: 2px;
}
.nav-item {
  display: flex;
  align-items: center;
  gap: 10px;
  padding: 9px 12px;
  border-radius: 7px;
  color: #666;
  font-size: 13px;
  text-decoration: none;
  transition: color .15s, background .15s;
  white-space: nowrap;
  min-width: 0;
}
.sidebar--collapsed .nav-item {
  padding: 9px;
  justify-content: center;
}
.nav-item:hover { color: #1a1a1a; background: #f5f5f5; }
.nav-item--active {
  color: #1a1a1a;
  background: #f0f0f0;
  border-left: 3px solid #1e293b;
  padding-left: 9px;
}
.sidebar--collapsed .nav-item--active {
  border-left: none;
  padding-left: 9px;
  border-bottom: 2px solid #1e293b;
}
.nav-icon {
  font-size: 14px;
  width: 18px;
  text-align: center;
  flex-shrink: 0;
}
.nav-label { overflow: hidden; }

.spacer { flex: 1; }

/* Footer */
.sidebar-footer {
  display: flex;
  flex-direction: column;
  gap: 10px;
}

.user-row {
  display: flex;
  align-items: center;
  gap: 8px;
  padding: 8px;
  border-radius: 8px;
  background: #f9f9f9;
  border: 1px solid #e0e0e0;
  min-width: 0;
}
.sidebar--collapsed .user-row {
  justify-content: center;
  padding: 6px;
}
.avatar {
  width: 30px;
  height: 30px;
  border-radius: 50%;
  background: #1e293b;
  border: 1px solid #cbd5e1;
  display: flex;
  align-items: center;
  justify-content: center;
  font-size: 10px;
  font-weight: 700;
  color: #fff;
  flex-shrink: 0;
}
.user-info {
  flex: 1;
  min-width: 0;
}
.user-name {
  font-size: 11px;
  font-weight: 600;
  color: #1a1a1a;
  white-space: nowrap;
  overflow: hidden;
  text-overflow: ellipsis;
}
.user-email {
  font-size: 10px;
  color: #999;
  white-space: nowrap;
  overflow: hidden;
  text-overflow: ellipsis;
}
.btn-logout {
  background: none;
  border: none;
  color: #ccc;
  font-size: 13px;
  padding: 2px 4px;
  flex-shrink: 0;
  cursor: pointer;
  transition: color .15s;
}
.btn-logout:hover { color: #ef4444; }

.btn-logout-icon {
  background: none;
  border: none;
  color: #ccc;
  font-size: 14px;
  padding: 6px;
  cursor: pointer;
  border-radius: 6px;
  transition: color .15s, background .15s;
  align-self: center;
}
.btn-logout-icon:hover { color: #ef4444; background: #fef2f2; }

.btn-new-invoice {
  display: flex;
  align-items: center;
  justify-content: center;
  gap: 4px;
  padding: 10px;
  background: #1e293b;
  border: none;
  border-radius: 7px;
  color: #fff;
  font-size: 12px;
  font-weight: 700;
  text-decoration: none;
  letter-spacing: .3px;
  transition: background .15s;
  white-space: nowrap;
}
.btn-new-invoice:hover { background: #0f172a; }
</style>
