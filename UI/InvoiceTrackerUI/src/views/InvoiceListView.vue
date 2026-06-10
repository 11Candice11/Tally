<template>
  <div class="list-layout">
    <div class="main-area">
      <!-- Header -->
      <div class="page-header">
        <div>
          <h1>Invoices</h1>
          <p class="page-sub">Manage your business billing and payment tracking.</p>
        </div>
        <div class="header-actions">
          <button :class="['btn-outline', { 'btn-outline--active': showFilters }]" @click="showFilters = !showFilters">
            ⊙ Filters{{ activeFilterCount > 0 ? ` (${activeFilterCount})` : '' }}
          </button>
          <button class="btn-outline">↓ Export</button>
        </div>
      </div>

      <!-- Filter panel -->
      <div v-if="showFilters" class="filter-panel">
        <div class="filter-row">
          <div class="filter-field">
            <label class="filter-label">CLIENT NAME</label>
            <input v-model="clientFilter" class="filter-input" placeholder="Search client…" />
          </div>
          <div class="filter-field">
            <label class="filter-label">FROM DATE</label>
            <input v-model="fromDate" type="date" class="filter-input" />
          </div>
          <div class="filter-field">
            <label class="filter-label">TO DATE</label>
            <input v-model="toDate" type="date" class="filter-input" />
          </div>
          <button class="filter-clear" @click="clearFilters">Clear</button>
        </div>
      </div>

      <!-- KPI Summary -->
      <div class="kpi-summary">
        <div class="kpi-summary-item">
          <p class="kpi-label">TOTAL OUTSTANDING</p>
          <p class="kpi-value">R {{ fmt(outstanding) }}</p>
          <p class="kpi-trend">{{ overdueCount > 0 ? overdueCount + ' overdue' : 'All up to date' }}</p>
        </div>
        <div class="kpi-summary-divider" />
        <div class="kpi-summary-item">
          <p class="kpi-label">PAID (30 DAYS)</p>
          <p class="kpi-value">R {{ fmt(paidMTD) }}</p>
        </div>
        <div class="kpi-summary-divider" />
        <div class="kpi-summary-item">
          <p class="kpi-label">PENDING</p>
          <p class="kpi-value">{{ pendingCount }} Invoices</p>
        </div>
        <div class="kpi-summary-divider" />
        <div class="kpi-summary-item">
          <p class="kpi-label">OVERDUE</p>
          <p class="kpi-value danger">{{ overdueCount }} Invoices</p>
        </div>
      </div>

      <!-- Tabs -->
      <div class="tabs-section">
        <div class="tabs">
          <button
            v-for="tab in tabs"
            :key="tab"
            :class="['tab', { active: statusFilter === tab }]"
            @click="statusFilter = tab"
          >{{ tab || 'All Invoices' }}</button>
        </div>
        <div class="sort-section">
          <span class="sort-label">Sort by:</span>
          <select v-model="sortBy" class="sort-select">
            <option value="date-desc">Latest First</option>
            <option value="date-asc">Oldest First</option>
            <option value="amount-desc">Amount: High to Low</option>
            <option value="amount-asc">Amount: Low to High</option>
          </select>
        </div>
      </div>

      <!-- Table -->
      <div class="table-wrapper">
        <table class="invoice-table">
          <thead>
            <tr>
              <th><input type="checkbox" /></th>
              <th>INVOICE / CLIENT</th>
              <th>STATUS</th>
              <th>ISSUE DATE</th>
              <th>DUE DATE</th>
              <th>AMOUNT</th>
              <th></th>
            </tr>
          </thead>
          <tbody>
            <tr
              v-for="inv in filtered"
              :key="inv.id"
              class="table-row"
              @click="router.push(`/invoices/${inv.id}`)"
            >
              <td class="td-checkbox" @click.stop><input type="checkbox" /></td>
              <td class="td-invoice">
                <div class="invoice-info">
                  <p class="invoice-num">{{ inv.invoiceNumber }}</p>
                  <p class="invoice-client">{{ inv.clientName }}</p>
                </div>
              </td>
              <td><span :class="['badge', `badge-${inv.status.toLowerCase()}`]">{{ inv.status.toUpperCase() }}</span></td>
              <td class="td-date">{{ fmtDate(inv.issueDate) }}</td>
              <td class="td-date">{{ fmtDate(inv.dueDate) }}</td>
              <td class="td-amount">R {{ fmtNum(invoiceTotal(inv)) }}</td>
              <td class="td-actions" @click.stop>
                <button class="action-btn" @click="router.push(`/invoices/${inv.id}`)">⋮</button>
              </td>
            </tr>
            <tr v-if="filtered.length === 0">
              <td colspan="7" class="empty-row">No invoices match your filters.</td>
            </tr>
          </tbody>
        </table>
      </div>

      <!-- Pagination -->
      <div class="pagination">
        <span class="pagination-info">Showing {{ filtered.length }} of {{ store.invoices.length }} invoices</span>
        <div class="pagination-controls">
          <button class="pagination-btn">‹</button>
          <button class="pagination-btn">›</button>
        </div>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, computed, onMounted } from 'vue'
