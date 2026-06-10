<template>
  <div class="reports">
    <!-- Page header -->
    <div class="page-header">
      <div>
        <h1>Reports</h1>
        <p class="page-sub">Revenue and invoice analytics</p>
      </div>
      <select v-model="period" class="period-select">
        <option value="0">All time</option>
        <option value="3">Last 3 months</option>
        <option value="6">Last 6 months</option>
        <option value="12">Last 12 months</option>
      </select>
    </div>

    <div v-if="store.loading" class="empty-state">Loading…</div>
    <div v-else-if="store.error" class="empty-state error">{{ store.error }}</div>
    <div v-else-if="store.invoices.length === 0" class="empty-state">No invoices found. Create some invoices first.</div>

    <template v-else>
    <!-- KPI row -->
    <div class="kpi-row">
      <div class="kpi-card">
        <p class="kpi-label">TOTAL REVENUE</p>
        <p class="kpi-value">R {{ fmt(summary.totalRevenue) }}</p>
        <p class="kpi-sub">{{ summary.paidCount }} paid invoices</p>
      </div>
      <div class="kpi-card">
        <p class="kpi-label">OUTSTANDING</p>
        <p class="kpi-value warn">R {{ fmt(summary.outstanding) }}</p>
        <p class="kpi-sub">{{ summary.sentCount }} awaiting payment</p>
      </div>
      <div class="kpi-card">
        <p class="kpi-label">OVERDUE</p>
        <p class="kpi-value danger">R {{ fmt(summary.overdue) }}</p>
        <p class="kpi-sub">{{ summary.overdueCount }} overdue</p>
      </div>
      <div class="kpi-card">
        <p class="kpi-label">AVG INVOICE VALUE</p>
        <p class="kpi-value">R {{ fmt(summary.avgValue) }}</p>
        <p class="kpi-sub">{{ summary.total }} invoices total</p>
      </div>
    </div>

    <div class="lower">
      <!-- Status breakdown -->
      <div class="card">
        <p class="section-label">STATUS BREAKDOWN</p>
        <div class="breakdown-list">
          <div v-for="item in statusBreakdown" :key="item.status" class="breakdown-row">
            <div class="breakdown-left">
              <span :class="['badge', `badge-${item.status.toLowerCase()}`]">{{ item.status.toUpperCase() }}</span>
              <span class="breakdown-count">{{ item.count }} invoice{{ item.count !== 1 ? 's' : '' }}</span>
            </div>
            <div class="breakdown-bar-wrap">
              <div class="breakdown-bar" :style="{ width: item.pct + '%', background: item.color }" />
            </div>
            <span class="breakdown-amount">R {{ fmt(item.amount) }}</span>
          </div>
        </div>
      </div>

      <!-- Top clients -->
      <div class="card">
        <p class="section-label">TOP CLIENTS BY REVENUE</p>
        <div class="top-clients">
          <div v-for="(client, i) in topClients" :key="client.email" class="top-client-row">
            <span class="rank">{{ i + 1 }}</span>
            <div class="top-client-info">
              <p class="top-client-name">{{ client.name }}</p>
              <p class="top-client-email">{{ client.email }}</p>
            </div>
            <div class="top-client-right">
              <p class="top-client-amount">R {{ fmt(client.total) }}</p>
              <p class="top-client-count">{{ client.count }} invoice{{ client.count !== 1 ? 's' : '' }}</p>
            </div>
          </div>
        </div>
      </div>
    </div>

    <!-- Revenue line graph -->
    <div class="card">
      <p class="section-label">REVENUE OVER TIME</p>
      <div class="chart-wrap">
        <Line :data="lineChartData" :options="lineChartOptions" />
      </div>
    </div>

    <!-- Monthly table -->
    <div class="card">
      <p class="section-label">MONTHLY BREAKDOWN</p>
      <table class="monthly-table">
        <thead>
          <tr>
            <th>MONTH</th>
            <th>INVOICES</th>
            <th>REVENUE</th>
            <th>OUTSTANDING</th>
            <th>OVERDUE</th>
          </tr>
        </thead>
        <tbody>
          <tr v-for="row in monthlyRows" :key="row.month">
            <td>{{ row.month }}</td>
            <td>{{ row.count }}</td>
            <td>R {{ fmt(row.revenue) }}</td>
            <td>R {{ fmt(row.outstanding) }}</td>
            <td :class="{ danger: row.overdue > 0 }">R {{ fmt(row.overdue) }}</td>
          </tr>
        </tbody>
      </table>
    </div>
    </template><!-- end v-else -->
  </div>
</template>

<script setup lang="ts">
import { ref, computed, onMounted } from 'vue'
import { useInvoiceStore } from '@/stores/invoiceStore'
import { invoiceTotal, InvoiceStatus } from '@/models/invoice'
import {
  Chart as ChartJS,
  CategoryScale,
  LinearScale,
  PointElement,
  LineElement,
  Title,
  Tooltip,
  Legend,
  Filler,
} from 'chart.js'
import { Line } from 'vue-chartjs'

