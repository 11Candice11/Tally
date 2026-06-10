// all the invoice types and the helper functions for calculating totals
// vatRate is stored as a decimal like 0.15 not 15, easy to forget that
export enum InvoiceStatus {
  Draft = 'Draft',
  Sent = 'Sent',
  Paid = 'Paid',
  Overdue = 'Overdue',
  Cancelled = 'Cancelled',
}

export interface LineItem {
  id: string
  description: string
  qty: number
  unitPrice: number
}

export interface ActivityEntry {
  label: string
  date: string
  color: 'green' | 'blue' | 'yellow' | 'red'
}

export interface Invoice {
  id: string
  invoiceNumber: string
  clientName: string
  clientEmail: string
  lineItems: LineItem[]
  status: InvoiceStatus
  issueDate: string
  dueDate: string
  vatRate: number       // e.g. 0.15
  discount: number      // absolute amount deducted after VAT
  lateFee: number       // absolute amount added after VAT
  paymentTerms: string  // e.g. "Net 30"
  currency: string      // e.g. "ZAR"
  notes?: string
  activity?: ActivityEntry[]
}

export interface InvoiceSummary {
  totalRevenue:  number
  outstanding:   number
  overdueTotals: number
  paidThisMonth: number
  paidLastMonth: number
  overdueCount:  number
  pendingCount:  number
}

export interface PagedResult<T> {
  items: T[]
  total: number
  page: number
  pageSize: number
}

export interface InvoiceFilters {
  status?: InvoiceStatus | ''
  search?: string
  dateRange?: 'month' | '3months' | 'year' | 'custom'
  from?: string
  to?: string
}

export type CreateInvoiceDto = Omit<Invoice, 'id' | 'activity'>
export type UpdateInvoiceDto = Partial<CreateInvoiceDto>

// Helpers
export function invoiceSubtotal(inv: Invoice): number {
  return inv.lineItems.reduce((s, l) => s + l.qty * l.unitPrice, 0)
}

export function invoiceVat(inv: Invoice): number {
  return invoiceSubtotal(inv) * inv.vatRate
}

export function invoiceTotal(inv: Invoice): number {
  return invoiceSubtotal(inv) + invoiceVat(inv) - (inv.discount ?? 0) + (inv.lateFee ?? 0)
}
