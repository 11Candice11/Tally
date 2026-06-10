import { describe, it, expect, vi, beforeEach } from 'vitest'
import authService from '../../api/AuthService'
import http from '../../api/http'

vi.mock('../../api/http', () => ({
  default: { post: vi.fn(), get: vi.fn(), put: vi.fn(), patch: vi.fn(), delete: vi.fn() },
  extractApiError: vi.fn((e: unknown) => (e as Error).message ?? 'Error'),
}))

const mockToken = 'mock.jwt.token'
const mockUser  = { id: 1, email: 'alice@test.com', name: 'Alice' }
const mockRes   = { token: mockToken, user: mockUser }

describe('AuthService', () => {
  beforeEach(() => vi.clearAllMocks())

  it('login posts to /api/auth/login and returns AuthResponse', async () => {
    vi.mocked(http.post).mockResolvedValue({ data: mockRes })

    const result = await authService.login('alice@test.com', 'password123')

    expect(http.post).toHaveBeenCalledWith('/api/auth/login', { email: 'alice@test.com', password: 'password123' })
    expect(result).toEqual(mockRes)
  })

  it('register posts to /api/auth/register and returns AuthResponse', async () => {
    vi.mocked(http.post).mockResolvedValue({ data: mockRes })

    const result = await authService.register('Alice', 'alice@test.com', 'password123')

    expect(http.post).toHaveBeenCalledWith('/api/auth/register', { name: 'Alice', email: 'alice@test.com', password: 'password123' })
    expect(result).toEqual(mockRes)
  })

  it('forgotPassword posts to /api/auth/forgot-password', async () => {
    vi.mocked(http.post).mockResolvedValue({ data: undefined })

    await authService.forgotPassword('alice@test.com')

    expect(http.post).toHaveBeenCalledWith('/api/auth/forgot-password', { email: 'alice@test.com' })
  })

  it('resetPassword posts to /api/auth/reset-password', async () => {
    vi.mocked(http.post).mockResolvedValue({ data: undefined })

    await authService.resetPassword('alice@test.com', 'tok123', 'newpass123')

    expect(http.post).toHaveBeenCalledWith('/api/auth/reset-password', {
      email: 'alice@test.com', token: 'tok123', newPassword: 'newpass123',
    })
  })

  it('throws a plain Error with the API message on failure', async () => {
    vi.mocked(http.post).mockRejectedValue(new Error('Invalid email or password.'))

    await expect(authService.login('bad@test.com', 'wrong'))
      .rejects.toThrow('Invalid email or password.')
  })
})
