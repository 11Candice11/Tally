import { describe, it, expect } from 'vitest'
import { extractApiError } from '../../api/http'

describe('extractApiError', () => {
  it('returns fallback for null input', () => {
    expect(extractApiError(null)).toBe('An unexpected error occurred.')
  })

  it('returns fallback for non-object input', () => {
    expect(extractApiError('string error')).toBe('An unexpected error occurred.')
  })

  it('extracts plain message from response data', () => {
    const err = { response: { data: { message: 'Not found.' } } }
    expect(extractApiError(err)).toBe('Not found.')
  })

  it('extracts ASP.NET Core ModelState errors', () => {
    const err = {
      response: {
        data: {
          errors: {
            Email: ['Email is required.'],
            Password: ['Password must be at least 8 characters.'],
          },
        },
      },
    }
    const result = extractApiError(err)
    expect(result).toContain('Email is required.')
    expect(result).toContain('Password must be at least 8 characters.')
  })

  it('extracts string response data directly', () => {
    const err = { response: { data: 'Unauthorized' } }
    expect(extractApiError(err)).toBe('Unauthorized')
  })

  it('falls back to err.message for network errors', () => {
    const err = { message: 'Network Error' }
    expect(extractApiError(err)).toBe('Network Error')
  })

  it('uses custom fallback when provided', () => {
    expect(extractApiError(null, 'Custom fallback')).toBe('Custom fallback')
  })

  it('ignores empty message string in response data', () => {
    const err = { response: { data: { message: '' } }, message: 'Network Error' }
    expect(extractApiError(err)).toBe('Network Error')
  })
})