ChartJS.register(CategoryScale, LinearScale, PointElement, LineElement, Title, Tooltip, Legend, Filler)

const store  = useInvoiceStore()
const period = ref('6')

onMounted(async () => {
  await store.fetchAll()
})

const filtered = computed(() => {
  const months = parseInt(period.value)
  if (months === 0) return store.invoices // all time
  const cutoff = new Date()
  cutoff.setMonth(cutoff.getMonth() - months)
  const cutoffStr = cutoff.toISOString().slice(0, 10)
  return store.invoices.filter(i => {
    if (!i.issueDate) return true
    return i.issueDate >= cutoffStr
  })
})

const summary = computed(() => {
  const invs = filtered.value
  const paid    = invs.filter(i => i.status === InvoiceStatus.Paid)
  const sent    = invs.filter(i => i.status === InvoiceStatus.Sent)
  const overdue = invs.filter(i => i.status === InvoiceStatus.Overdue)
  const totalRevenue = paid.reduce((s, i) => s + invoiceTotal(i), 0)
  const outstanding  = sent.reduce((s, i) => s + invoiceTotal(i), 0)
  const overdueAmt   = overdue.reduce((s, i) => s + invoiceTotal(i), 0)
  return {
    totalRevenue, outstanding, overdue: overdueAmt,
    paidCount: paid.length, sentCount: sent.length, overdueCount: overdue.length,
    total: invs.length,
    avgValue: invs.length ? invs.reduce((s, i) => s + invoiceTotal(i), 0) / invs.length : 0,
  }
})

const statusBreakdown = computed(() => {
  const invs = filtered.value
  const totalAmt = invs.reduce((s, i) => s + invoiceTotal(i), 0) || 1
  const groups = [
    { status: 'Paid',      color: '#10b981' },
    { status: 'Sent',      color: '#3b82f6' },
    { status: 'Overdue',   color: '#ef4444' },
    { status: 'Draft',     color: '#d1d5db' },
    { status: 'Cancelled', color: '#9ca3af' },
  ]
  return groups.map(g => {
    const matching = invs.filter(i => i.status === g.status)
    const amount = matching.reduce((s, i) => s + invoiceTotal(i), 0)
    return { ...g, count: matching.length, amount, pct: Math.round((amount / totalAmt) * 100) }
  }).filter(g => g.count > 0)
})

const topClients = computed(() => {
  const map = new Map<string, { name: string; email: string; total: number; count: number }>()
  for (const inv of filtered.value) {
    const key = inv.clientEmail.toLowerCase()
    if (!map.has(key)) map.set(key, { name: inv.clientName, email: inv.clientEmail, total: 0, count: 0 })
    const c = map.get(key)!
    c.total += invoiceTotal(inv)
    c.count++
  }
  return [...map.values()].sort((a, b) => b.total - a.total).slice(0, 5)
})

const monthlyRows = computed(() => {
  const map = new Map<string, { count: number; revenue: number; outstanding: number; overdue: number }>()
  for (const inv of filtered.value) {
    const key = inv.issueDate.slice(0, 7) // YYYY-MM
    if (!map.has(key)) map.set(key, { count: 0, revenue: 0, outstanding: 0, overdue: 0 })
    const row = map.get(key)!
    row.count++
    const t = invoiceTotal(inv)
    if (inv.status === InvoiceStatus.Paid)    row.revenue     += t
    if (inv.status === InvoiceStatus.Sent)    row.outstanding += t
    if (inv.status === InvoiceStatus.Overdue) row.overdue     += t
  }
  return [...map.entries()]
    .sort(([a], [b]) => b.localeCompare(a))
    .map(([month, data]) => ({
      month: new Date(month + '-01').toLocaleDateString('en-ZA', { month: 'short', year: 'numeric' }),
      ...data
    }))
})

// Chronological monthly data for the line graph
const chartRows = computed(() =>
  [...monthlyRows.value].reverse()
)

const lineChartData = computed(() => ({
  labels: chartRows.value.map(r => r.month),
  datasets: [
    {
      label: 'Revenue',
      data: chartRows.value.map(r => r.revenue),
      borderColor: '#10b981',
      backgroundColor: 'rgba(16, 185, 129, 0.08)',
      borderWidth: 2,
      pointRadius: 4,
      pointBackgroundColor: '#10b981',
      tension: 0.35,
      fill: true,
    },
    {
      label: 'Outstanding',
      data: chartRows.value.map(r => r.outstanding),
      borderColor: '#3b82f6',
      backgroundColor: 'rgba(59, 130, 246, 0.06)',
      borderWidth: 2,
      pointRadius: 4,
      pointBackgroundColor: '#3b82f6',
      tension: 0.35,
      fill: true,
    },
    {
      label: 'Overdue',
      data: chartRows.value.map(r => r.overdue),
      borderColor: '#ef4444',
      backgroundColor: 'rgba(239, 68, 68, 0.06)',
      borderWidth: 2,
      pointRadius: 4,
      pointBackgroundColor: '#ef4444',
      tension: 0.35,
      fill: true,
    },
  ],
}))

