import { defineStore } from 'pinia'
import { ref, computed } from 'vue'
import * as authApi from '../api/authApi'

const TOKEN_KEY        = 'jwt_token'
const REFRESH_KEY      = 'jwt_refresh'
const USER_KEY         = 'jwt_user'
const EXPIRY_KEY       = 'jwt_expiry'

// pinia store for auth state, persists to localStorage so the session survives page refresh
// the silent refresh timer fires 1 minute before the JWT expires so the user never gets logged out mid session
export const useAuthStore = defineStore('auth', () => {
  const token        = ref<string | null>(localStorage.getItem(TOKEN_KEY))
  const refreshToken = ref<string | null>(localStorage.getItem(REFRESH_KEY))
  const user         = ref<{ id: number; email: string; name: string } | null>(
    JSON.parse(localStorage.getItem(USER_KEY) || 'null')
  )
  const expiresAt    = ref<number | null>(
    localStorage.getItem(EXPIRY_KEY) ? parseInt(localStorage.getItem(EXPIRY_KEY)!) : null
  )

  // null expiresAt means no expiry info (e.g. token issued before this field existed) — treated as valid
  const isAuthenticated = computed(() =>
    !!token.value && (expiresAt.value === null || Date.now() < expiresAt.value)
  )

  let refreshTimer: ReturnType<typeof setTimeout> | null = null

  function setSession(res: authApi.AuthResponse) {
    token.value        = res.token
    refreshToken.value = res.refreshToken
    user.value         = res.user

    localStorage.setItem(TOKEN_KEY,   res.token)
    localStorage.setItem(REFRESH_KEY, res.refreshToken)
    localStorage.setItem(USER_KEY,    JSON.stringify(res.user))

    // Parse expiry from JWT payload
    try {
      const payload    = JSON.parse(atob(res.token.split('.')[1]))
      const expMs      = payload.exp * 1000
      expiresAt.value  = expMs
      localStorage.setItem(EXPIRY_KEY, String(expMs))
      scheduleRefresh(expMs)
    } catch {
      // ignore parse errors — token treated as non-expiring
    }
  }

  function clearSession() {
    token.value        = null
    refreshToken.value = null
    user.value         = null
    expiresAt.value    = null
    localStorage.removeItem(TOKEN_KEY)
    localStorage.removeItem(REFRESH_KEY)
    localStorage.removeItem(USER_KEY)
    localStorage.removeItem(EXPIRY_KEY)
    if (refreshTimer) clearTimeout(refreshTimer)
  }

  // Refresh 1 minute before expiry
  function scheduleRefresh(expiresAtMs: number) {
    if (refreshTimer) clearTimeout(refreshTimer)
    const delay = expiresAtMs - Date.now() - 60_000
    if (delay <= 0) {
      silentRefresh()
      return
    }
    refreshTimer = setTimeout(silentRefresh, delay)
  }

  async function silentRefresh() {
    const rt = refreshToken.value
    if (!rt) return
    try {
      const res = await authApi.refresh(rt)
      setSession(res)
    } catch {
      // Refresh token expired or revoked — force logout
      clearSession()
      window.location.href = '/login'
    }
  }

  // Restore session on page load and reschedule refresh timer
  function restoreSession() {
    const expiryStr = localStorage.getItem(EXPIRY_KEY)
    if (!expiryStr || !token.value) return
    const expiresAt = parseInt(expiryStr)
    if (Date.now() >= expiresAt) {
      // Access token already expired — try silent refresh immediately
      silentRefresh()
    } else {
      scheduleRefresh(expiresAt)
    }
  }

  async function login(email: string, password: string) {
    const res = await authApi.login(email, password)
    setSession(res)
  }

  async function register(name: string, email: string, password: string) {
    const res = await authApi.register(name, email, password)
    setSession(res)
  }

  async function forgotPassword(email: string) {
    await authApi.forgotPassword(email)
  }

  async function logout() {
    const rt = refreshToken.value
    if (rt) {
      try { await authApi.logout(rt) } catch { /* best effort */ }
    }
    clearSession()
  }

  // Kick off the refresh timer on store init
  restoreSession()

  return {
    token, refreshToken, user, expiresAt, isAuthenticated,
    login, register, forgotPassword, logout,
  }
})