import { useRouter } from 'vue-router'
import { useInvoiceStore } from '@/stores/invoiceStore'
import { invoiceTotal, InvoiceStatus } from '@/models/invoice'

const router = useRouter()
const store  = useInvoiceStore()

const statusFilter = ref<string>('')
const showFilters  = ref(false)
const clientFilter = ref('')
const fromDate     = ref('')
const toDate       = ref('')
const sortBy       = ref('date-desc')

onMounted(() => store.fetchAll())

const tabs = [
  '' as const,
  'Draft' as const,
  'Sent' as const,
  'Paid' as const,
  'Overdue' as const,
  'Cancelled' as const,
]

// Computed KPIs from local invoice list
const outstanding = computed(() =>
  store.invoices
    .filter(i => i.status === InvoiceStatus.Sent || i.status === InvoiceStatus.Overdue)
    .reduce((s, i) => s + invoiceTotal(i), 0)
)
const overdueCount = computed(() =>
  store.invoices.filter(i => i.status === InvoiceStatus.Overdue).length
)
const paidMTD = computed(() =>
  store.invoices
    .filter(i => i.status === InvoiceStatus.Paid)
    .reduce((s, i) => s + invoiceTotal(i), 0)
)
const pendingCount = computed(() =>
  store.invoices.filter(i => i.status === InvoiceStatus.Sent).length
)

const activeFilterCount = computed(() =>
  [clientFilter.value, fromDate.value, toDate.value].filter(Boolean).length
)

function clearFilters() {
  clientFilter.value = ''
  fromDate.value     = ''
  toDate.value       = ''
}

const filtered = computed(() => {
  let list = store.invoices.filter(i => {
    if (statusFilter.value && i.status !== statusFilter.value) return false
    if (clientFilter.value && !i.clientName.toLowerCase().includes(clientFilter.value.toLowerCase())) return false
    if (fromDate.value && i.issueDate < fromDate.value) return false
    if (toDate.value   && i.issueDate > toDate.value)   return false
    return true
  })

  switch (sortBy.value) {
    case 'date-asc':    list = [...list].sort((a, b) => a.issueDate.localeCompare(b.issueDate)); break
    case 'date-desc':   list = [...list].sort((a, b) => b.issueDate.localeCompare(a.issueDate)); break
    case 'amount-asc':  list = [...list].sort((a, b) => invoiceTotal(a) - invoiceTotal(b)); break
    case 'amount-desc': list = [...list].sort((a, b) => invoiceTotal(b) - invoiceTotal(a)); break
  }

  return list
})

function fmt(n: number) {
  return n.toLocaleString('en-ZA', { minimumFractionDigits: 0 })
}

function fmtDate(d: string) {
  return new Date(d).toLocaleDateString('en-ZA', { day: '2-digit', month: 'short', year: 'numeric' })
}

function fmtNum(n: number) {
  return n.toLocaleString('en-ZA', { minimumFractionDigits: 2 })
}
</script>

<style scoped>
.list-layout { display: flex; gap: 0; }

.main-area { flex: 1; display: flex; flex-direction: column; gap: 20px; }

