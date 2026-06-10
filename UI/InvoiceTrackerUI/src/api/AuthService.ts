import { ServiceBase } from './ServiceBase'

export interface AuthResponse {
  token: string
  user: { id: number; email: string; name: string }
}

class AuthService extends ServiceBase {
  login(email: string, password: string): Promise<AuthResponse> {
    return this.post<AuthResponse>('/api/auth/login', { email, password })
  }

  register(name: string, email: string, password: string): Promise<AuthResponse> {
    return this.post<AuthResponse>('/api/auth/register', { name, email, password })
  }

  forgotPassword(email: string): Promise<void> {
    return this.post<void>('/api/auth/forgot-password', { email })
  }

  resetPassword(email: string, token: string, newPassword: string): Promise<void> {
    return this.post<void>('/api/auth/reset-password', { email, token, newPassword })
  }
}

export default new AuthService()
