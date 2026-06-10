import invoiceService from './InvoiceService'

export const getInvoices   = invoiceService.getAll.bind(invoiceService)
export const getInvoice    = invoiceService.getById.bind(invoiceService)
export const createInvoice = invoiceService.create.bind(invoiceService)
export const updateInvoice = invoiceService.update.bind(invoiceService)
export const deleteInvoice = invoiceService.remove.bind(invoiceService)
export const sendInvoice   = invoiceService.send.bind(invoiceService)
export const patchStatus   = invoiceService.patchStatus.bind(invoiceService)
