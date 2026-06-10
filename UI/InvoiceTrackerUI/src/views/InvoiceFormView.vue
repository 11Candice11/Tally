<template>
  <div class="form-layout">
    <!-- Left: form -->
    <div class="form-main">
      <div class="form-header">
        <h1>{{ isEdit ? `Edit ${store.current?.invoiceNumber}` : 'New Invoice' }}</h1>
        <p class="form-sub">{{ isEdit ? 'Update invoice details' : 'Drafting invoice for professional services' }}</p>
      </div>

      <!-- Client Details -->
      <section class="form-section">
        <p class="section-label">CLIENT NAME</p>
        <div class="client-select-wrap">
          <select v-model="clientSelectValue" class="form-select" @change="onClientSelect">
            <option value="">Select a client…</option>
            <option v-for="c in uniqueClients" :key="c.id" :value="String(c.id)">
              {{ c.name }} — {{ c.email }}
            </option>
            <option value="__add__">+ Add new client…</option>
          </select>
          <p v-if="form.clientEmail" class="client-email-hint">{{ form.clientEmail }}</p>
        </div>
      </section>

      <!-- Add Client Modal -->
      <div v-if="showAddClient" class="modal-overlay" @click.self="showAddClient = false">
        <div class="modal">
          <div class="modal-header">
            <h2>Add New Client</h2>
            <button class="modal-close" @click="showAddClient = false">×</button>
          </div>
          <p class="modal-sub">Enter the client's details. They'll be saved for future invoices.</p>
          <div class="modal-field">
            <label class="modal-label">CLIENT NAME</label>
            <input v-model="newClient.name" class="modal-input" placeholder="Acme Corp" />
          </div>
          <div class="modal-field">
            <label class="modal-label">EMAIL ADDRESS</label>
            <input v-model="newClient.email" type="email" class="modal-input" placeholder="billing@acme.com" />
          </div>
          <div class="modal-field">
            <label class="modal-label">CELLPHONE <span class="modal-optional">(optional)</span></label>
            <input v-model="newClient.phone" type="tel" class="modal-input" placeholder="+27 82 123 4567" />
          </div>
          <div v-if="addClientError" class="modal-error">{{ addClientError }}</div>
          <div class="modal-actions">
            <button class="modal-btn-cancel" @click="showAddClient = false">Cancel</button>
            <button class="modal-btn-save" @click="confirmAddClient">Add Client</button>
          </div>
        </div>
      </div>

      <!-- Invoice Details -->
      <section class="form-section">
        <div class="row-2">
          <div class="field">
            <label class="field-label">ISSUE DATE</label>
            <input v-model="form.issueDate" type="date" class="form-input" />
          </div>
          <div class="field">
            <label class="field-label">DUE DATE</label>
            <input v-model="form.dueDate" type="date" class="form-input" />
          </div>
        </div>
      </section>

      <!-- Line Items -->
      <section class="form-section">
        <div class="line-items-header">
          <p class="section-label">LINE ITEMS</p>
          <button class="add-row-btn" @click="addItem">+ Add Row</button>
        </div>
        <div class="line-items-table">
          <div class="line-items-head">
            <span class="col-desc">DESCRIPTION</span>
            <span class="col-qty">QTY</span>
            <span class="col-price">PRICE</span>
            <span class="col-total">TOTAL</span>
            <span class="col-del" />
          </div>
          <div v-for="(item, i) in form.lineItems" :key="item.id" class="line-item-row">
            <input v-model="item.description" class="col-desc" placeholder="Description" />
            <input v-model.number="item.qty" class="col-qty" type="number" min="1" />
            <input v-model.number="item.unitPrice" class="col-price" type="number" min="0.01" step="0.01" placeholder="0.00" />
            <span class="col-total line-total">R {{ fmtNum(item.qty * item.unitPrice) }}</span>
            <button class="del-btn" @click="removeItem(i)">×</button>
          </div>
        </div>
      </section>

      <!-- Advanced Options -->
      <section class="form-section">
        <button class="advanced-toggle" @click="showAdvanced = !showAdvanced">
          <span class="toggle-icon">{{ showAdvanced ? '▼' : '▶' }}</span>
          ⚙ Advanced Options
        </button>
        <div v-if="showAdvanced" class="advanced-options">
          <div class="row-2">
            <div class="field">
              <label class="field-label">DISCOUNT (%)</label>
              <input v-model.number="form.discount" type="number" min="0" max="100" class="form-input" placeholder="0" />
            </div>
            <div class="field">
              <label class="field-label">LATE FEE ($)</label>
              <input v-model.number="form.lateFee" type="number" min="0" class="form-input" placeholder="0" />
            </div>
          </div>
          <div class="row-2">
            <div class="field">
              <label class="field-label">PAYMENT TERMS</label>
              <select v-model="form.paymentTerms" class="form-input">
                <option>Net 14</option>
                <option>Net 30</option>
                <option>Net 60</option>
                <option>Due on Receipt</option>
              </select>
            </div>
            <div class="field">
              <label class="field-label">CURRENCY</label>
              <select v-model="form.currency" class="form-input">
                <option>ZAR - South African Rand</option>
                <option>USD - US Dollar</option>
                <option>EUR - Euro</option>
              </select>
            </div>
          </div>
          <div class="field">
            <label class="field-label">NOTES</label>
            <textarea v-model="form.notes" rows="3" class="form-input" placeholder="Additional notes or payment instructions..." />
          </div>
        </div>
      </section>
    </div>

    <!-- Right: summary -->
    <aside class="form-sidebar">
      <div class="summary-card">
        <p class="card-title">Invoice Summary</p>
        <div class="summary-row">
          <span>Subtotal</span>
          <span>R {{ fmtNum(subtotal) }}</span>
        </div>
        <div class="summary-row">
          <span>Tax ({{ vatPct }}%)</span>
          <span>R {{ fmtNum(vat) }}</span>
        </div>
        <div class="summary-row" v-if="form.discount">
          <span>Discount</span>
          <span>-R {{ fmtNum(form.discount || 0) }}</span>
        </div>
        <div class="summary-divider" />
        <div class="summary-total">
          <span>AMOUNT DUE</span>
          <span class="total-value">R {{ fmtNum(total) }}</span>
        </div>
        <div class="summary-note">
          <span class="note-icon">🔒</span>
          Secure processing active
        </div>
      </div>

      <div class="notes-card">
        <p class="card-title">NOTES TO CLIENT</p>
        <textarea v-model="form.clientNotes" rows="4" class="form-input" placeholder="Thank you for your business. Please include invoice number with payment." />
      </div>

      <div class="methods-card">
        <p class="card-title">ACCEPTED METHODS</p>
        <div class="method-check">
          <input type="checkbox" id="bank" checked />
          <label for="bank">Bank Transfer</label>
        </div>
        <div class="method-check">
          <input type="checkbox" id="card" checked />
          <label for="card">Credit/Debit Card</label>
        </div>
        <div class="method-check">
          <input type="checkbox" id="stripe" />
          <label for="stripe">Stripe Checkout</label>
        </div>
      </div>

      <div class="button-group">
        <button class="btn-draft" :disabled="sending" @click="save(true)">Save Draft</button>
        <button class="btn-save" :disabled="sending" @click="openPreview">Review & Send</button>
      </div>
    </aside>

    <!-- Preview modal -->
    <div v-if="showPreview" class="modal-overlay" @click.self="showPreview = false">
      <div class="modal preview-modal">
        <div class="modal-header">
          <h2>Review Invoice</h2>
          <button class="modal-close" @click="showPreview = false">×</button>
        </div>
        <p class="modal-sub">Check the details below before sending to <strong>{{ form.clientEmail }}</strong>.</p>

        <!-- Client + dates -->
        <div class="preview-meta">
          <div class="preview-meta-col">
            <p class="preview-label">BILLED TO</p>
            <p class="preview-value">{{ form.clientName }}</p>
            <p class="preview-hint">{{ form.clientEmail }}</p>
          </div>
          <div class="preview-meta-col preview-meta-right">
            <p class="preview-label">ISSUE DATE</p>
            <p class="preview-value">{{ form.issueDate }}</p>
            <p class="preview-label" style="margin-top:8px">DUE DATE</p>
            <p class="preview-value">{{ form.dueDate }}</p>
          </div>
        </div>

        <!-- Line items -->
        <table class="preview-table">
          <thead>
            <tr>
              <th>DESCRIPTION</th>
              <th class="ta-right">QTY</th>
              <th class="ta-right">UNIT PRICE</th>
              <th class="ta-right">TOTAL</th>
            </tr>
          </thead>
          <tbody>
            <tr v-for="item in form.lineItems" :key="item.id">
              <td>{{ item.description }}</td>
              <td class="ta-right">{{ item.qty }}</td>
              <td class="ta-right">R {{ fmtNum(item.unitPrice) }}</td>
              <td class="ta-right">R {{ fmtNum(item.qty * item.unitPrice) }}</td>
            </tr>
          </tbody>
        </table>

        <!-- Totals -->
        <div class="preview-totals">
          <div class="preview-totals-row"><span>Subtotal</span><span>R {{ fmtNum(subtotal) }}</span></div>
          <div class="preview-totals-row"><span>VAT ({{ vatPct }}%)</span><span>R {{ fmtNum(vat) }}</span></div>
          <div v-if="form.discount" class="preview-totals-row preview-deduct">
            <span>Discount</span><span>-R {{ fmtNum(form.discount) }}</span>
          </div>
          <div v-if="form.lateFee" class="preview-totals-row preview-add">
            <span>Late Fee</span><span>+R {{ fmtNum(form.lateFee) }}</span>
          </div>
          <div class="preview-totals-divider" />
          <div class="preview-totals-row preview-grand"><span>TOTAL DUE</span><span>R {{ fmtNum(total) }}</span></div>
        </div>

        <!-- Payment terms / currency -->
        <div v-if="form.paymentTerms || form.currency" class="preview-meta preview-terms">
          <div v-if="form.paymentTerms" class="preview-meta-col">
            <p class="preview-label">PAYMENT TERMS</p>
            <p class="preview-value" style="font-size:13px">{{ form.paymentTerms }}</p>
          </div>
          <div v-if="form.currency" class="preview-meta-col preview-meta-right">
            <p class="preview-label">CURRENCY</p>
            <p class="preview-value" style="font-size:13px">{{ form.currency }}</p>
          </div>
        </div>

        <div v-if="form.notes" class="preview-notes">
          <p class="preview-label">NOTES</p>
          <p class="preview-hint">{{ form.notes }}</p>
        </div>

        <div v-if="sendError" class="modal-error">{{ sendError }}</div>

        <div class="modal-actions">
          <button class="modal-btn-cancel" @click="showPreview = false">← Edit</button>
          <button class="modal-btn-save" :disabled="sending" @click="confirmSend">
            {{ sending ? 'Sending…' : 'Confirm & Send' }}
          </button>
        </div>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { reactive, computed, onMounted, ref } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { useInvoiceStore } from '@/stores/invoiceStore'
