import { computed } from 'vue'
import { useInvoiceStore } from '../stores/invoiceStore'
import { InvoiceStatus, invoiceTotal } from '../models/invoice'

export function useInvoices() {
  const store = useInvoiceStore()

  const overdue = computed(() =>
    store.invoices.filter(i => i.status === InvoiceStatus.Overdue)
  )

  const totalMTD = computed(() =>
    store.invoices.reduce((s, i) => s + invoiceTotal(i), 0)
  )

  const paidMTD = computed(() =>
    store.invoices
      .filter(i => i.status === InvoiceStatus.Paid)
      .reduce((s, i) => s + invoiceTotal(i), 0)
  )

  return {
    invoices: store.invoices,
    loading:  store.loading,
    error:    store.error,
    total:    totalMTD,
    paid:     paidMTD,
    overdue,
    fetchAll: store.fetchAll,
    create:   store.create,
    update:   store.update,
    remove:   store.remove,
  }
}
