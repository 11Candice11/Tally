// client model, invoiceCount and totalBilled come from the API not stored locally
// the API calculates those from the invoices table so they're always up to date
export interface Client {
  id:           number
  name:         string
  email:        string
  phone?:       string
  invoiceCount: number
  totalBilled:  number
  lastInvoice?: string
}

export interface CreateClientDto {
  name:   string
  email:  string
  phone?: string
}

export interface UpdateClientDto {
  name?:  string
  email?: string
  phone?: string
}