import { useClientStore } from '@/stores/clientStore'
import { InvoiceStatus, type LineItem } from '@/models/invoice'
import * as api from '@/api/invoiceApi'
import {
  sanitizeName, sanitizeEmail, sanitizePhone, sanitizeNotes, sanitizeText,
  validateClientForm, validateInvoiceForm,
} from '@/composables/useValidation'
import { extractApiError } from '@/api/http'

const route       = useRoute()
const router      = useRouter()
const store       = useInvoiceStore()
const clientStore = useClientStore()

const isEdit = computed(() => !!route.params.id)
const showAdvanced   = ref(false)
const showAddClient  = ref(false)
const showPreview    = ref(false)
const addClientError = ref('')
const sendError      = ref('')
const clientSelectValue = ref('')
const sending = ref(false)

const newClient = reactive({ name: '', email: '', phone: '' })

const form = reactive({
  clientName:  '',
  clientEmail: '',
  issueDate:   new Date().toISOString().slice(0, 10),
  dueDate:     '',
  notes:       '',
  clientNotes: 'Thank you for your business. Please include invoice number with payment.',
  status:      InvoiceStatus.Draft,
  vatRate:     0.15,
  discount:    0,
  lateFee:     0,
  paymentTerms: 'Net 14',
  currency:    'ZAR - South African Rand',
  lineItems:   [newItem()] as LineItem[],
})