const lineChartOptions = {
  responsive: true,
  maintainAspectRatio: false,
  interaction: { mode: 'index' as const, intersect: false },
  plugins: {
    legend: {
      position: 'top' as const,
      labels: { font: { size: 12 }, color: '#666', boxWidth: 12, padding: 16 },
    },
    tooltip: {
      callbacks: {
        label: (ctx: import('chart.js').TooltipItem<'line'>) =>
          ` ${ctx.dataset.label ?? ''}: R ${ctx.parsed.y.toLocaleString('en-ZA')}`,
      },
    },
  },
  scales: {
    x: {
      grid: { color: '#f0f0f0' },
      ticks: { color: '#999', font: { size: 11 } },
    },
    y: {
      grid: { color: '#f0f0f0' },
      ticks: {
        color: '#999',
        font: { size: 11 },
        callback: (value: number | string) => `R ${Number(value).toLocaleString('en-ZA')}`,
      },
      beginAtZero: true,
    },
  },
}

function fmt(n: number) {
  return n.toLocaleString('en-ZA', { minimumFractionDigits: 0 })
}
</script>

<style scoped>
.reports { display: flex; flex-direction: column; gap: 20px; }

.page-header { display: flex; justify-content: space-between; align-items: flex-end; }
.page-header h1 { font-size: 28px; font-weight: 700; color: #1a1a1a; }
.page-sub { color: #999; font-size: 13px; margin-top: 4px; }

.period-select {
  background: #fff;
  border: 1px solid #e0e0e0;
  border-radius: 6px;
  color: #1a1a1a;
  font-family: inherit;
  font-size: 13px;
  padding: 8px 12px;
}

.empty-state { color: #999; font-size: 14px; padding: 60px 0; text-align: center; }
.empty-state.error { color: #ef4444; }

.kpi-row {
  display: grid;
  grid-template-columns: repeat(4, 1fr);
  gap: 1px;
  background: #e0e0e0;
  border: 1px solid #e0e0e0;
  border-radius: 10px;
  overflow: hidden;
}
.kpi-card {
  background: #fff;
  padding: 20px 24px;
  display: flex;
  flex-direction: column;
  gap: 6px;
}
.kpi-label { font-size: 11px; letter-spacing: 1px; color: #999; font-weight: 600; }
.kpi-value { font-size: 26px; font-weight: 700; color: #1a1a1a; letter-spacing: -1px; }
.kpi-value.warn   { color: #3b82f6; }
.kpi-value.danger { color: #ef4444; }
.kpi-sub { font-size: 12px; color: #666; }

.lower { display: grid; grid-template-columns: 1fr 1fr; gap: 16px; }

.card {
  background: #fff;
  border: 1px solid #e0e0e0;
  border-radius: 10px;
  padding: 20px 24px;
}
.section-label { font-size: 11px; letter-spacing: 1.5px; color: #999; margin-bottom: 16px; font-weight: 600; }

/* Status breakdown */
.breakdown-list { display: flex; flex-direction: column; gap: 14px; }
.breakdown-row { display: grid; grid-template-columns: 180px 1fr 120px; align-items: center; gap: 12px; }
.breakdown-left { display: flex; align-items: center; gap: 10px; }
.breakdown-count { font-size: 12px; color: #999; }
.breakdown-bar-wrap { background: #f0f0f0; border-radius: 4px; height: 6px; overflow: hidden; }
.breakdown-bar { height: 100%; border-radius: 4px; transition: width .4s; }
.breakdown-amount { font-size: 13px; font-weight: 600; color: #1a1a1a; text-align: right; }

/* Top clients */
.top-clients { display: flex; flex-direction: column; gap: 2px; }
.top-client-row {
  display: flex;
  align-items: center;
  gap: 14px;
  padding: 10px 0;
  border-bottom: 1px solid #f0f0f0;
}
.top-client-row:last-child { border-bottom: none; }
.rank { font-size: 12px; color: #999; width: 16px; text-align: center; }
.top-client-info { flex: 1; }
.top-client-name  { font-size: 13px; font-weight: 600; color: #1a1a1a; }
.top-client-email { font-size: 12px; color: #999; }
.top-client-right { text-align: right; }
.top-client-amount { font-size: 13px; font-weight: 600; color: #1a1a1a; }
.top-client-count  { font-size: 12px; color: #999; }

/* Chart */
.chart-wrap { height: 260px; position: relative; }

/* Monthly table */
.monthly-table { width: 100%; border-collapse: collapse; font-size: 13px; }
.monthly-table th {
  text-align: left;
  font-size: 11px;
  letter-spacing: .5px;
  color: #999;
  padding: 0 0 12px;
  border-bottom: 1px solid #e0e0e0;
  font-weight: 600;
}
.monthly-table td {
  padding: 12px 0;
  border-bottom: 1px solid #f0f0f0;
  color: #1a1a1a;
}
.monthly-table tr:last-child td { border-bottom: none; }
.monthly-table td.danger { color: #ef4444; }
</style>
