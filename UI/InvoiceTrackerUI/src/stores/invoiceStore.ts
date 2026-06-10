import { defineStore } from 'pinia'
import { ref } from 'vue'
import type { Invoice, InvoiceFilters, InvoiceSummary, CreateInvoiceDto, UpdateInvoiceDto } from '../models/invoice'
import invoiceService from '../api/InvoiceService'

const emptySummary: InvoiceSummary = {
  totalRevenue:  0,
  outstanding:   0,
  overdueTotals: 0,
  paidThisMonth: 0,
  paidLastMonth: 0,
  overdueCount:  0,
  pendingCount:  0,
}

// invoice store holds the list, current invoice, pagination and summary stats
// fetchSummary is non fatal so the dashboard still loads even if it fails
export const useInvoiceStore = defineStore('invoices', () => {
  const invoices = ref<Invoice[]>([])
  const current  = ref<Invoice | null>(null)
  const loading  = ref(false)
  const error    = ref<string | null>(null)
  const total    = ref(0)
  const page     = ref(1)
  const pageSize = ref(20)
  const summary  = ref<InvoiceSummary>({ ...emptySummary })

  async function fetchSummary() {
    try {
      summary.value = await invoiceService.getSummary()
    } catch {
      // non-fatal — dashboard shows zeros rather than crashing
    }
  }

  async function fetchAll(filters?: InvoiceFilters & { page?: number; pageSize?: number }) {
    loading.value = true
    error.value = null
    try {
      const result   = await invoiceService.getAll(filters)
      invoices.value = result.items
      total.value    = result.total
      page.value     = result.page
      pageSize.value = result.pageSize
    } catch {
      error.value = 'Failed to load invoices'
    } finally {
      loading.value = false
    }
  }

  async function fetchOne(id: string) {
    loading.value = true
    error.value = null
    try {
      current.value = await invoiceService.getById(id)
    } catch {
      error.value = 'Failed to load invoice'
    } finally {
      loading.value = false
    }
  }

  async function create(dto: CreateInvoiceDto): Promise<Invoice> {
    const inv = await invoiceService.create(dto)
    invoices.value.unshift(inv)
    return inv
  }

  async function update(id: string, dto: UpdateInvoiceDto): Promise<Invoice> {
    const updated = await invoiceService.update(id, dto)
    const idx = invoices.value.findIndex(i => i.id === id)
    if (idx !== -1) invoices.value[idx] = updated
    if (current.value?.id === id) current.value = updated
    return updated
  }

  async function remove(id: string) {
    await invoiceService.remove(id)
    invoices.value = invoices.value.filter(i => i.id !== id)
  }

  return {
    invoices, current, loading, error,
    total, page, pageSize, summary,
    fetchSummary, fetchAll, fetchOne, create, update, remove,
  }
})