// Client dropdown comes from the normalised Clients table
const uniqueClients = computed(() => clientStore.clients)

function onClientSelect() {
  if (clientSelectValue.value === '__add__') {
    clientSelectValue.value = ''
    newClient.name  = ''
    newClient.email = ''
    newClient.phone = ''
    addClientError.value = ''
    showAddClient.value = true
    return
  }
  const id    = parseInt(clientSelectValue.value)
  const found = uniqueClients.value.find(c => c.id === id)
  if (found) {
    form.clientName  = found.name
    form.clientEmail = found.email
  } else {
    form.clientName  = ''
    form.clientEmail = ''
  }
}

async function confirmAddClient() {
  addClientError.value = ''
  const errs = validateClientForm(newClient.name, newClient.email, newClient.phone)
  if (errs.length) { addClientError.value = errs[0].message; return }

  try {
    const created = await clientStore.create({
      name:  sanitizeName(newClient.name),
      email: sanitizeEmail(newClient.email),
      phone: sanitizePhone(newClient.phone) || undefined,
    })
    form.clientName       = created.name
    form.clientEmail      = created.email
    clientSelectValue.value = String(created.id)
    showAddClient.value   = false
  } catch (e) {
    addClientError.value = extractApiError(e, 'Failed to add client.')
  }
}

