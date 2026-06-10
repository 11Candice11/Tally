<template>
  <div class="dashboard">
    <!-- Page header -->
    <div class="page-header">
      <div>
        <h1>Financial Overview</h1>
        <p class="page-sub">Welcome back. Here is your business health for {{ currentMonth }}.</p>
      </div>
      <div class="header-actions">
        <button class="btn-outline" @click="router.push('/reports')">Download Report</button>
        <button class="btn-primary" @click="router.push('/invoices/new')">Create Invoice</button>
      </div>
    </div>

    <!-- KPI Cards -->
    <div class="kpi-grid">
      <div class="kpi-card">
        <div class="kpi-top">
          <p class="kpi-label">TOTAL REVENUE</p>
          <span class="kpi-icon kpi-icon--blue">💳</span>
        </div>
        <p class="kpi-value">R {{ fmt(store.summary.totalRevenue) }}</p>
        <p v-if="revenueGrowth !== null" :class="['kpi-sub', revenueGrowth >= 0 ? 'up' : 'down']">
          {{ revenueGrowth >= 0 ? '↑' : '↓' }} {{ revenueGrowth >= 0 ? '+' : '' }}{{ revenueGrowth.toFixed(1) }}% from last month
        </p>
        <p v-else class="kpi-sub" style="color:#999">No data from last month</p>
      </div>
      <div class="kpi-card">
        <div class="kpi-top">
          <p class="kpi-label">OUTSTANDING</p>
          <span class="kpi-icon kpi-icon--blue">⊙</span>
        </div>
        <p class="kpi-value">R {{ fmt(store.summary.outstanding) }}</p>
        <p class="kpi-sub down">
          <span class="dot-warn">⊙</span>
          {{ store.summary.overdueCount }} invoice{{ store.summary.overdueCount !== 1 ? 's' : '' }} overdue
        </p>
      </div>
      <div class="kpi-card">
        <div class="kpi-top">
          <p class="kpi-label">PAID THIS MONTH</p>
          <span class="kpi-icon kpi-icon--blue">✓</span>
        </div>
        <p class="kpi-value">R {{ fmt(store.summary.paidThisMonth) }}</p>
        <p class="kpi-sub up">
          <span>✓</span> {{ recoveryRate !== null ? recoveryRate + '% recovery rate' : 'No invoices yet' }}
        </p>
      </div>
    </div>

    <!-- Chart + Activity -->
    <div class="lower">
      <!-- Bar chart -->
      <div class="chart-card">
        <div class="chart-header">
          <div>
            <p class="card-title">Revenue Trends</p>
            <p class="card-sub">Comparative analysis of monthly growth</p>
          </div>
          <button class="btn-period">Last 6 Months ▾</button>
        </div>
        <div class="bar-chart">
          <div v-for="(bar, i) in chartBars" :key="i" class="bar-col">
            <div
              class="bar"
              :class="{ 'bar--active': i === chartBars.length - 1 }"
              :style="{ height: bar.pct + '%' }"
            />
            <span class="bar-label">{{ bar.month }}</span>
          </div>
        </div>
      </div>

      <!-- Recent Activity -->
      <div class="activity-card">
        <p class="card-title">Recent Activity</p>
        <div class="activity-list">
          <div
            v-for="inv in recent"
            :key="inv.id"
            class="activity-row"
            @click="router.push(`/invoices/${inv.id}`)"
          >
            <span
              class="activity-dot"
              :class="`activity-dot--${inv.status.toLowerCase()}`"
            >▶</span>
            <div class="activity-info">
              <p class="activity-title">{{ activityLabel(inv.status) }}</p>
              <p class="activity-detail">{{ inv.clientName }} · {{ inv.invoiceNumber }}</p>
              <p class="activity-time">{{ relativeDate(inv.issueDate) }}</p>
            </div>
            <span
              class="activity-amount"
              :class="inv.status === 'Paid' ? 'amount--green' : ''"
            >
              {{ inv.status === 'Paid' ? '+' : '' }}R {{ fmt(invoiceTotal(inv)) }}
            </span>
          </div>
        </div>
        <button class="view-all-btn" @click="router.push('/invoices')">View All Activity</button>
      </div>
    </div>

    <!-- Active Invoices table -->
    <div class="table-card">
      <div class="table-header">
        <p class="card-title">Active Invoices</p>
        <button class="manage-all-btn" @click="router.push('/invoices')">Manage All</button>
      </div>
      <table class="invoice-table">
        <thead>
          <tr>
            <th>INVOICE</th>
            <th>CLIENT</th>
            <th>DUE DATE</th>
            <th>AMOUNT</th>
            <th>STATUS</th>
            <th></th>
          </tr>
        </thead>
        <tbody>
          <tr
            v-for="inv in activeInvoices"
            :key="inv.id"
            @click="router.push(`/invoices/${inv.id}`)"
          >
            <td class="td-inv-num">{{ inv.invoiceNumber }}</td>
            <td class="td-client">{{ inv.clientName }}</td>
            <td class="td-date">{{ fmtDate(inv.dueDate) }}</td>
            <td class="td-amount">R {{ fmt(invoiceTotal(inv)) }}</td>
            <td>
              <span :class="['badge', `badge-${inv.status.toLowerCase()}`]">
                {{ inv.status.toUpperCase() }}
              </span>
            </td>
            <td class="td-actions" @click.stop>
              <button class="action-menu-btn" @click="router.push(`/invoices/${inv.id}`)">⋮</button>
            </td>
          </tr>
        </tbody>
      </table>
    </div>
  </div>
