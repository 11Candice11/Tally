<template>
  <div class="summary-cards">
    <div class="card">
      <p class="label">Total</p>
      <p class="value">${{ total.toLocaleString() }}</p>
    </div>
    <div class="card">
      <p class="label">Paid</p>
      <p class="value">${{ paid.toLocaleString() }}</p>
    </div>
    <div class="card">
      <p class="label">Overdue</p>
      <p class="value">{{ overdueCount }}</p>
    </div>
  </div>
</template>

<script setup lang="ts">
defineProps<{
  total: number
  paid: number
  overdueCount: number
}>()
</script>

<style scoped>
.summary-cards { display: flex; gap: 1rem; }
.card { padding: 1rem 1.5rem; border-radius: 8px; background: #f8fafc; border: 1px solid #e2e8f0; min-width: 140px; }
.label { font-size: 0.75rem; color: #64748b; margin: 0; }
.value { font-size: 1.5rem; font-weight: 700; margin: 4px 0 0; }
</style>
