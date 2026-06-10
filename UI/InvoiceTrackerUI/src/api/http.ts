// axios instance with the base URL from env and JWT injected on every request
// the 401 interceptor redirects to login, lazy import avoids a circular dependency with the router
// TODO move token to httpOnly cookie so XSS cant steal it
import axios from 'axios'

const http = axios.create({ baseURL: import.meta.env.VITE_API_URL })

http.interceptors.request.use(config => {
  const token = localStorage.getItem('jwt_token')
  if (token) config.headers.Authorization = `Bearer ${token}`
  return config
})

// lazy import avoids circular dependency: router → authStore → authApi → http
http.interceptors.response.use(
  res => res,
  err => {
    if (err.response?.status === 401) {
      localStorage.removeItem('jwt_token')
      localStorage.removeItem('jwt_user')
      import('../router/index').then(m => m.default.push({ name: 'Login' }))
    }
    return Promise.reject(err)
  }
)

export default http

/**
 * Extracts a user-facing error message from an Axios error.
 * Handles ASP.NET Core ValidationProblemDetails, plain { message } objects, and network failures.
 */
export function extractApiError(err: unknown, fallback = 'An unexpected error occurred.'): string {
  if (!err || typeof err !== 'object') return fallback
  const e = err as any
  const data = e?.response?.data

  if (data) {
    // ASP.NET Core ModelState: { errors: { Field: ["msg"] } }
    if (data.errors && typeof data.errors === 'object') {
      const msgs = Object.values(data.errors as Record<string, string[]>).flat()
      if (msgs.length) return msgs.join(' ')
    }
    // Plain message
    if (typeof data.message === 'string' && data.message) return data.message
    if (typeof data === 'string' && data) return data
  }

  // Network / timeout
  if (e?.message) return e.message

  return fallback
}
