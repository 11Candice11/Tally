<template>
  <div v-if="store.loading" class="loading">Loading...</div>
  <div v-else-if="inv" class="detail-layout">

    <!-- Send modal -->
    <div v-if="showSendModal" class="modal-overlay" @click.self="showSendModal = false">
      <div class="modal">
        <div class="modal-header">
          <h2>Send Invoice</h2>
          <button class="modal-close" @click="showSendModal = false">×</button>
        </div>
        <p class="modal-sub">Confirm the recipient email address for <strong>{{ inv.invoiceNumber }}</strong>.</p>
        <div class="modal-field">
          <label class="modal-label">RECIPIENT EMAIL</label>
          <input
            v-model="sendEmail"
            type="email"
            class="modal-input"
            placeholder="client@example.com"
            @keydown.enter="doSend"
          />
        </div>
        <div v-if="sendError" class="modal-error">{{ sendError }}</div>
        <div class="modal-actions">
          <button class="modal-btn-cancel" @click="showSendModal = false">Cancel</button>
          <button class="modal-btn-send" :disabled="sending" @click="doSend">
            {{ sending ? 'Sending…' : 'Send Invoice' }}
          </button>
        </div>
      </div>
    </div>

    <!-- Header -->
    <div class="detail-header">
      <div class="header-left">
        <p class="inv-label">INVOICE</p>
        <h1 class="inv-number">{{ inv.invoiceNumber }}</h1>
        <div class="inv-meta">
          <span>Issued <strong>{{ fmtDate(inv.issueDate) }}</strong></span>
          <span class="sep">|</span>
          <span>Due <strong :class="{ 'due-overdue': inv.status === 'Overdue' }">{{ fmtDate(inv.dueDate) }}</strong></span>
          <span class="sep">|</span>
          <span :class="['badge', `badge-${inv.status.toLowerCase()}`]">{{ inv.status.toUpperCase() }}</span>
        </div>
      </div>
      <div class="header-actions">
        <button class="btn-action" @click="router.back()">← Back</button>
        <button class="btn-action" @click="router.push(`/invoices/${inv.id}/pdf`)">↓ Download PDF</button>
        <button class="btn-action" @click="openSendModal">Send</button>
        <button class="btn-mark-paid" @click="markPaid">✓ Mark Paid</button>
      </div>
    </div>

    <div class="detail-divider" />

    <!-- Body -->
    <div class="detail-body">
      <!-- Left: invoice content -->
      <div class="invoice-content">

        <!-- Billed to -->
        <div class="billed-card">
          <p class="field-label">BILLED TO</p>
          <p class="client-name">{{ inv.clientName }}</p>
          <p class="client-email">{{ inv.clientEmail }}</p>
        </div>

        <!-- Line items table -->
        <table class="items-table">
          <thead>
            <tr>
              <th>DESCRIPTION</th>
              <th>QTY</th>
              <th>UNIT PRICE</th>
              <th class="th-right">TOTAL</th>
            </tr>
          </thead>
          <tbody>
            <tr v-for="item in inv.lineItems" :key="item.id">
              <td class="td-desc">{{ item.description }}</td>
              <td class="td-qty">{{ item.qty }}</td>
              <td class="td-price">R {{ fmtNum(item.unitPrice) }}</td>
              <td class="td-total">R {{ fmtNum(item.qty * item.unitPrice) }}</td>
            </tr>
          </tbody>
        </table>

        <!-- Totals -->
        <div class="totals">
          <div class="totals-row">
            <span>Subtotal</span>
            <span>R {{ fmtNum(subtotal) }}</span>
          </div>
          <div class="totals-row">
            <span>VAT ({{ vatPct }}%)</span>
            <span>R {{ fmtNum(vat) }}</span>
          </div>
          <div class="totals-divider" />
          <div class="totals-row grand">
            <span>Total Amount</span>
            <span class="grand-value">R {{ fmtNum(total) }}</span>
          </div>
        </div>
      </div>

      <!-- Right sidebar -->
      <aside class="detail-sidebar">
        <!-- Activity -->
        <div class="sidebar-card">
          <p class="section-label">ACTIVITY</p>
          <div v-if="inv.activity && inv.activity.length">
            <div v-for="(entry, i) in inv.activity" :key="i" class="activity-entry">
              <span class="activity-dot" :class="`dot-${entry.color}`" />
              <div>
                <p class="activity-label">{{ entry.label }}</p>
                <p class="activity-date">{{ entry.date }}</p>
              </div>
            </div>
          </div>
          <p v-else class="activity-empty">No activity yet.</p>
        </div>

        <!-- Notes -->
        <div v-if="inv.notes" class="sidebar-card">
          <p class="section-label">NOTES</p>
          <p class="notes-text">{{ inv.notes }}</p>
        </div>

        <button class="btn-edit" @click="router.push(`/invoices/${inv.id}/edit`)">Edit Invoice</button>
        <button class="btn-delete" @click="deleteInvoice">Delete Invoice</button>
      </aside>
    </div>
  </div>
