import { describe, it, expect, beforeEach, vi } from 'vitest'
import { setActivePinia, createPinia } from 'pinia'
import { useInvoiceStore } from '../../stores/invoiceStore'
import { InvoiceStatus } from '../../models/invoice'
import type { Invoice, InvoiceSummary } from '../../models/invoice'
import invoiceService from '../../api/InvoiceService'

vi.mock('../../api/InvoiceService', () => ({
  default: {
    getSummary:  vi.fn(),
    getAll:      vi.fn(),
    getById:     vi.fn(),
    create:      vi.fn(),
    update:      vi.fn(),
    remove:      vi.fn(),
    send:        vi.fn(),
    patchStatus: vi.fn(),
  },
}))

const makeInvoice = (id: string, status: InvoiceStatus, unitPrice: number): Invoice => ({
  id,
  invoiceNumber: `INV-${id}`,
  clientName:   'Test Client',
  clientEmail:  'test@client.com',
  status,
  issueDate:    '2025-04-01',
  dueDate:      '2025-04-30',
  vatRate:      0.15,
  discount:     0,
  lateFee:      0,
  paymentTerms: 'Net 30',
  currency:     'ZAR',
  lineItems:    [{ id: '1', description: 'Work', qty: 1, unitPrice }],
})

const makeSummary = (overrides: Partial<InvoiceSummary> = {}): InvoiceSummary => ({
  totalRevenue:  0,
  outstanding:   0,
  overdueTotals: 0,
  paidThisMonth: 0,
  paidLastMonth: 0,
  overdueCount:  0,
  pendingCount:  0,
  ...overrides,
})

describe('invoiceStore', () => {
  beforeEach(() => {
    setActivePinia(createPinia())
    vi.clearAllMocks()
  })

  // ── fetchAll ───────────────────────────────────────────────────────────────

  it('fetchAll populates invoices', async () => {
    vi.mocked(invoiceService.getAll).mockResolvedValue({
      items: [makeInvoice('1', InvoiceStatus.Paid, 1000)],
      total: 1, page: 1, pageSize: 20,
    })
    const store = useInvoiceStore()

    await store.fetchAll()

    expect(store.invoices).toHaveLength(1)
    expect(store.loading).toBe(false)
  })

  it('fetchAll sets error on failure', async () => {
    vi.mocked(invoiceService.getAll).mockRejectedValue(new Error('Network error'))
    const store = useInvoiceStore()

    await store.fetchAll()

    expect(store.error).toBe('Failed to load invoices')
    expect(store.invoices).toHaveLength(0)
  })

  // ── fetchSummary ───────────────────────────────────────────────────────────

  it('fetchSummary populates summary from server', async () => {
    vi.mocked(invoiceService.getSummary).mockResolvedValue(
      makeSummary({ totalRevenue: 5000, outstanding: 1500, overdueCount: 2, pendingCount: 3 })
    )
    const store = useInvoiceStore()

    await store.fetchSummary()

    expect(store.summary.totalRevenue).toBe(5000)
    expect(store.summary.outstanding).toBe(1500)
    expect(store.summary.overdueCount).toBe(2)
    expect(store.summary.pendingCount).toBe(3)
  })

  it('fetchSummary leaves summary as zeros on failure', async () => {
    vi.mocked(invoiceService.getSummary).mockRejectedValue(new Error('Network error'))
    const store = useInvoiceStore()

    await store.fetchSummary()

    expect(store.summary.totalRevenue).toBe(0)
    expect(store.summary.overdueCount).toBe(0)
  })

  // ── create ─────────────────────────────────────────────────────────────────

  it('create prepends invoice to list', async () => {
    const newInvoice = makeInvoice('99', InvoiceStatus.Draft, 3000)
    vi.mocked(invoiceService.create).mockResolvedValue(newInvoice)
    vi.mocked(invoiceService.getAll).mockResolvedValue({
      items: [makeInvoice('1', InvoiceStatus.Paid, 1000)], total: 1, page: 1, pageSize: 20,
    })
    const store = useInvoiceStore()
    await store.fetchAll()

    await store.create({ ...newInvoice })

    expect(store.invoices[0].id).toBe('99')
    expect(store.invoices).toHaveLength(2)
  })

  // ── update ─────────────────────────────────────────────────────────────────

  it('update replaces invoice in list', async () => {
    const original = makeInvoice('1', InvoiceStatus.Draft, 1000)
    const updated  = { ...original, clientName: 'Updated Client' }
    vi.mocked(invoiceService.getAll).mockResolvedValue({
      items: [original], total: 1, page: 1, pageSize: 20,
    })
    vi.mocked(invoiceService.update).mockResolvedValue(updated)
    const store = useInvoiceStore()
    await store.fetchAll()

    await store.update('1', { clientName: 'Updated Client' })

    expect(store.invoices[0].clientName).toBe('Updated Client')
  })

  // ── remove ─────────────────────────────────────────────────────────────────

  it('remove deletes invoice from list', async () => {
    vi.mocked(invoiceService.getAll).mockResolvedValue({
      items: [
        makeInvoice('1', InvoiceStatus.Paid, 1000),
        makeInvoice('2', InvoiceStatus.Sent, 2000),
      ], total: 2, page: 1, pageSize: 20,
    })
    vi.mocked(invoiceService.remove).mockResolvedValue()
    const store = useInvoiceStore()
    await store.fetchAll()

    await store.remove('1')

    expect(store.invoices).toHaveLength(1)
    expect(store.invoices[0].id).toBe('2')
  })
})
