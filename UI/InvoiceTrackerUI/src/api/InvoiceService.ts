// mapInvoice converts the API response to the frontend model
// the API returns datetime strings with a T in the middle, we strip that for the date inputs
import { ServiceBase } from './ServiceBase'
import type { Invoice, InvoiceFilters, InvoiceSummary, CreateInvoiceDto, UpdateInvoiceDto, PagedResult } from '../models/invoice'

function mapInvoice(d: any): Invoice {
  return {
    id:            String(d.id),
    invoiceNumber: d.invoiceNumber,
    clientName:    d.clientName,
    clientEmail:   d.clientEmail,
    status:        d.status,
    issueDate:     d.issueDate?.split('T')[0] ?? '',
    dueDate:       d.dueDate?.split('T')[0] ?? '',
    vatRate:       d.vatRate,
    discount:      d.discount ?? 0,
    lateFee:       d.lateFee ?? 0,
    paymentTerms:  d.paymentTerms ?? 'Net 30',
    currency:      d.currency ?? 'ZAR',
    notes:         d.notes,
    lineItems:     (d.lineItems ?? []).map((l: any) => ({
      id:          String(l.id),
      description: l.description,
      qty:         l.qty,
      unitPrice:   l.unitPrice,
    })),
  }
}

class InvoiceService extends ServiceBase {
  getSummary(): Promise<InvoiceSummary> {
    return this.get<InvoiceSummary>('/api/invoices/summary')
  }

  async getAll(filters?: InvoiceFilters & { page?: number; pageSize?: number }): Promise<PagedResult<Invoice>> {
    const params: Record<string, string> = {}
    if (filters?.status)   params.status     = filters.status
    if (filters?.search)   params.clientName = filters.search
    if (filters?.from)     params.from       = filters.from
    if (filters?.to)       params.to         = filters.to
    if (filters?.page)     params.page       = String(filters.page)
    if (filters?.pageSize) params.pageSize   = String(filters.pageSize)

    const data = await this.get<any>('/api/invoices', params)
    return {
      items:    data.items.map(mapInvoice),
      total:    data.total,
      page:     data.page,
      pageSize: data.pageSize,
    }
  }

  async getById(id: string): Promise<Invoice> {
    const data = await this.get<any>(`/api/invoices/${id}`)
    return mapInvoice(data)
  }

  async create(dto: CreateInvoiceDto): Promise<Invoice> {
    const data = await this.post<any>('/api/invoices', {
      clientName:   dto.clientName,
      clientEmail:  dto.clientEmail,
      status:       String(dto.status),
      issueDate:    dto.issueDate,
      dueDate:      dto.dueDate,
      vatRate:      dto.vatRate,
      discount:     dto.discount ?? 0,
      lateFee:      dto.lateFee ?? 0,
      paymentTerms: dto.paymentTerms ?? 'Net 30',
      currency:     dto.currency ?? 'ZAR',
      notes:        dto.notes,
      lineItems:    dto.lineItems.map(l => ({
        description: l.description,
        qty:         Number(l.qty),
        unitPrice:   Number(l.unitPrice),
      })),
    })
    return mapInvoice(data)
  }

  async update(id: string, dto: UpdateInvoiceDto): Promise<Invoice> {
    const data = await this.put<any>(`/api/invoices/${id}`, {
      clientName:   dto.clientName,
      clientEmail:  dto.clientEmail,
      status:       dto.status ? String(dto.status) : undefined,
      issueDate:    dto.issueDate,
      dueDate:      dto.dueDate,
      vatRate:      dto.vatRate,
      discount:     dto.discount,
      lateFee:      dto.lateFee,
      paymentTerms: dto.paymentTerms,
      currency:     dto.currency,
      notes:        dto.notes,
      lineItems:    dto.lineItems?.map(l => ({
        description: l.description,
        qty:         Number(l.qty),
        unitPrice:   Number(l.unitPrice),
      })),
    })
    return mapInvoice(data)
  }

  remove(id: string): Promise<void> {
    return this.delete(`/api/invoices/${id}`)
  }

  async send(id: string, toEmail: string): Promise<void> {
    await this.post(`/api/invoices/${id}/send`, { toEmail })
  }

  async patchStatus(id: string, status: string): Promise<Invoice> {
    const data = await this.patch<any>(`/api/invoices/${id}/status`, { status })
    return mapInvoice(data)
  }
}

export default new InvoiceService()
