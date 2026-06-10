import { describe, it, expect, vi, beforeEach } from 'vitest'
import invoiceService from '../../api/InvoiceService'
import http from '../../api/http'
import { InvoiceStatus } from '../../models/invoice'

vi.mock('../../api/http', () => ({
  default: { post: vi.fn(), get: vi.fn(), put: vi.fn(), patch: vi.fn(), delete: vi.fn() },
  extractApiError: vi.fn((e: unknown) => (e as Error).message ?? 'Error'),
}))

const apiInvoice = {
  id: 1,
  invoiceNumber: 'INV-202504-0001',
  clientName: 'Acme Corp',
  clientEmail: 'billing@acme.com',
  status: 'Draft',
  issueDate: '2025-04-01T00:00:00',
  dueDate: '2025-04-30T00:00:00',
  vatRate: 0.15,
  discount: 0,
  lateFee: 0,
  paymentTerms: 'Net 30',
  currency: 'ZAR',
  notes: null,
  lineItems: [{ id: 1, description: 'Consulting', qty: 2, unitPrice: 1000 }],
}

describe('InvoiceService', () => {
  beforeEach(() => vi.clearAllMocks())

  // ── getSummary ─────────────────────────────────────────────────────────────

  it('getSummary calls GET /api/invoices/summary', async () => {
    const summary = { totalRevenue: 5000, outstanding: 1500, overdueTotals: 0, paidThisMonth: 2000, paidLastMonth: 1000, overdueCount: 0, pendingCount: 2 }
    vi.mocked(http.get).mockResolvedValue({ data: summary })

    const result = await invoiceService.getSummary()

    expect(http.get).toHaveBeenCalledWith('/api/invoices/summary', { params: undefined })
    expect(result).toEqual(summary)
  })

  // ── getAll ─────────────────────────────────────────────────────────────────

  it('getAll calls GET /api/invoices and maps response', async () => {
    vi.mocked(http.get).mockResolvedValue({
      data: { items: [apiInvoice], total: 1, page: 1, pageSize: 20 },
    })

    const result = await invoiceService.getAll()

    expect(http.get).toHaveBeenCalledWith('/api/invoices', { params: {} })
    expect(result.total).toBe(1)
    expect(result.items).toHaveLength(1)
    expect(result.items[0].id).toBe('1')           // numeric id → string
    expect(result.items[0].issueDate).toBe('2025-04-01') // T-part stripped
    expect(result.items[0].lineItems[0].id).toBe('1')
  })

  it('getAll passes status and search filters as query params', async () => {
    vi.mocked(http.get).mockResolvedValue({
      data: { items: [], total: 0, page: 1, pageSize: 20 },
    })

    await invoiceService.getAll({ status: InvoiceStatus.Sent, search: 'Acme', page: 2, pageSize: 10 })

    expect(http.get).toHaveBeenCalledWith('/api/invoices', {
      params: { status: 'Sent', clientName: 'Acme', page: '2', pageSize: '10' },
    })
  })

  // ── getById ────────────────────────────────────────────────────────────────

  it('getById calls GET /api/invoices/:id and maps response', async () => {
    vi.mocked(http.get).mockResolvedValue({ data: apiInvoice })

    const result = await invoiceService.getById('1')

    expect(http.get).toHaveBeenCalledWith('/api/invoices/1', { params: undefined })
    expect(result.id).toBe('1')
    expect(result.clientName).toBe('Acme Corp')
  })

  // ── create ─────────────────────────────────────────────────────────────────

  it('create posts to /api/invoices and maps response', async () => {
    vi.mocked(http.post).mockResolvedValue({ data: apiInvoice })

    const result = await invoiceService.create({
      invoiceNumber: 'INV-202504-0001',
      clientName: 'Acme Corp',
      clientEmail: 'billing@acme.com',
      status: InvoiceStatus.Draft,
      issueDate: '2025-04-01',
      dueDate: '2025-04-30',
      vatRate: 0.15,
      discount: 0,
      lateFee: 0,
      paymentTerms: 'Net 30',
      currency: 'ZAR',
      lineItems: [{ id: '1', description: 'Consulting', qty: 2, unitPrice: 1000 }],
    })

    expect(http.post).toHaveBeenCalledWith('/api/invoices', expect.objectContaining({
      clientName: 'Acme Corp',
      clientEmail: 'billing@acme.com',
    }))
    expect(result.id).toBe('1')
  })

  // ── update ─────────────────────────────────────────────────────────────────

  it('update calls PUT /api/invoices/:id and maps response', async () => {
    const updated = { ...apiInvoice, clientName: 'Updated Corp' }
    vi.mocked(http.put).mockResolvedValue({ data: updated })

    const result = await invoiceService.update('1', { clientName: 'Updated Corp' })

    expect(http.put).toHaveBeenCalledWith('/api/invoices/1', expect.objectContaining({
      clientName: 'Updated Corp',
    }))
    expect(result.clientName).toBe('Updated Corp')
  })

  // ── remove ─────────────────────────────────────────────────────────────────

  it('remove calls DELETE /api/invoices/:id', async () => {
    vi.mocked(http.delete).mockResolvedValue({ data: undefined })

    await invoiceService.remove('1')

    expect(http.delete).toHaveBeenCalledWith('/api/invoices/1')
  })

  // ── send ───────────────────────────────────────────────────────────────────

  it('send posts to /api/invoices/:id/send', async () => {
    vi.mocked(http.post).mockResolvedValue({ data: { message: 'Invoice sent successfully.' } })

    await invoiceService.send('1', 'client@test.com')

    expect(http.post).toHaveBeenCalledWith('/api/invoices/1/send', { toEmail: 'client@test.com' })
  })

  // ── patchStatus ────────────────────────────────────────────────────────────

  it('patchStatus calls PATCH /api/invoices/:id/status and maps response', async () => {
    const sentInvoice = { ...apiInvoice, status: 'Sent' }
    vi.mocked(http.patch).mockResolvedValue({ data: sentInvoice })

    const result = await invoiceService.patchStatus('1', 'Sent')

    expect(http.patch).toHaveBeenCalledWith('/api/invoices/1/status', { status: 'Sent' })
    expect(result.status).toBe('Sent')
  })

  // ── error handling ─────────────────────────────────────────────────────────

  it('propagates errors from the API', async () => {
    vi.mocked(http.get).mockRejectedValue(new Error('Unauthorized'))

    await expect(invoiceService.getAll()).rejects.toThrow('Unauthorized')
  })
})