</template>

<script setup lang="ts">
import { computed, onMounted } from 'vue'
import { useRouter } from 'vue-router'
import { useInvoiceStore } from '@/stores/invoiceStore'
import { invoiceTotal, InvoiceStatus } from '@/models/invoice'

const router = useRouter()
const store  = useInvoiceStore()

onMounted(() => {
  store.fetchAll()
  store.fetchSummary()
})

const currentMonth = new Date().toLocaleDateString('en-ZA', { month: 'long', year: 'numeric' })

const recent = computed(() => store.invoices.slice(0, 4))

const activeInvoices = computed(() =>
  store.invoices
    .filter(i => i.status !== InvoiceStatus.Draft)
    .slice(0, 6)
)

// Revenue growth: this month vs last month — both values come from the server summary
const revenueGrowth = computed(() => {
  const last = store.summary.paidLastMonth
  if (last === 0) return null
  return ((store.summary.paidThisMonth - last) / last) * 100
})

// Recovery rate = paid / (paid + overdue + sent), using server counts
const recoveryRate = computed(() => {
  const paid    = store.invoices.filter(i => i.status === InvoiceStatus.Paid).length
  const denominator = paid + store.summary.overdueCount + store.summary.pendingCount
  if (denominator === 0) return null
  return Math.round((paid / denominator) * 100)
})

function fmt(n: number) {
  return n.toLocaleString('en-ZA', { minimumFractionDigits: 0 })
}

function fmtDate(d: string) {
  return new Date(d).toLocaleDateString('en-ZA', { day: '2-digit', month: 'short', year: 'numeric' })
}

function activityLabel(status: string) {
  const map: Record<string, string> = {
    Paid:    'Invoice Paid',
    Sent:    'Invoice Sent',
    Overdue: 'Payment Overdue',
    Draft:   'Draft Saved',
  }
  return map[status] ?? 'Invoice Updated'
}

function relativeDate(dateStr: string) {
  const diff = Date.now() - new Date(dateStr).getTime()
  const hours = Math.floor(diff / 3_600_000)
  if (hours < 1)   return 'Just now'
  if (hours < 24)  return `${hours} hour${hours !== 1 ? 's' : ''} ago`
  const days = Math.floor(hours / 24)
  if (days === 1)  return 'Yesterday'
  if (days < 7)    return `${days} days ago`
  return fmtDate(dateStr)
}

const chartBars = [
  { month: 'Nov', pct: 38 },
  { month: 'Dec', pct: 45 },
  { month: 'Jan', pct: 52 },
  { month: 'Feb', pct: 60 },
  { month: 'Mar', pct: 55 },
  { month: 'Apr', pct: 88 },
]
</script>