onMounted(async () => {
  await Promise.all([store.fetchAll(), clientStore.fetchAll()])
  if (isEdit.value) {
    await store.fetchOne(route.params.id as string)
    const inv = store.current
    if (inv) {
      form.clientName  = inv.clientName
      form.clientEmail = inv.clientEmail
      clientSelectValue.value = inv.clientName
      form.issueDate    = inv.issueDate
      form.dueDate      = inv.dueDate
      form.clientNotes  = inv.notes ?? ''
      form.status       = inv.status
      form.vatRate      = inv.vatRate
      form.discount     = inv.discount ?? 0
      form.lateFee      = inv.lateFee ?? 0
      form.paymentTerms = inv.paymentTerms ?? 'Net 14'
      form.currency     = inv.currency === 'ZAR' ? 'ZAR - South African Rand'
                        : inv.currency === 'USD' ? 'USD - US Dollar'
                        : inv.currency === 'EUR' ? 'EUR - Euro'
                        : inv.currency
      form.lineItems    = inv.lineItems.map(l => ({ ...l }))
    }
  } else {
    // Pre-fill from query params (e.g. navigating from the client directory)
    const qEmail = route.query.clientEmail as string | undefined
    if (qEmail) {
      const found = clientStore.clients.find(c => c.email === qEmail)
      if (found) {
        form.clientName       = found.name
        form.clientEmail      = found.email
        clientSelectValue.value = String(found.id)
      }
    }
  }
})

function newItem(): LineItem {
  const id = 'xxxxxxxx-xxxx-4xxx-yxxx-xxxxxxxxxxxx'.replace(/[xy]/g, c => {
    const r = Math.random() * 16 | 0
    return (c === 'x' ? r : (r & 0x3 | 0x8)).toString(16)
  })
  return { id, description: '', qty: 1, unitPrice: 0 }
}
function addItem()        { form.lineItems.push(newItem()) }
function removeItem(i: number) { form.lineItems.splice(i, 1) }

const subtotal = computed(() => form.lineItems.reduce((s, l) => s + l.qty * l.unitPrice, 0))
const vat      = computed(() => subtotal.value * form.vatRate)
const total    = computed(() => subtotal.value + vat.value - (form.discount || 0) + (form.lateFee || 0))
const vatPct   = computed(() => Math.round(form.vatRate * 100))

function fmtNum(n: number) {
  return n.toLocaleString('en-ZA', { minimumFractionDigits: 2 })
}

function validate(): string | null {
  const errs = validateInvoiceForm({
    clientName:  form.clientName,
    clientEmail: form.clientEmail,
    issueDate:   form.issueDate,
    dueDate:     form.dueDate,
    notes:       form.notes,
    lineItems:   form.lineItems,
  })
  return errs.length ? errs[0].message : null
}

