import { describe, it, expect, beforeEach, vi } from 'vitest'
import { setActivePinia, createPinia } from 'pinia'
import { useAuthStore } from '../../stores/authStore'
import * as authApi from '../../api/authApi'

vi.mock('../../api/authApi')

const mockToken = 'mock.jwt.token'
const mockUser  = { id: 1, email: 'alice@test.com', name: 'Alice' }
const mockAuth  = { token: mockToken, refreshToken: 'mock.refresh.token', user: mockUser }

describe('authStore', () => {
  beforeEach(() => {
    setActivePinia(createPinia())
    localStorage.clear()
    vi.clearAllMocks()
  })

  it('starts unauthenticated with no stored token', () => {
    const store = useAuthStore()
    expect(store.isAuthenticated).toBe(false)
    expect(store.user).toBeNull()
  })

  it('login sets token and user', async () => {
    vi.mocked(authApi.login).mockResolvedValue(mockAuth)
    const store = useAuthStore()

    await store.login('alice@test.com', 'password123')

    expect(store.isAuthenticated).toBe(true)
    expect(store.token).toBe(mockToken)
    expect(store.user).toEqual(mockUser)
    expect(localStorage.getItem('jwt_token')).toBe(mockToken)
  })

  it('register sets token and user', async () => {
    vi.mocked(authApi.register).mockResolvedValue(mockAuth)
    const store = useAuthStore()

    await store.register('Alice', 'alice@test.com', 'password123')

    expect(store.isAuthenticated).toBe(true)
    expect(store.user?.name).toBe('Alice')
  })

  it('logout clears token, user, and localStorage', async () => {
    vi.mocked(authApi.login).mockResolvedValue(mockAuth)
    vi.mocked(authApi.logout).mockResolvedValue(undefined)
    const store = useAuthStore()
    await store.login('alice@test.com', 'password123')

    await store.logout()

    expect(store.isAuthenticated).toBe(false)
    expect(store.token).toBeNull()
    expect(store.user).toBeNull()
    expect(localStorage.getItem('jwt_token')).toBeNull()
  })

  it('login propagates API errors', async () => {
    vi.mocked(authApi.login).mockRejectedValue(new Error('Invalid email or password.'))
    const store = useAuthStore()

    await expect(store.login('bad@test.com', 'wrong')).rejects.toThrow('Invalid email or password.')
    expect(store.isAuthenticated).toBe(false)
  })

  it('restores session from localStorage on init', () => {
    localStorage.setItem('jwt_token', mockToken)
    localStorage.setItem('jwt_user', JSON.stringify(mockUser))

    setActivePinia(createPinia())
    const store = useAuthStore()

    expect(store.isAuthenticated).toBe(true)
    expect(store.user).toEqual(mockUser)
  })

  it('isAuthenticated is false when stored token is expired', () => {
    const pastMs = Date.now() - 1000
    localStorage.setItem('jwt_token', mockToken)
    localStorage.setItem('jwt_user', JSON.stringify(mockUser))
    localStorage.setItem('jwt_expiry', String(pastMs))

    setActivePinia(createPinia())
    const store = useAuthStore()

    expect(store.isAuthenticated).toBe(false)
  })

  it('isAuthenticated is true when stored token has not yet expired', () => {
    const futureMs = Date.now() + 60_000
    localStorage.setItem('jwt_token', mockToken)
    localStorage.setItem('jwt_user', JSON.stringify(mockUser))
    localStorage.setItem('jwt_expiry', String(futureMs))

    setActivePinia(createPinia())
    const store = useAuthStore()

    expect(store.isAuthenticated).toBe(true)
  })
})
