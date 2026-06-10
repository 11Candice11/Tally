import { describe, it, expect } from 'vitest'
import { invoiceSubtotal, invoiceVat, invoiceTotal, InvoiceStatus } from '../../models/invoice'
import type { Invoice } from '../../models/invoice'

const makeInvoice = (overrides: Partial<Invoice> = {}): Invoice => ({
  id: '1',
  invoiceNumber: 'INV-0001',
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
  lineItems: [
    { id: '1', description: 'Consulting', qty: 2, unitPrice: 1000 },
    { id: '2', description: 'Design',     qty: 1, unitPrice: 500  },
  ],
  ...overrides,
})

describe('invoiceSubtotal', () => {
  it('sums qty × unitPrice for all line items', () => {
    expect(invoiceSubtotal(makeInvoice())).toBe(2500)
  })

  it('returns 0 for empty line items', () => {
    expect(invoiceSubtotal(makeInvoice({ lineItems: [] }))).toBe(0)
  })
})

describe('invoiceVat', () => {
  it('calculates VAT at 15%', () => {
    expect(invoiceVat(makeInvoice())).toBe(375)
  })

  it('calculates VAT at custom rate', () => {
    expect(invoiceVat(makeInvoice({ vatRate: 0.2 }))).toBe(500)
  })

  it('returns 0 VAT on empty invoice', () => {
    expect(invoiceVat(makeInvoice({ lineItems: [] }))).toBe(0)
  })
})

describe('invoiceTotal', () => {
  it('returns subtotal + VAT', () => {
    expect(invoiceTotal(makeInvoice())).toBe(2875)
  })

  it('returns 0 for empty invoice', () => {
    expect(invoiceTotal(makeInvoice({ lineItems: [] }))).toBe(0)
  })
})