function buildLineItems() {
  return form.lineItems.map(({ description, qty, unitPrice }) => ({
    id: 'xxxxxxxx-xxxx-4xxx-yxxx-xxxxxxxxxxxx'.replace(/[xy]/g, c => {
      const r = Math.random() * 16 | 0
      return (c === 'x' ? r : (r & 0x3 | 0x8)).toString(16)
    }),
    description: sanitizeText(description),
    qty,
    unitPrice,
  }))
}

function openPreview() {
  const err = validate()
  if (err) { alert(err); return }
  sendError.value = ''
  showPreview.value = true
}

function buildDto() {
  // clientNotes is the user-facing label; it persists as `notes` on the model
  const notesText = form.clientNotes.trim() || (form.notes ? sanitizeNotes(form.notes) : undefined)
  return {
    clientName:    sanitizeName(form.clientName),
    clientEmail:   sanitizeEmail(form.clientEmail),
    issueDate:     new Date(form.issueDate + 'T00:00:00Z').toISOString(),
    dueDate:       new Date(form.dueDate + 'T00:00:00Z').toISOString(),
    notes:         notesText || undefined,
    vatRate:       form.vatRate,
    discount:      form.discount || 0,
    lateFee:       form.lateFee || 0,
    paymentTerms:  form.paymentTerms,
    currency:      form.currency.split(' - ')[0],   // "ZAR - South African Rand" → "ZAR"
    status:        InvoiceStatus.Draft,
    invoiceNumber: '',
    lineItems:     buildLineItems(),
  }
}

async function confirmSend() {
  sendError.value = ''
  sending.value = true
  try {
    const dto = buildDto()
    const invoice = isEdit.value
      ? await store.update(route.params.id as string, dto)
      : await store.create(dto)

    await api.sendInvoice(invoice.id, form.clientEmail)
    router.push('/invoices')
  } catch (e: any) {
    const detail = e?.response?.data
    sendError.value = typeof detail === 'object'
      ? Object.values(detail?.errors ?? {}).flat().join('\n') || JSON.stringify(detail)
      : String(detail ?? 'Failed to send invoice.')
  } finally {
    sending.value = false
  }
}

async function save(_asDraft: boolean) {
  const err = validate()
  if (err) { alert(err); return }
  sending.value = true
  try {
    const dto = buildDto()
    if (isEdit.value) {
      await store.update(route.params.id as string, dto)
    } else {
      await store.create(dto)
    }
    router.push('/invoices')
  } catch (e: any) {
    const detail = e?.response?.data
    const msg = typeof detail === 'object'
      ? Object.values(detail?.errors ?? {}).flat().join('\n') || JSON.stringify(detail)
      : String(detail ?? 'Failed to save invoice.')
    alert(msg)
  } finally {
    sending.value = false
  }
}
</script>

<style scoped>
.form-layout { display: flex; gap: 24px; align-items: flex-start; }
.form-main   { flex: 1; display: flex; flex-direction: column; gap: 20px; }
.form-sidebar { width: 300px; flex-shrink: 0; display: flex; flex-direction: column; gap: 16px; position: sticky; top: 72px; }

