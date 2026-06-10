<template>
  <div class="app" :class="{ 'app--sidebar': auth.isAuthenticated }">
    <AppSidebar v-if="auth.isAuthenticated" />
    <div class="app-body">
      <!-- Top bar (search + actions) -->
      <header v-if="auth.isAuthenticated && !isPdfRoute" class="topbar">
        <div class="topbar-search">
          <span class="search-icon">⌕</span>
          <input
            v-model="globalSearch"
            placeholder="Search invoices or clients..."
            class="search-input"
            @keydown.enter="doSearch"
          />
        </div>
        <div class="topbar-right">
          <span class="topbar-avatar">{{ initials }}</span>
        </div>
      </header>

      <main>
        <router-view />
      </main>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, computed } from 'vue'
import { useRouter, useRoute } from 'vue-router'
import { useAuthStore } from './stores/authStore'
import AppSidebar from './components/AppSidebar.vue'

const auth   = useAuthStore()
const router = useRouter()
const route  = useRoute()

const isPdfRoute = computed(() => route.name === 'InvoicePdf' || route.path.includes('/pdf'))

const globalSearch = ref('')

const initials = computed(() => {
  const name = auth.user?.name || auth.user?.email || ''
  return name.split(' ').map((w: string) => w[0]).join('').toUpperCase().slice(0, 2)
})

function doSearch() {
  if (globalSearch.value.trim()) {
    router.push({ path: '/invoices', query: { search: globalSearch.value.trim() } })
    globalSearch.value = ''
  }
}
</script>

<style>
*, *::before, *::after { box-sizing: border-box; margin: 0; padding: 0; }

body {
  background: #f5f5f5;
  color: #1a1a1a;
  font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, 'Helvetica Neue', Arial, sans-serif;
  font-size: 14px;
  line-height: 1.5;
}

a { text-decoration: none; color: inherit; }

/* ── Layout ─────────────────────────────────────────────── */
.app {
  min-height: 100vh;
}
.app--sidebar {
  display: flex;
}
.app-body {
  flex: 1;
  display: flex;
  flex-direction: column;
  min-width: 0;
  background: #f5f5f5;
}

/* ── Top bar ─────────────────────────────────────────────── */
.topbar {
  display: flex;
  align-items: center;
  justify-content: space-between;
  padding: 0 28px;
  height: 56px;
  border-bottom: 1px solid #e0e0e0;
  background: #fff;
  flex-shrink: 0;
}
.topbar-search {
  position: relative;
  width: 320px;
}
.search-icon {
  position: absolute;
  left: 12px;
  top: 50%;
  transform: translateY(-50%);
  color: #999;
  font-size: 16px;
  pointer-events: none;
}
.topbar-search .search-input {
  width: 100%;
  background: #f5f5f5;
  border: 1px solid #e0e0e0;
  border-radius: 6px;
  color: #1a1a1a;
  font-family: inherit;
  font-size: 13px;
  padding: 8px 12px 8px 36px;
  outline: none;
  transition: border-color .15s;
}
.topbar-search .search-input:focus { border-color: #1e293b; }
.topbar-search .search-input::placeholder { color: #999; }

.topbar-right {
  display: flex;
  align-items: center;
  gap: 12px;
}
.topbar-icon-btn {
  background: none;
  border: none;
  color: #666;
  font-size: 16px;
  padding: 6px 8px;
  border-radius: 6px;
  cursor: pointer;
  transition: color .15s, background .15s;
}
.topbar-icon-btn:hover { color: #1a1a1a; background: #f0f0f0; }
.topbar-avatar {
  width: 32px;
  height: 32px;
  border-radius: 50%;
  background: #1e293b;
  border: 1px solid #cbd5e1;
  display: flex;
  align-items: center;
  justify-content: center;
  font-size: 11px;
  font-weight: 700;
  color: #fff;
  cursor: default;
}

main { flex: 1; padding: 28px; overflow-y: auto; }

/* ── Shared utility classes ─────────────────────────────── */
.badge {
  display: inline-flex; align-items: center; gap: 5px;
  padding: 4px 10px; border-radius: 4px;
  font-size: 11px; font-weight: 600; letter-spacing: .5px;
}
.badge::before { content: '●'; font-size: 6px; }
.badge-paid    { background: #d1fae5; color: #065f46; }
.badge-paid::before { color: #10b981; }
.badge-sent    { background: #dbeafe; color: #1e40af; }
.badge-sent::before { color: #3b82f6; }
.badge-overdue { background: #fee2e2; color: #991b1b; }
.badge-overdue::before { color: #ef4444; }
.badge-draft   { background: #f3f4f6; color: #374151; }
.badge-draft::before { color: #9ca3af; }
.badge-cancelled { background: #f3f4f6; color: #6b7280; }
.badge-cancelled::before { color: #9ca3af; }

input, select, textarea {
  background: #fff;
  border: 1px solid #e0e0e0;
  border-radius: 6px;
  color: #1a1a1a;
  font-family: inherit;
  font-size: 14px;
  padding: 10px 12px;
  outline: none;
  width: 100%;
  transition: border-color .15s;
}
input:focus, select:focus, textarea:focus { border-color: #1e293b; }
input::placeholder, textarea::placeholder { color: #999; }

button { font-family: inherit; cursor: pointer; }
</style>
