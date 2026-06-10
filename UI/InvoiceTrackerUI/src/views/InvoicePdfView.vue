<template>
  <div class="pdf-shell">
    <!-- Top toolbar -->
    <div class="pdf-toolbar">
      <div class="toolbar-left">
        <button class="tb-btn" @click="router.back()">← Back</button>
        <span class="toolbar-title">Invoice Preview</span>
      </div>
      <div class="toolbar-right">
        <button class="tb-btn" @click="printInvoice">🖨 Print</button>
        <button class="btn-download" @click="printInvoice">↓ Download PDF</button>
      </div>
    </div>

    <div class="pdf-body">
      <!-- PDF canvas area -->
      <div class="pdf-canvas-wrap">
        <div class="pdf-page" :style="{ transform: `scale(${zoom / 100})`, transformOrigin: 'top center' }">
          <!-- Page 1 -->
          <div class="page-content">
            <!-- Header -->
            <div class="doc-header">
              <div class="doc-brand">
                <div class="doc-logo-block">
                  <span class="doc-logo-icon">⬡</span>
                </div>
                <div>
                  <p class="doc-company">TALLY  Inc.</p>
                  <p class="doc-tagline">Design &amp; Development Services</p>
                </div>
              </div>
              <div class="doc-title-block">
                <p class="doc-inv-label">INVOICE NUMBER</p>
                <p class="doc-inv-num">{{ inv?.invoiceNumber }}</p>
                <span v-if="inv" :class="['status-pill', `status-${inv.status.toLowerCase()}`]">
                  ● {{ inv.status.toUpperCase() }}
                </span>
              </div>
            </div>

            <div class="doc-divider" />

            <!-- Issued by / Bill to -->
            <div class="doc-parties" v-if="inv">
              <div class="party-block">
                <p class="party-label">ISSUED BY</p>
                <p class="party-name">TALLY </p>
                <p class="party-line">123 Creative Plaza, Suite 400</p>
                <p class="party-line">Somerset West, South Africa</p>
                <p class="party-line">billing@tally.com</p>
              </div>
              <div class="party-block">
                <p class="party-label">BILL TO</p>
                <p class="party-name">{{ inv.clientName }}</p>
                <p class="party-line">{{ inv.clientEmail }}</p>
              </div>
            </div>

            <!-- Date row -->
            <div class="doc-dates" v-if="inv">
              <div class="date-cell">
                <p class="date-label">INVOICE DATE</p>
                <p class="date-val">{{ fmtDate(inv.issueDate) }}</p>
              </div>
              <div class="date-cell">
                <p class="date-label">DUE DATE</p>
                <p class="date-val">{{ fmtDate(inv.dueDate) }}</p>
              </div>
              <div class="date-cell">
                <p class="date-label">CURRENCY</p>
                <p class="date-val">ZAR - South African Rand</p>
              </div>
            </div>

            <!-- Line items -->
            <table class="doc-table" v-if="inv">
              <thead>
                <tr>
                  <th>DESCRIPTION</th>
                  <th>QTY</th>
                  <th>RATE</th>
                  <th class="th-right">AMOUNT</th>
                </tr>
              </thead>
              <tbody>
                <tr v-for="item in inv.lineItems" :key="item.id">
                  <td class="td-desc">{{ item.description }}</td>
                  <td>{{ item.qty }}</td>
                  <td>R {{ fmtNum(item.unitPrice) }}</td>
                  <td class="td-amount">R {{ fmtNum(item.qty * item.unitPrice) }}</td>
                </tr>
              </tbody>
            </table>

            <!-- Totals -->
            <div class="doc-totals" v-if="inv">
              <div class="totals-row">
                <span>Subtotal</span>
                <span>R {{ fmtNum(subtotal) }}</span>
              </div>
              <div class="totals-row">
                <span>Vat ({{ vatPct }}.0%)</span>
                <span>R {{ fmtNum(vat) }}</span>
              </div>
              <div class="totals-divider" />
              <div class="totals-row grand">
                <span>Total Amount</span>
                <span>R {{ fmtNum(total) }}</span>
              </div>
            </div>

            <!-- Notes / Payment instructions -->
            <div class="doc-footer" v-if="inv?.notes">
              <p class="footer-label">PAYMENT INSTRUCTIONS</p>
              <p class="footer-text">{{ inv.notes }}</p>
              <p class="footer-thanks">Thank you for your business.</p>
            </div>
            <div class="doc-footer" v-else>
              <p class="footer-thanks">Thank you for your business.</p>
            </div>
          </div>
        </div>
      </div>

      <!-- Right sidebar -->
      <aside class="doc-info-panel">
        <!-- Document info -->
        <div class="panel-section">
          <p class="panel-label">DOCUMENT INFO</p>
          <div class="info-grid" v-if="inv">
            <span class="info-key">Invoice</span>
            <span class="info-val">{{ inv.invoiceNumber }}</span>
            <span class="info-key">Client</span>
            <span class="info-val">{{ inv.clientName.split(' ').slice(0,2).join(' ') }}</span>
            <span class="info-key">Status</span>
            <span><span :class="['status-pill', `status-${inv.status.toLowerCase()}`]">{{ inv.status.toUpperCase() }}</span></span>
            <span class="info-key">Amount</span>
            <span class="info-val accent">R {{ fmtNum(total) }}</span>
            <span class="info-key">Due Date</span>
            <span class="info-val" :class="{ 'accent-red': inv.status === 'Overdue' }">{{ fmtDate(inv.dueDate) }}</span>
          </div>
        </div>

        <div class="panel-divider" />

        <!-- Actions -->
        <div class="panel-section">
          <button class="action-btn primary" @click="printInvoice">↓ Download PDF</button>
          <button class="action-btn" @click="router.push(`/invoices/${inv?.id}`)">= Email to client</button>
          <button class="action-btn" @click="router.push(`/invoices/${inv?.id}/edit`)">✎ Edit invoice</button>
          <button class="action-btn danger">✕ Void invoice</button>
        </div>

        <div class="panel-divider" />

        <!-- Zoom -->
        <div class="panel-section">
          <p class="panel-label">ZOOM</p>
          <div class="zoom-controls">
            <button class="zoom-btn" @click="zoom = Math.max(50, zoom - 10)">−</button>
            <span class="zoom-val">{{ zoom }}%</span>
            <button class="zoom-btn" @click="zoom = Math.min(200, zoom + 10)">+</button>
            <button class="zoom-btn" @click="zoom = 100">Reset</button>
          </div>
        </div>
      </aside>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, computed, onMounted } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { useInvoiceStore } from '../stores/invoiceStore'
