import http, { extractApiError } from './http'

// base class for all API services, wraps axios and converts errors to plain Error objects
// all services extend this so error handling is consistant everywhere
export abstract class ServiceBase {
  protected async get<T>(url: string, params?: Record<string, string>): Promise<T> {
    try {
      const { data } = await http.get<T>(url, { params })
      return data
    } catch (e) {
      throw new Error(extractApiError(e))
    }
  }

  protected async post<T>(url: string, body?: unknown): Promise<T> {
    try {
      const { data } = await http.post<T>(url, body)
      return data
    } catch (e) {
      throw new Error(extractApiError(e))
    }
  }

  protected async put<T>(url: string, body?: unknown): Promise<T> {
    try {
      const { data } = await http.put<T>(url, body)
      return data
    } catch (e) {
      throw new Error(extractApiError(e))
    }
  }

  protected async patch<T>(url: string, body?: unknown): Promise<T> {
    try {
      const { data } = await http.patch<T>(url, body)
      return data
    } catch (e) {
      throw new Error(extractApiError(e))
    }
  }

  protected async delete(url: string): Promise<void> {
    try {
      await http.delete(url)
    } catch (e) {
      throw new Error(extractApiError(e))
    }
  }
}
