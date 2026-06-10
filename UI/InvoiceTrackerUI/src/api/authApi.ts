// raw auth API calls, no state management here thats the stores job
// refresh is called automatically by the auth store before the token expires
import http from './http'

export interface AuthResponse {
  token: string
  refreshToken: string
  user: { id: number; email: string; name: string }
}

export async function login(email: string, password: string): Promise<AuthResponse> {
  const { data } = await http.post<AuthResponse>('/api/auth/login', { email, password })
  return data
}

export async function register(name: string, email: string, password: string): Promise<AuthResponse> {
  const { data } = await http.post<AuthResponse>('/api/auth/register', { name, email, password })
  return data
}

export async function refresh(refreshToken: string): Promise<AuthResponse> {
  const { data } = await http.post<AuthResponse>('/api/auth/refresh', { refreshToken })
  return data
}

export async function logout(refreshToken: string): Promise<void> {
  await http.post('/api/auth/logout', { refreshToken })
}

export async function forgotPassword(email: string): Promise<void> {
  await http.post('/api/auth/forgot-password', { email })
}

export async function resetPassword(email: string, token: string, newPassword: string): Promise<void> {
  await http.post('/api/auth/reset-password', { email, token, newPassword })
}