import { invoiceSubtotal, invoiceVat, invoiceTotal } from '../models/invoice'

const route  = useRoute()
const router = useRouter()
const store  = useInvoiceStore()

const zoom = ref(100)

onMounted(() => store.fetchOne(route.params.id as string))

const inv      = computed(() => store.current)
const subtotal = computed(() => inv.value ? invoiceSubtotal(inv.value) : 0)
const vat      = computed(() => inv.value ? invoiceVat(inv.value) : 0)
const total    = computed(() => inv.value ? invoiceTotal(inv.value) : 0)
const vatPct   = computed(() => inv.value ? Math.round(inv.value.vatRate * 100) : 15)

function fmtDate(d: string) {
  return new Date(d).toLocaleDateString('en-ZA', { day: '2-digit', month: 'long', year: 'numeric' })
}
function fmtNum(n: number) {
  return n.toLocaleString('en-ZA', { minimumFractionDigits: 2 })
}

function printInvoice() {
  window.print()
}
</script>

<style scoped>
/* ── Shell ─────────────────────────────────────────────── */
.pdf-shell {
  display: flex;
  flex-direction: column;
  height: calc(100vh - 56px);
  margin: -28px;
  background: #f5f5f5;
  overflow: hidden;
}

/* ── Toolbar ───────────────────────────────────────────── */
.pdf-toolbar {
  display: flex;
  align-items: center;
  justify-content: space-between;
  padding: 0 20px;
  height: 52px;
  background: #fff;
  border-bottom: 1px solid #e0e0e0;
  flex-shrink: 0;
}
.toolbar-left, .toolbar-right { display: flex; align-items: center; gap: 10px; }
.toolbar-title { font-size: 15px; font-weight: 700; color: #1a1a1a; }

.tb-btn {
  background: #fff;
  border: 1px solid #e0e0e0;
  border-radius: 6px;
  color: #666;
  font-family: inherit;
  font-size: 13px;
  padding: 6px 14px;
  cursor: pointer;
  transition: all .15s;
}
.tb-btn:hover { border-color: #1a1a1a; color: #1a1a1a; }

.btn-download {
  padding: 7px 16px;
  background: #1e293b;
  border: none;
  border-radius: 6px;
  color: #fff;
  font-family: inherit;
  font-size: 13px;
  font-weight: 700;
  cursor: pointer;
  transition: background .15s;
}
.btn-download:hover { background: #0f172a; }

.tb-icon-btn {
  background: none;
  border: none;
  color: #666;
  font-size: 16px;
  padding: 6px;
  cursor: pointer;
}
.topbar-avatar {
  width: 32px; height: 32px;
  border-radius: 50%;
  background: #1e293b;
  display: flex; align-items: center; justify-content: center;
  font-size: 11px; font-weight: 700; color: #fff;
}

/* ── Body ──────────────────────────────────────────────── */
.pdf-body {
  display: grid;
  grid-template-columns: 1fr 280px;
  flex: 1;
  overflow: hidden;
}

/* ── Canvas ────────────────────────────────────────────── */
.pdf-canvas-wrap {
  overflow: auto;
  background: #e8e8e8;
  display: flex;
  justify-content: center;
  padding: 32px 24px;
}

.pdf-page {
  width: 100%;
  max-width: 680px;
  flex-shrink: 0;
  background: #fff;
  border-radius: 4px;
  box-shadow: 0 4px 24px rgba(0,0,0,.12);
  transition: transform .15s;
}

.page-content {
  padding: 40px 44px;
  color: #1a1a1a;
  font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', sans-serif;
}

/* Doc header */
.doc-header {
  display: flex;
  justify-content: space-between;
  align-items: flex-start;
  margin-bottom: 24px;
}
.doc-brand { display: flex; align-items: center; gap: 14px; }
.doc-logo-block {
  width: 44px; height: 44px;
  background: #1e293b;
  border-radius: 10px;
  display: flex; align-items: center; justify-content: center;
  font-size: 20px; color: #fff;
}
.doc-company { font-size: 16px; font-weight: 700; color: #1a1a1a; }
.doc-tagline { font-size: 12px; color: #999; margin-top: 2px; }

.doc-title-block { text-align: right; }
.doc-inv-label { font-size: 10px; letter-spacing: 1px; color: #999; font-weight: 600; margin-bottom: 4px; }
.doc-inv-num { font-size: 16px; font-weight: 700; color: #1a1a1a; margin-bottom: 8px; }

/* Status pills */
.status-pill {
  display: inline-flex;
  align-items: center;
  gap: 5px;
  padding: 4px 10px;
  border-radius: 20px;
  font-size: 11px;
  font-weight: 700;
  letter-spacing: .5px;
}
.status-paid     { background: #d1fae5; color: #065f46; }
.status-sent     { background: #dbeafe; color: #1e40af; }
.status-overdue  { background: #fee2e2; color: #991b1b; }
.status-draft    { background: #f3f4f6; color: #374151; }
.status-cancelled { background: #f3f4f6; color: #6b7280; }

.doc-divider { border-top: 1px solid #e0e0e0; margin: 20px 0; }

/* Parties */
.doc-parties {
  display: grid;
  grid-template-columns: 1fr 1fr;
  gap: 32px;
  margin-bottom: 20px;
}
.party-label { font-size: 10px; letter-spacing: 1px; color: #999; font-weight: 600; margin-bottom: 8px; }
.party-name  { font-size: 14px; font-weight: 700; color: #1a1a1a; margin-bottom: 4px; }
.party-line  { font-size: 12px; color: #666; line-height: 1.7; }

/* Dates */
.doc-dates {
  display: grid;
  grid-template-columns: repeat(3, 1fr);
  gap: 0;
  background: #f9f9f9;
  border: 1px solid #e0e0e0;
  border-radius: 8px;
  padding: 14px 20px;
  margin-bottom: 24px;
}
.date-cell { display: flex; flex-direction: column; gap: 4px; }
.date-label { font-size: 10px; letter-spacing: 1px; color: #999; font-weight: 600; }
.date-val   { font-size: 13px; color: #1a1a1a; font-weight: 500; }

/* Table */
.doc-table { width: 100%; border-collapse: collapse; }
.doc-table th {
  font-size: 11px;
  letter-spacing: .5px;
  color: #999;
  padding: 10px 0;
  text-align: left;
  border-bottom: 1px solid #e0e0e0;
  font-weight: 600;
}
.th-right { text-align: right; }
.doc-table td {
  padding: 16px 0;
  font-size: 13px;
  color: #1a1a1a;
  border-bottom: 1px solid #f0f0f0;
}
.doc-table tbody tr:last-child td { border-bottom: none; }
.td-desc   { color: #1a1a1a; font-weight: 500; word-break: break-word; overflow-wrap: break-word; max-width: 280px; }
.td-amount { font-weight: 700; font-size: 16px; text-align: right; white-space: nowrap; }

/* Totals */
.doc-totals {
  display: flex;
  flex-direction: column;
  gap: 8px;
  align-items: flex-end;
  margin-top: 20px;
  padding-top: 16px;
  border-top: 1px solid #e0e0e0;
}
.totals-row {
  display: flex;
  justify-content: space-between;
  width: 100%;
  max-width: 320px;
  font-size: 13px;
  color: #666;
}
.totals-divider { width: 100%; max-width: 320px; border-top: 1px solid #e0e0e0; margin: 4px 0; }
.totals-row.grand { font-size: 18px; font-weight: 700; color: #1a1a1a; }

/* Footer */
.doc-footer {
  margin-top: 32px;
  padding-top: 20px;
  border-top: 1px solid #e0e0e0;
}
.footer-label { font-size: 10px; letter-spacing: 1px; color: #999; font-weight: 600; margin-bottom: 8px; }
.footer-text  { font-size: 12px; color: #666; line-height: 1.7; margin-bottom: 16px; }
.footer-thanks { font-size: 13px; font-weight: 600; color: #1a1a1a; text-align: right; }

/* ── Right panel ───────────────────────────────────────── */
.doc-info-panel {
  background: #fff;
  border-left: 1px solid #e0e0e0;
  padding: 20px;
  overflow-y: auto;
  display: flex;
  flex-direction: column;
  gap: 0;
}
.panel-section { display: flex; flex-direction: column; gap: 8px; padding: 16px 0; }
.panel-label { font-size: 11px; letter-spacing: 1px; color: #999; font-weight: 600; margin-bottom: 4px; }
.panel-divider { border-top: 1px solid #e0e0e0; }

.info-grid {
  display: grid;
  grid-template-columns: auto 1fr;
  gap: 8px 16px;
  align-items: center;
}
.info-key { font-size: 12px; color: #999; white-space: nowrap; }
.info-val { font-size: 12px; color: #1a1a1a; font-weight: 500; }
.info-val.accent { color: #1a1a1a; font-weight: 700; }
.accent-red { color: #ef4444 !important; }

.action-btn {
  width: 100%;
  padding: 9px 14px;
  border-radius: 7px;
  background: #fff;
  border: 1px solid #e0e0e0;
  color: #666;
  font-family: inherit;
  font-size: 13px;
  text-align: left;
  cursor: pointer;
  transition: all .15s;
}
.action-btn:hover { border-color: #1a1a1a; color: #1a1a1a; }
.action-btn.primary {
  background: #1e293b;
  border-color: #1e293b;
  color: #fff;
  font-weight: 700;
}
.action-btn.primary:hover { background: #0f172a; }
.action-btn.danger { color: #ef4444; border-color: #fecaca; }
.action-btn.danger:hover { background: #fef2f2; border-color: #ef4444; }

.zoom-controls { display: flex; align-items: center; gap: 8px; }
.zoom-btn {
  background: #f5f5f5;
  border: 1px solid #e0e0e0;
  border-radius: 5px;
  color: #666;
  font-family: inherit;
  font-size: 13px;
  padding: 4px 10px;
  cursor: pointer;
  transition: all .15s;
}
.zoom-btn:hover { border-color: #1a1a1a; color: #1a1a1a; }
.zoom-val { font-size: 13px; color: #1a1a1a; font-weight: 600; min-width: 40px; text-align: center; }

/* Print */
@media print {
  .pdf-shell { height: auto; margin: 0; }
  .pdf-toolbar, .doc-info-panel { display: none; }
  .pdf-body { display: block; }
  .pdf-canvas-wrap { padding: 0; background: white; overflow: visible; }
  .pdf-page { box-shadow: none; transform: none !important; width: 100%; }
}
</style>