</template>

<script setup lang="ts">
import { computed, onMounted, ref } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { useInvoiceStore } from '../stores/invoiceStore'
import { InvoiceStatus, invoiceSubtotal, invoiceVat, invoiceTotal } from '../models/invoice'
import * as api from '../api/invoiceApi'
import { sanitizeEmail, isValidEmail } from '../composables/useValidation'

const route  = useRoute()
const router = useRouter()
const store  = useInvoiceStore()

onMounted(() => store.fetchOne(route.params.id as string))

const inv      = computed(() => store.current)
const subtotal = computed(() => inv.value ? invoiceSubtotal(inv.value) : 0)
const vat      = computed(() => inv.value ? invoiceVat(inv.value) : 0)
const total    = computed(() => inv.value ? invoiceTotal(inv.value) : 0)
const vatPct   = computed(() => inv.value ? Math.round(inv.value.vatRate * 100) : 15)

// Send modal state
const showSendModal = ref(false)
const sendEmail     = ref('')
const sending       = ref(false)
const sendError     = ref('')

function openSendModal() {
  sendEmail.value = inv.value?.clientEmail ?? ''
  sendError.value = ''
  showSendModal.value = true
}

async function doSend() {
  if (!sendEmail.value.trim() || !isValidEmail(sanitizeEmail(sendEmail.value))) {
    sendError.value = 'Please enter a valid email address.'
    return
  }
  sending.value   = true
  sendError.value = ''
  try {
    await api.sendInvoice(inv.value!.id, sanitizeEmail(sendEmail.value))
    showSendModal.value = false
    await store.fetchOne(route.params.id as string)
  } catch (e: any) {
    sendError.value = e?.response?.data?.message ?? 'Failed to send invoice. Check your email settings.'
  } finally {
    sending.value = false
  }
}

function fmtDate(d: string) {
  return new Date(d).toLocaleDateString('en-ZA', { day: '2-digit', month: 'short', year: 'numeric' })
}
function fmtNum(n: number) {
  return n.toLocaleString('en-ZA', { minimumFractionDigits: 2 })
}

async function markPaid() {
  if (inv.value && inv.value.status !== InvoiceStatus.Paid) {
    const updated = await api.patchStatus(inv.value.id, InvoiceStatus.Paid)
    store.current = updated
  }
}

async function deleteInvoice() {
  if (inv.value && confirm(`Delete ${inv.value.invoiceNumber}?`)) {
    await store.remove(inv.value.id)
    router.push('/invoices')
  }
}
</script>

