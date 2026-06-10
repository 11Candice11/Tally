import { describe, it, expect, vi, beforeEach } from 'vitest'
import clientService from '../../api/ClientService'
import http from '../../api/http'

vi.mock('../../api/http', () => ({
  default: { post: vi.fn(), get: vi.fn(), put: vi.fn(), patch: vi.fn(), delete: vi.fn() },
  extractApiError: vi.fn((e: unknown) => (e as Error).message ?? 'Error'),
}))

const mockClient = {
  id: 1,
  name: 'Acme Corp',
  email: 'billing@acme.com',
  phone: '+27 11 000 0000',
  invoiceCount: 3,
  totalBilled: 9000,
  lastInvoice: '2025-04-01',
}

describe('ClientService', () => {
  beforeEach(() => vi.clearAllMocks())

  // ── getAll ─────────────────────────────────────────────────────────────────

  it('getAll calls GET /api/clients and returns array', async () => {
    vi.mocked(http.get).mockResolvedValue({ data: [mockClient] })

    const result = await clientService.getAll()

    expect(http.get).toHaveBeenCalledWith('/api/clients', { params: undefined })
    expect(result).toHaveLength(1)
    expect(result[0].name).toBe('Acme Corp')
  })

  // ── getById ────────────────────────────────────────────────────────────────

  it('getById calls GET /api/clients/:id', async () => {
    vi.mocked(http.get).mockResolvedValue({ data: mockClient })

    const result = await clientService.getById(1)

    expect(http.get).toHaveBeenCalledWith('/api/clients/1', { params: undefined })
    expect(result.id).toBe(1)
  })

  // ── create ─────────────────────────────────────────────────────────────────

  it('create posts to /api/clients and returns new client', async () => {
    vi.mocked(http.post).mockResolvedValue({ data: mockClient })

    const result = await clientService.create({ name: 'Acme Corp', email: 'billing@acme.com', phone: '+27 11 000 0000' })

    expect(http.post).toHaveBeenCalledWith('/api/clients', {
      name: 'Acme Corp',
      email: 'billing@acme.com',
      phone: '+27 11 000 0000',
    })
    expect(result.id).toBe(1)
  })

  // ── update ─────────────────────────────────────────────────────────────────

  it('update calls PUT /api/clients/:id and returns updated client', async () => {
    const updated = { ...mockClient, name: 'Acme Updated' }
    vi.mocked(http.put).mockResolvedValue({ data: updated })

    const result = await clientService.update(1, { name: 'Acme Updated' })

    expect(http.put).toHaveBeenCalledWith('/api/clients/1', { name: 'Acme Updated' })
    expect(result.name).toBe('Acme Updated')
  })

  // ── remove ─────────────────────────────────────────────────────────────────

  it('remove calls DELETE /api/clients/:id', async () => {
    vi.mocked(http.delete).mockResolvedValue({ data: undefined })

    await clientService.remove(1)

    expect(http.delete).toHaveBeenCalledWith('/api/clients/1')
  })

  // ── error handling ─────────────────────────────────────────────────────────

  it('propagates errors from the API', async () => {
    vi.mocked(http.get).mockRejectedValue(new Error('Forbidden'))

    await expect(clientService.getAll()).rejects.toThrow('Forbidden')
  })
})