/* Header */
.page-header {
  display: flex;
  justify-content: space-between;
  align-items: flex-end;
}
.page-header h1 {
  font-size: 28px;
  font-weight: 700;
  color: #1a1a1a;
}
.page-sub { font-size: 13px; color: #999; margin-top: 4px; }

.header-actions { display: flex; gap: 10px; }

.btn-outline {
  padding: 8px 14px;
  background: #fff;
  border: 1px solid #e0e0e0;
  border-radius: 6px;
  color: #666;
  font-family: inherit;
  font-size: 13px;
  cursor: pointer;
  transition: all .15s;
}
.btn-outline:hover { border-color: #1a1a1a; color: #1a1a1a; }

.btn-outline--active {
  border-color: #1e293b;
  color: #1e293b;
  background: #f0f4f8;
}

/* Filter panel */
.filter-panel {
  background: #fff;
  border: 1px solid #e0e0e0;
  border-radius: 10px;
  padding: 16px 20px;
}
.filter-row {
  display: flex;
  gap: 16px;
  align-items: flex-end;
}
.filter-field {
  display: flex;
  flex-direction: column;
  gap: 6px;
  flex: 1;
}
.filter-label {
  font-size: 10px;
  letter-spacing: .8px;
  color: #999;
  font-weight: 600;
}
.filter-input {
  background: #f9f9f9;
  border: 1px solid #e0e0e0;
  border-radius: 6px;
  color: #1a1a1a;
  font-family: inherit;
  font-size: 13px;
  padding: 8px 10px;
  outline: none;
  transition: border-color .15s;
  width: 100%;
}
.filter-input:focus { border-color: #1e293b; }
.filter-clear {
  padding: 8px 16px;
  background: none;
  border: 1px solid #e0e0e0;
  border-radius: 6px;
  color: #999;
  font-family: inherit;
  font-size: 13px;
  cursor: pointer;
  white-space: nowrap;
  transition: all .15s;
  align-self: flex-end;
}
.filter-clear:hover { border-color: #ef4444; color: #ef4444; }

.empty-row {
  text-align: center;
  color: #999;
  font-size: 13px;
  padding: 40px !important;
}

/* KPI Summary */
.kpi-summary {
  display: flex;
  align-items: center;
  gap: 0;
  background: #fff;
  border: 1px solid #e0e0e0;
  border-radius: 10px;
  padding: 20px 24px;
}
.kpi-summary-item {
  flex: 1;
  display: flex;
  flex-direction: column;
  gap: 4px;
}
.kpi-label { font-size: 11px; letter-spacing: 1px; color: #999; font-weight: 600; }
.kpi-value { font-size: 20px; font-weight: 700; color: #1a1a1a; }
.kpi-value.danger { color: #ef4444; }
.kpi-trend { font-size: 12px; color: #999; }
.kpi-trend.down { color: #ef4444; }

.kpi-summary-divider {
  width: 1px;
  height: 40px;
  background: #e0e0e0;
  margin: 0 20px;
}

/* Tabs */
.tabs-section {
  display: flex;
  justify-content: space-between;
  align-items: center;
  border-bottom: 1px solid #e0e0e0;
  padding-bottom: 0;
}
.tabs { display: flex; gap: 0; }
.tab {
  padding: 12px 16px;
  background: none;
  border: none;
  border-bottom: 2px solid transparent;
  color: #999;
  font-family: inherit;
  font-size: 13px;
  cursor: pointer;
  transition: all .15s;
}
.tab:hover { color: #1a1a1a; }
.tab.active { color: #1a1a1a; border-bottom-color: #1a1a1a; }

.sort-section { display: flex; align-items: center; gap: 10px; }
.sort-label { font-size: 12px; color: #999; }
.sort-select {
  background: #fff;
  border: 1px solid #e0e0e0;
  border-radius: 6px;
  color: #1a1a1a;
  font-family: inherit;
  font-size: 12px;
  padding: 6px 10px;
  cursor: pointer;
}

/* Table */
.table-wrapper {
  background: #fff;
  border: 1px solid #e0e0e0;
  border-radius: 10px;
  overflow: hidden;
}
.invoice-table {
  width: 100%;
  border-collapse: collapse;
}
.invoice-table th {
  font-size: 11px;
  letter-spacing: .5px;
  color: #999;
  padding: 14px 16px;
  text-align: left;
  border-bottom: 1px solid #e0e0e0;
  font-weight: 600;
  background: #f9f9f9;
}
.table-row {
  cursor: pointer;
  transition: background .1s;
}
.table-row:hover { background: #f9f9f9; }
.invoice-table td {
  padding: 14px 16px;
  border-bottom: 1px solid #f0f0f0;
  color: #666;
  font-size: 13px;
}
.invoice-table tbody tr:last-child td { border-bottom: none; }

.td-checkbox { width: 40px; }
.td-checkbox input { cursor: pointer; }
.invoice-info { display: flex; flex-direction: column; gap: 2px; }
.invoice-num { font-weight: 700; color: #1a1a1a; }
.invoice-client { font-size: 12px; color: #999; }
.td-date { color: #999; }
.td-amount { font-weight: 600; color: #1a1a1a; }
.td-actions { text-align: right; }

.action-btn {
  background: none;
  border: 1px solid #e0e0e0;
  border-radius: 4px;
  color: #999;
  font-size: 14px;
  padding: 2px 8px;
  cursor: pointer;
  transition: all .15s;
}
.action-btn:hover { border-color: #1a1a1a; color: #1a1a1a; }

/* Pagination */
.pagination {
  display: flex;
  justify-content: space-between;
  align-items: center;
  padding: 16px 0;
  font-size: 12px;
  color: #999;
}
.pagination-controls { display: flex; gap: 8px; }
.pagination-btn {
  background: #fff;
  border: 1px solid #e0e0e0;
  border-radius: 4px;
  color: #666;
  font-size: 14px;
  padding: 4px 8px;
  cursor: pointer;
  transition: all .15s;
}
.pagination-btn:hover { border-color: #1a1a1a; color: #1a1a1a; }

.bulk-actions { padding: 16px 0; }
.btn-bulk {
  background: #1e293b;
  border: none;
  border-radius: 6px;
  color: #fff;
  font-family: inherit;
  font-size: 13px;
  font-weight: 700;
  padding: 10px 16px;
  cursor: pointer;
}
</style>