<style scoped>
.dashboard { display: flex; flex-direction: column; gap: 24px; }

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
  letter-spacing: -.5px;
}
.page-sub { font-size: 13px; color: #666; margin-top: 4px; }

.header-actions { display: flex; gap: 10px; align-items: center; }

.btn-outline {
  padding: 8px 16px;
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

.btn-primary {
  padding: 8px 18px;
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
.btn-primary:hover { background: #0f172a; }

/* KPI */
.kpi-grid {
  display: grid;
  grid-template-columns: repeat(3, 1fr);
  gap: 16px;
}
.kpi-card {
  background: #fff;
  border: 1px solid #e0e0e0;
  border-radius: 10px;
  padding: 20px 24px;
  display: flex;
  flex-direction: column;
  gap: 8px;
}
.kpi-top {
  display: flex;
  justify-content: space-between;
  align-items: center;
}
.kpi-label { font-size: 11px; letter-spacing: 1px; color: #999; font-weight: 600; }
.kpi-icon {
  width: 32px; height: 32px;
  border-radius: 8px;
  display: flex; align-items: center; justify-content: center;
  font-size: 14px;
}
.kpi-icon--blue { background: #dbeafe; }
.kpi-value {
  font-size: 28px;
  font-weight: 700;
  color: #1a1a1a;
  letter-spacing: -.5px;
}
.kpi-sub { font-size: 12px; display: flex; align-items: center; gap: 5px; }
.kpi-sub.up   { color: #10b981; }
.kpi-sub.down { color: #ef4444; }
.dot-warn { color: #ef4444; }

/* Lower grid */
.lower {
  display: grid;
  grid-template-columns: 1fr 300px;
  gap: 16px;
}

/* Chart */
.chart-card {
  background: #fff;
  border: 1px solid #e0e0e0;
  border-radius: 10px;
  padding: 22px 24px;
}
.chart-header {
  display: flex;
  justify-content: space-between;
  align-items: flex-start;
  margin-bottom: 24px;
}
.card-title { font-size: 15px; font-weight: 700; color: #1a1a1a; }
.card-sub   { font-size: 12px; color: #999; margin-top: 3px; }

.btn-period {
  background: #f5f5f5;
  border: 1px solid #e0e0e0;
  border-radius: 6px;
  color: #666;
  font-family: inherit;
  font-size: 12px;
  padding: 6px 10px;
  cursor: pointer;
  white-space: nowrap;
}

.bar-chart {
  display: flex;
  align-items: flex-end;
  gap: 10px;
  height: 180px;
}
.bar-col {
  flex: 1;
  display: flex;
  flex-direction: column;
  align-items: center;
  justify-content: flex-end;
  gap: 8px;
  height: 100%;
}
.bar {
  width: 100%;
  background: #e0e0e0;
  border-radius: 4px 4px 0 0;
  transition: height .3s;
  min-height: 6px;
}
.bar--active {
  background: linear-gradient(to top, #0d9488, #14b8a6);
}
.bar-label { font-size: 11px; color: #999; }
.bar-col:last-child .bar-label { color: #0d9488; }

/* Activity */
.activity-card {
  background: #fff;
  border: 1px solid #e0e0e0;
  border-radius: 10px;
  padding: 22px 20px;
  display: flex;
  flex-direction: column;
}
.activity-list {
  flex: 1;
  display: flex;
  flex-direction: column;
  gap: 0;
  margin-top: 16px;
}
.activity-row {
  display: flex;
  align-items: flex-start;
  gap: 10px;
  padding: 10px 0;
  border-bottom: 1px solid #f0f0f0;
  cursor: pointer;
  transition: background .1s;
}
.activity-row:last-child { border-bottom: none; }
.activity-row:hover { background: #f9f9f9; margin: 0 -20px; padding: 10px 20px; }

.activity-dot {
  font-size: 8px;
  margin-top: 3px;
  flex-shrink: 0;
}
.activity-dot--paid    { color: #10b981; }
.activity-dot--sent    { color: #3b82f6; }
.activity-dot--overdue { color: #ef4444; }
.activity-dot--draft   { color: #999; }

.activity-info { flex: 1; min-width: 0; }
.activity-title  { font-size: 13px; font-weight: 600; color: #1a1a1a; }
.activity-detail { font-size: 12px; color: #999; margin-top: 2px; white-space: nowrap; overflow: hidden; text-overflow: ellipsis; }
.activity-time   { font-size: 11px; color: #ccc; margin-top: 3px; }

.activity-amount {
  font-size: 13px;
  font-weight: 700;
  color: #999;
  flex-shrink: 0;
}
.amount--green { color: #10b981; }

.view-all-btn {
  margin-top: 14px;
  background: none;
  border: none;
  color: #999;
  font-family: inherit;
  font-size: 12px;
  cursor: pointer;
  text-align: center;
  padding: 6px 0;
  transition: color .15s;
}
.view-all-btn:hover { color: #1a1a1a; }

/* Table */
.table-card {
  background: #fff;
  border: 1px solid #e0e0e0;
  border-radius: 10px;
  padding: 22px 24px;
}
.table-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
  margin-bottom: 20px;
}
.manage-all-btn {
  background: none;
  border: none;
  color: #999;
  font-family: inherit;
  font-size: 13px;
  cursor: pointer;
  transition: color .15s;
}
.manage-all-btn:hover { color: #1a1a1a; }

.invoice-table {
  width: 100%;
  border-collapse: collapse;
}
.invoice-table th {
  font-size: 11px;
  letter-spacing: .5px;
  color: #999;
  padding: 0 12px 12px 0;
  text-align: left;
  border-bottom: 1px solid #e0e0e0;
  font-weight: 600;
}
.invoice-table tr {
  cursor: pointer;
  transition: background .1s;
}
.invoice-table tbody tr:hover { background: #f9f9f9; }
.invoice-table td {
  padding: 14px 12px 14px 0;
  border-bottom: 1px solid #f0f0f0;
  color: #666;
  font-size: 13px;
}
.invoice-table tbody tr:last-child td { border-bottom: none; }

.td-inv-num { font-weight: 700; color: #1a1a1a; }
.td-client  { color: #666; }
.td-date    { color: #999; }
.td-amount  { font-weight: 600; color: #1a1a1a; }
.td-actions { text-align: right; }

.action-menu-btn {
  background: none;
  border: 1px solid #e0e0e0;
  border-radius: 4px;
  color: #999;
  font-size: 14px;
  padding: 2px 8px;
  cursor: pointer;
  transition: all .15s;
}
.action-menu-btn:hover { border-color: #1a1a1a; color: #1a1a1a; }
</style>