<style scoped>
.loading { color: #999; padding: 60px; text-align: center; font-size: 14px; }
.detail-layout { display: flex; flex-direction: column; gap: 0; }

/* Header */
.detail-header {
  display: flex;
  justify-content: space-between;
  align-items: flex-start;
  padding-bottom: 24px;
}
.inv-label {
  font-size: 11px;
  letter-spacing: 1.5px;
  color: #999;
  margin-bottom: 6px;
  font-weight: 600;
}
.inv-number {
  font-size: 42px;
  font-weight: 700;
  color: #1a1a1a;
  letter-spacing: -1px;
  line-height: 1.1;
}
.inv-meta {
  display: flex;
  align-items: center;
  gap: 10px;
  margin-top: 12px;
  font-size: 13px;
  color: #999;
}
.inv-meta strong { color: #1a1a1a; }
.due-overdue { color: #ef4444 !important; }
.sep { color: #ddd; }

.header-actions {
  display: flex;
  gap: 8px;
  align-items: center;
  flex-shrink: 0;
}
.btn-action {
  padding: 8px 16px;
  border-radius: 8px;
  background: #fff;
  border: 1px solid #e0e0e0;
  color: #666;
  font-family: inherit;
  font-size: 13px;
  cursor: pointer;
  transition: all .15s;
  white-space: nowrap;
}
.btn-action:hover { border-color: #1a1a1a; color: #1a1a1a; }

.btn-mark-paid {
  padding: 8px 18px;
  border-radius: 8px;
  background: #a3e635;
  color: #1a1a1a;
  border: none;
  font-family: inherit;
  font-size: 13px;
  font-weight: 700;
  cursor: pointer;
  transition: background .15s;
  white-space: nowrap;
}
.btn-mark-paid:hover { background: #bef264; }

.detail-divider {
  border-top: 1px solid #e0e0e0;
  margin-bottom: 24px;
}

/* Body */
.detail-body {
  display: grid;
  grid-template-columns: 1fr 280px;
  gap: 20px;
  align-items: flex-start;
}

/* Invoice content */
.invoice-content {
  background: #fff;
  border: 1px solid #e0e0e0;
  border-radius: 10px;
  padding: 28px;
  display: flex;
  flex-direction: column;
  gap: 28px;
}

/* Billed to */
.billed-card {
  background: #f9f9f9;
  border: 1px solid #e0e0e0;
  border-radius: 8px;
  padding: 16px 20px;
}
.field-label {
  font-size: 10px;
  letter-spacing: 1px;
  color: #999;
  margin-bottom: 8px;
  font-weight: 600;
}
.client-name  { font-size: 16px; font-weight: 700; color: #1a1a1a; }
.client-email { font-size: 13px; color: #999; margin-top: 4px; }

/* Line items table */
.items-table {
  width: 100%;
  border-collapse: collapse;
}
.items-table th {
  font-size: 11px;
  letter-spacing: .5px;
  color: #999;
  padding: 10px 12px;
  text-align: left;
  border-bottom: 1px solid #e0e0e0;
  font-weight: 600;
  background: #f9f9f9;
}
.th-right { text-align: right; }
.items-table td {
  padding: 16px 12px;
  border-bottom: 1px solid #f0f0f0;
  font-size: 14px;
  color: #1a1a1a;
}
.items-table tbody tr:last-child td { border-bottom: none; }
.td-desc  { color: #1a1a1a; font-weight: 500; }
.td-qty   { color: #666; }
.td-price { color: #666; }
.td-total { font-weight: 700; color: #1a1a1a; text-align: right; font-size: 16px; }

/* Totals */
.totals {
  display: flex;
  flex-direction: column;
  gap: 8px;
  align-items: flex-end;
}
.totals-row {
  display: flex;
  gap: 80px;
  font-size: 13px;
  color: #666;
}
.totals-divider {
  width: 280px;
  border-top: 1px solid #e0e0e0;
  margin: 4px 0;
}
.totals-row.grand {
  font-size: 16px;
  font-weight: 700;
  color: #1a1a1a;
}
.grand-value { color: #1a1a1a; }

/* Sidebar */
.detail-sidebar {
  display: flex;
  flex-direction: column;
  gap: 14px;
  position: sticky;
  top: 72px;
}
.sidebar-card {
  background: #fff;
  border: 1px solid #e0e0e0;
  border-radius: 10px;
  padding: 18px;
}
.section-label {
  font-size: 11px;
  letter-spacing: 1.5px;
  color: #999;
  margin-bottom: 14px;
  font-weight: 600;
}

.activity-entry {
  display: flex;
  gap: 10px;
  margin-bottom: 14px;
}
.activity-entry:last-child { margin-bottom: 0; }
.activity-dot {
  width: 8px;
  height: 8px;
  border-radius: 50%;
  flex-shrink: 0;
  margin-top: 4px;
}
.dot-green  { background: #10b981; }
.dot-blue   { background: #3b82f6; }
.dot-yellow { background: #f59e0b; }
.dot-red    { background: #ef4444; }
.activity-label { font-size: 13px; color: #1a1a1a; font-weight: 600; }
.activity-date  { font-size: 11px; color: #999; margin-top: 2px; }
.activity-empty { font-size: 13px; color: #999; }

.notes-text { font-size: 13px; color: #666; line-height: 1.6; }

.btn-edit {
  width: 100%;
  padding: 10px;
  background: #fff;
  border: 1px solid #e0e0e0;
  border-radius: 8px;
  color: #666;
  font-family: inherit;
  font-size: 13px;
  cursor: pointer;
  transition: all .15s;
}
.btn-edit:hover { border-color: #1a1a1a; color: #1a1a1a; }

.btn-delete {
  width: 100%;
  padding: 10px;
  background: #fff;
  border: 1px solid #fecaca;
  border-radius: 8px;
  color: #ef4444;
  font-family: inherit;
  font-size: 13px;
  cursor: pointer;
  transition: all .15s;
}
.btn-delete:hover { background: #fef2f2; border-color: #ef4444; }

/* Send modal */
.modal-overlay {
  position: fixed;
  inset: 0;
  background: rgba(0, 0, 0, .4);
  display: flex;
  align-items: center;
  justify-content: center;
  z-index: 200;
}
.modal {
  background: #fff;
  border-radius: 12px;
  padding: 28px;
  width: 440px;
  box-shadow: 0 20px 60px rgba(0,0,0,.15);
}
.modal-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
  margin-bottom: 8px;
}
.modal-header h2 {
  font-size: 18px;
  font-weight: 700;
  color: #1a1a1a;
}
.modal-close {
  background: none;
  border: none;
  font-size: 22px;
  color: #999;
  cursor: pointer;
  line-height: 1;
  padding: 0;
}
.modal-close:hover { color: #1a1a1a; }
.modal-sub {
  font-size: 13px;
  color: #666;
  margin-bottom: 20px;
  line-height: 1.5;
}
.modal-sub strong { color: #1a1a1a; }
.modal-field { display: flex; flex-direction: column; gap: 6px; margin-bottom: 16px; }
.modal-label { font-size: 11px; letter-spacing: .5px; color: #666; font-weight: 600; }
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
  font-size: 12px;
  color: #ef4444;
  margin-bottom: 12px;
  padding: 8px 12px;
  background: #fef2f2;
  border-radius: 6px;
  border: 1px solid #fecaca;
}
.modal-actions {
  display: flex;
  gap: 10px;
  justify-content: flex-end;
  margin-top: 4px;
}
.modal-btn-cancel {
  padding: 9px 18px;
  background: #fff;
  border: 1px solid #e0e0e0;
  border-radius: 8px;
  color: #666;
  font-family: inherit;
  font-size: 13px;
  cursor: pointer;
  transition: all .15s;
}
.modal-btn-cancel:hover { border-color: #1a1a1a; color: #1a1a1a; }
.modal-btn-send {
  padding: 9px 20px;
  background: #1e293b;
  border: none;
  border-radius: 8px;
  color: #fff;
  font-family: inherit;
  font-size: 13px;
  font-weight: 700;
  cursor: pointer;
  transition: background .15s;
}
.modal-btn-send:hover:not(:disabled) { background: #0f172a; }
.modal-btn-send:disabled { opacity: .6; cursor: not-allowed; }
</style>