.form-header h1 { font-size: 26px; font-weight: 700; color: #1a1a1a; }
.form-sub { color: #999; font-size: 13px; margin-top: 4px; }

.form-section {
  background: #fff;
  border: 1px solid #e0e0e0;
  border-radius: 10px;
  padding: 20px;
}
.section-label { font-size: 11px; letter-spacing: 1px; color: #999; margin-bottom: 12px; font-weight: 600; }

.row-2 { display: grid; grid-template-columns: 1fr 1fr; gap: 16px; }
.field { display: flex; flex-direction: column; gap: 6px; }
.field-label { font-size: 11px; letter-spacing: .5px; color: #666; font-weight: 600; }

.form-input, .form-select {
  background: #f9f9f9;
  border: 1px solid #e0e0e0;
  border-radius: 6px;
  color: #1a1a1a;
  font-family: inherit;
  font-size: 13px;
  padding: 10px 12px;
  outline: none;
  transition: border-color .15s;
}
.form-input:focus, .form-select:focus { border-color: #1e293b; }
.form-input::placeholder { color: #ccc; }

/* Line items */
.line-items-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
  margin-bottom: 12px;
}
.add-row-btn {
  background: none;
  border: none;
  color: #0d9488;
  font-family: inherit;
  font-size: 12px;
  cursor: pointer;
  font-weight: 600;
}

.line-items-table {
  display: flex;
  flex-direction: column;
  gap: 8px;
}
.line-items-head, .line-item-row {
  display: grid;
  grid-template-columns: 1fr 80px 140px 120px 28px;
  gap: 8px;
  align-items: center;
}
.line-items-head {
  margin-bottom: 8px;
  padding-bottom: 8px;
  border-bottom: 1px solid #e0e0e0;
}
.line-items-head span { font-size: 10px; letter-spacing: .5px; color: #999; font-weight: 600; }
.line-total { font-weight: 700; color: #1a1a1a; font-size: 13px; }
.del-btn {
  background: none;
  border: none;
  color: #ef4444;
  font-size: 16px;
  cursor: pointer;
  padding: 0;
  line-height: 1;
}
.del-btn:hover { color: #dc2626; }

/* Advanced */
.advanced-toggle {
  background: none;
  border: none;
  color: #666;
  font-family: inherit;
  font-size: 13px;
  padding: 0;
  cursor: pointer;
  display: flex;
  align-items: center;
  gap: 8px;
  font-weight: 600;
}
.advanced-toggle:hover { color: #1a1a1a; }
.toggle-icon { font-size: 10px; }

.advanced-options {
  margin-top: 16px;
  padding-top: 16px;
  border-top: 1px solid #e0e0e0;
  display: flex;
  flex-direction: column;
  gap: 16px;
}

/* Sidebar */
.summary-card, .notes-card, .methods-card {
  background: #fff;
  border: 1px solid #e0e0e0;
  border-radius: 10px;
  padding: 16px;
  color: #1a1a1a;
}
.card-title { font-size: 12px; letter-spacing: 1px; color: #999; margin-bottom: 12px; font-weight: 700; }

.summary-row { display: flex; justify-content: space-between; color: #666; font-size: 13px; padding: 6px 0; }
.summary-divider { border-top: 1px solid #e0e0e0; margin: 10px 0; }
.summary-total { display: flex; justify-content: space-between; font-weight: 700; font-size: 14px; color: #1a1a1a; }
.total-value { color: #10b981; }

.summary-note {
  display: flex;
  align-items: center;
  gap: 6px;
  font-size: 11px;
  color: #999;
  margin-top: 10px;
  padding-top: 10px;
  border-top: 1px solid #e0e0e0;
}

.notes-card textarea {
  background: #f9f9f9;
  border: 1px solid #e0e0e0;
  border-radius: 6px;
  color: #1a1a1a;
  font-family: inherit;
  font-size: 13px;
  padding: 10px;
  resize: vertical;
  width: 100%;
}
.notes-card textarea::placeholder { color: #999; }

.method-check {
  display: flex;
  align-items: center;
  gap: 8px;
  margin-bottom: 8px;
}
.method-check input { cursor: pointer; }
.method-check label { font-size: 13px; color: #1a1a1a; cursor: pointer; }

.button-group {
  display: flex;
  flex-direction: column;
  gap: 10px;
}
.btn-draft {
  background: #fff;
  border: 1px solid #e0e0e0;
  border-radius: 6px;
  color: #666;
  font-family: inherit;
  font-size: 13px;
  font-weight: 600;
  padding: 10px;
  cursor: pointer;
  transition: all .15s;
}
.btn-draft:hover { border-color: #1a1a1a; color: #1a1a1a; }

.btn-save {
  background: #1e293b;
  border: none;
  border-radius: 6px;
  color: #fff;
  font-family: inherit;
  font-size: 13px;
  font-weight: 700;
  padding: 10px;
  cursor: pointer;
  transition: background .15s;
}
.btn-save:hover { background: #0f172a; }

/* Client select */
.client-select-wrap { display: flex; flex-direction: column; gap: 6px; }
.client-email-hint { font-size: 12px; color: #0d9488; margin: 0; }

/* Modal */
.modal-overlay {
  position: fixed;
  inset: 0;
  background: rgba(0,0,0,.4);
  display: flex;
  align-items: center;
  justify-content: center;
  z-index: 300;
}
.modal {
  background: #fff;
  border-radius: 12px;
  padding: 28px;
  width: 420px;
  box-shadow: 0 20px 60px rgba(0,0,0,.15);
}
.modal-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
  margin-bottom: 8px;
}
.modal-header h2 { font-size: 18px; font-weight: 700; color: #1a1a1a; }
.modal-close {
  background: none; border: none;
  font-size: 22px; color: #999; cursor: pointer; line-height: 1; padding: 0;
}
.modal-close:hover { color: #1a1a1a; }
.modal-sub { font-size: 13px; color: #666; margin-bottom: 20px; line-height: 1.5; }
.modal-field { display: flex; flex-direction: column; gap: 6px; margin-bottom: 14px; }
.modal-label { font-size: 11px; letter-spacing: .5px; color: #666; font-weight: 600; }
.modal-optional { font-weight: 400; color: #aaa; letter-spacing: 0; text-transform: none; font-size: 10px; }
.modal-input {
  background: #f9f9f9;
  border: 1px solid #e0e0e0;
  border-radius: 8px;
  color: #1a1a1a;
  font-family: inherit;
  font-size: 14px;
  padding: 10px 14px;
  outline: none;
  transition: border-color .15s;
}
.modal-input:focus { border-color: #1e293b; }
.modal-error {
  font-size: 12px; color: #ef4444;
  margin-bottom: 12px; padding: 8px 12px;
  background: #fef2f2; border-radius: 6px; border: 1px solid #fecaca;
}
.modal-actions { display: flex; gap: 10px; justify-content: flex-end; margin-top: 4px; }
.modal-btn-cancel {
  padding: 9px 18px; background: #fff;
  border: 1px solid #e0e0e0; border-radius: 8px;
  color: #666; font-family: inherit; font-size: 13px; cursor: pointer; transition: all .15s;
}
.modal-btn-cancel:hover { border-color: #1a1a1a; color: #1a1a1a; }
.modal-btn-save {
  padding: 9px 20px; background: #1e293b;
  border: none; border-radius: 8px;
  color: #fff; font-family: inherit; font-size: 13px; font-weight: 700; cursor: pointer; transition: background .15s;
}
.modal-btn-save:hover { background: #0f172a; }

/* Preview modal */
.preview-modal { width: 560px; max-height: 85vh; overflow-y: auto; }

.preview-meta {
  display: flex;
  justify-content: space-between;
  background: #f9f9f9;
  border: 1px solid #e0e0e0;
  border-radius: 8px;
  padding: 14px 16px;
  margin-bottom: 16px;
}
.preview-meta-col { display: flex; flex-direction: column; gap: 2px; }
.preview-meta-right { text-align: right; }
.preview-label { font-size: 10px; letter-spacing: 1px; color: #999; font-weight: 600; margin-bottom: 2px; }
.preview-value { font-size: 14px; font-weight: 600; color: #1a1a1a; }
.preview-hint  { font-size: 12px; color: #999; }

.preview-table {
  width: 100%;
  border-collapse: collapse;
  margin-bottom: 12px;
  font-size: 13px;
}
.preview-table th {
  font-size: 10px;
  letter-spacing: .5px;
  color: #999;
  font-weight: 600;
  padding: 8px 0;
  border-bottom: 1px solid #e0e0e0;
  text-align: left;
}
.preview-table td {
  padding: 10px 0;
  border-bottom: 1px solid #f0f0f0;
  color: #1a1a1a;
}
.preview-table tbody tr:last-child td { border-bottom: none; }
.ta-right { text-align: right; }

.preview-totals {
  display: flex;
  flex-direction: column;
  gap: 6px;
  align-items: flex-end;
  margin-bottom: 14px;
}
.preview-totals-row {
  display: flex;
  gap: 60px;
  font-size: 13px;
  color: #666;
}
.preview-totals-divider { width: 240px; border-top: 1px solid #e0e0e0; margin: 4px 0; }
.preview-grand  { font-size: 15px; font-weight: 700; color: #1a1a1a; }
.preview-deduct { color: #10b981; }
.preview-add    { color: #ef4444; }
.preview-terms  { margin-top: 12px; }

.preview-notes {
  background: #f9f9f9;
  border-radius: 6px;
  padding: 10px 12px;
  margin-bottom: 14px;
}
</style>
